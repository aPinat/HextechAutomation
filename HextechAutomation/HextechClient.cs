using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.JsonWebTokens;
using RiotAuth;

namespace HextechAutomation;

public class HextechClient
{
    private const string LedgeUrl = "https://euw-red.lol.sgp.pvp.net"; // TODO: clientconfig or system.yaml, maybe part of RiotAuth
    private const string Region = "EUW1"; // TODO: any token, maybe part of RiotAuth
    private const string Location = "lolriot.aws-euc1-prod.euw1"; // TODO: read system.yaml or hardcode by region or where else can I get this?

    private readonly HttpClient _http;
    private readonly LCUNameResolver _nameResolver;
    private readonly long _summonerId;
    private readonly string _summonerJwt;

    private PlayerLootDefinitionsResponseDTO? _lootDefinitions;

    private HextechClient(HttpClient http, LCUNameResolver nameResolver, string summonerJwt, long summonerId)
    {
        _http = http;
        _nameResolver = nameResolver;
        _summonerJwt = summonerJwt;
        _summonerId = summonerId;
    }

    public static async Task<HextechClient> CreateInstanceAsync(JsonWebToken sessionToken)
    {
        var nameResolverTask = LCUNameResolver.CreateInstanceAsync();
        var puuid = RiotSignOn.GetPuuid(sessionToken);

        var http = new HttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "HextechAutomation/0.1 (https://github.com/aPinat/HextechAutomation)");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken.EncodedToken);

        var json = await http.GetFromJsonAsync<JsonNode>($"{LedgeUrl}/summoner-ledge/v1/regions/{Region}/summoners/puuid/{puuid}/jwt");
        var summonerJwt = json?.GetValue<string>() ?? throw new Exception("Unable to fetch summoner JWT");
        var jwt = new JsonWebToken(summonerJwt);
        var summonerId = jwt.GetPayloadValue<long>("id");

        return new HextechClient(http, await nameResolverTask, summonerJwt, summonerId);
    }

    public async Task PrintPlayerLootAsync()
    {
        Console.WriteLine("Loot");
        var lootDefinitions = await GetPlayerLootDefinitionsAsync();
        foreach (var loot in lootDefinitions.PlayerLoot)
            Console.WriteLine(PlayerLootToString(loot));
    }

    public async Task CraftKeysAsync()
    {
        var lootDefinitions = await GetPlayerLootDefinitionsAsync();
        var keyFragment = lootDefinitions.PlayerLoot.FirstOrDefault(loot => loot.LootName == "MATERIAL_key_fragment");
        if (keyFragment is { Count: >= 3 })
        {
            var repeat = keyFragment.Count / 3;
            Console.Write($"Found {keyFragment.Count} key fragments. Craft {repeat} keys? (Y/n): ");
            await CraftAsync(new PlayerLootCraftRequestDTO
            {
                LootNameRefIds = new[] { new LootNameRefId { LootName = "MATERIAL_key_fragment" } }, RecipeName = "MATERIAL_key_fragment_forge", Repeat = repeat
            });
        }
    }

    public async Task OpenChestsAsync()
    {
        var lootDefinitions = await GetPlayerLootDefinitionsAsync();
        foreach (var (lootName, refId, count) in lootDefinitions.PlayerLoot.Where(loot => loot.LootName.StartsWith("CHEST")))
        {
            var recipe = lootDefinitions.RecipeList.Recipes.FirstOrDefault(recipe => recipe.Slots.Length == 1 && recipe.Slots.Any(slot => slot.Query == $"lootName == '{lootName}'"));
            if (recipe is null)
            {
                Console.WriteLine($"Ignoring {PlayerLootToString(lootName, refId, count)}, since it can't be opened by itself, aka probably requiring a Hextech Key.");
                continue;
            }

            Console.Write($"Found {PlayerLootToString(lootName, refId, count)}. Open all? (Y/n): ");
            await CraftAsync(new PlayerLootCraftRequestDTO { LootNameRefIds = new[] { new LootNameRefId { LootName = lootName, RefId = refId } }, RecipeName = recipe.RecipeName, Repeat = count });
        }
    }

    public async Task CleanupChampionShardsAsync()
    {
        var inventory = await _http.GetFromJsonAsync<JsonNode>($"{LedgeUrl}/lolinventoryservice-ledge/v1/inventories/simple?inventoryTypes=CHAMPION"); // no need for puuid, location, accountId params
        var ownedChampionIds = inventory?["data"]?["items"]?["CHAMPION"].Deserialize<long[]>() ?? throw new Exception("Unable to fetch player inventory.");

        var response = await _http.PostAsJsonAsync($"{LedgeUrl}/championmastery-ledge/player/{_summonerId}/champions", _summonerJwt);
        response.EnsureSuccessStatusCode();
        var masteries = await response.Content.ReadFromJsonAsync<ChampionMasteryResponseDTO[]>() ?? throw new Exception("Unable to fetch player champion mastery.");
        Console.WriteLine("Mastery");
        foreach (var mastery in masteries)
            Console.WriteLine($"{_nameResolver.GetChampionName(mastery.ChampionId)}: {mastery.ChampionPoints} Points; Level {mastery.ChampionLevel}; {mastery.TokensEarned} Tokens");

        var lootDefinitions = await GetPlayerLootDefinitionsAsync();
        foreach (var (lootName, refId, count) in lootDefinitions.PlayerLoot.Where(loot => loot.LootName.StartsWith("CHAMPION_RENTAL")))
        {
            Console.Write($"Found {PlayerLootToString(lootName, refId, count)}. ");

            var keep = 0;
            var championId = long.Parse(lootName.Remove(0, 16));

            if (!ownedChampionIds.Contains(championId))
            {
                Console.Write("Not owned. ");
                keep += 1;
            }
            else
            {
                Console.Write("Owned. ");
            }

            var mastery = masteries.FirstOrDefault(mastery => mastery.ChampionId == championId);
            switch (mastery)
            {
                case null:
                    Console.Write("Mastery 0. ");
                    keep += 2;
                    break;
                case { ChampionLevel: <= 5 }:
                    keep += 2;
                    break;
                case { ChampionLevel: 6 }:
                    keep += 1;
                    break;
            }

            if (mastery is { })
                Console.Write($"Mastery {mastery.ChampionLevel}. ");

            var repeat = count - keep;
            Console.Write($"Keep {keep}. ");
            if (repeat <= 0)
            {
                Console.WriteLine();
                continue;
            }

            Console.Write($"Disenchant {repeat}? (Y/n): ");
            await CraftAsync(new PlayerLootCraftRequestDTO
            {
                LootNameRefIds = new[] { new LootNameRefId { LootName = lootName, RefId = refId } }, RecipeName = "CHAMPION_RENTAL_disenchant", Repeat = repeat
            });
        }
    }

    public async Task ForgeEmotesAsync()
    {
        var lootDefinitions = await GetPlayerLootDefinitionsAsync();
        var emotesToReroll = lootDefinitions.PlayerLoot.Where(loot => loot.LootName.StartsWith("EMOTE") && !loot.LootName.StartsWith("EMOTE_RENTAL"))
            .Where(loot =>
            {
                var tags = lootDefinitions.LootItemList.LootItems.FirstOrDefault(item => item.LootName == loot.LootName)?.Tags;
                return tags != null && tags.Contains("nodisenchant") && tags.Contains("norerolloutput") && !tags.Contains("noreroll");
            });

        await ForgeEmotesAsync(emotesToReroll);
    }

    public async Task ForgeEmotesAsync(string[] keep)
    {
        if (keep.Length == 0)
        {
            Console.WriteLine("ERROR: Requires at least 1 emote to keep.");
            return;
        }

        while (true)
        {
            var lootDefinitions = await GetPlayerLootDefinitionsAsync();
            var emotesToReroll = lootDefinitions.PlayerLoot.Where(loot => loot.LootName.StartsWith("EMOTE") && !loot.LootName.StartsWith("EMOTE_RENTAL"))
                .Where(loot => lootDefinitions.LootItemList.LootItems.FirstOrDefault(item => item.LootName == loot.LootName)?.Tags?.Contains("noreroll") != true)
                .Where(loot => !keep.Contains(loot.LootName))
                .ToArray();
            if (emotesToReroll.Length == 0)
                return;

            await ForgeEmotesAsync(emotesToReroll);
        }
    }

    private async Task ForgeEmotesAsync(IEnumerable<PlayerLoot> emotesToReroll)
    {
        foreach (var (lootName, refId, count) in emotesToReroll)
        {
            var craftRequest = new PlayerLootCraftRequestDTO { LootNameRefIds = new[] { new LootNameRefId { LootName = lootName, RefId = refId } }, RecipeName = "EMOTE_forge", Repeat = count };
            await CraftAsync(craftRequest, false);
        }
    }

    private async Task<PlayerLootDefinitionsResponseDTO> GetPlayerLootDefinitionsAsync()
    {
        if (_lootDefinitions is null)
        {
            _lootDefinitions =
                await _http.GetFromJsonAsync<PlayerLootDefinitionsResponseDTO>(
                    $"{LedgeUrl}/loot/v1/playerlootdefinitions/location/{Location}/playerId/{_summonerId}?lastLootItemUpdate=0&lastRecipeUpdate=0&lastQueryUpdate=0");
            return _lootDefinitions ?? throw new Exception("Unable to fetch player loot definitions.");
        }

        var updatedLootDefinitions =
            await _http.GetFromJsonAsync<PlayerLootDefinitionsResponseDTO>(
                $"{LedgeUrl}/loot/v1/playerlootdefinitions/location/{Location}/playerId/{_summonerId}?lastLootItemUpdate={_lootDefinitions.LootItemList.LastUpdate}&lastRecipeUpdate={_lootDefinitions.RecipeList.LastUpdate}&lastQueryUpdate={_lootDefinitions.QueryResult.LastUpdate}");

        if (updatedLootDefinitions is null)
            throw new Exception("Unable to fetch updated player loot definitions.");

        var lootItemList = updatedLootDefinitions.LootItemList.LastUpdate == _lootDefinitions.LootItemList.LastUpdate ? _lootDefinitions.LootItemList : updatedLootDefinitions.LootItemList;
        var recipeList = updatedLootDefinitions.RecipeList.LastUpdate == _lootDefinitions.RecipeList.LastUpdate ? _lootDefinitions.RecipeList : updatedLootDefinitions.RecipeList;
        var queryResult = updatedLootDefinitions.QueryResult.LastUpdate == _lootDefinitions.QueryResult.LastUpdate ? _lootDefinitions.QueryResult : updatedLootDefinitions.QueryResult;
        _lootDefinitions = new PlayerLootDefinitionsResponseDTO(lootItemList, recipeList, queryResult, updatedLootDefinitions.PlayerLoot);

        return _lootDefinitions;
    }

    public async Task CraftAsync(PlayerLootCraftRequestDTO craftRequest, bool ask = true)
    {
        if (ask)
        {
            var key = Console.ReadKey();
            Console.WriteLine();
            if (key is not { Key: ConsoleKey.Y or ConsoleKey.Enter })
                return;
        }

        var responseMessage = await _http.PostAsJsonAsync($"{LedgeUrl}/loot/v1/playerloot/location/lolriot.ams1.euw1/craftref/id/{Guid.NewGuid()}", craftRequest);
        responseMessage.EnsureSuccessStatusCode();
        var craftResponse = await responseMessage.Content.ReadFromJsonAsync<PlayerLootCraftResponseDTO>();
        switch (craftResponse)
        {
            case { Status: "OK" }:
                if (craftResponse.Removed.Length > 0)
                    Console.WriteLine(
                        $"Removed: {craftResponse.Removed.Aggregate(string.Empty, (s, loot) => s + PlayerLootToString(loot) + "; ")}");
                if (craftResponse.Added.Length > 0)
                    Console.WriteLine(
                        $"Added: {craftResponse.Added.Aggregate(string.Empty, (s, loot) => s + PlayerLootToString(loot) + "; ")}");
                if (craftResponse.Redeemed.Length > 0)
                    Console.WriteLine(
                        $"Redeemed: {craftResponse.Redeemed.Aggregate(string.Empty, (s, lootName) => s + PlayerLootToString(lootName, "", 1) + "; ")}");
                break;
            case { Status: not null }:
                Console.WriteLine($"{craftResponse.Status}: {craftResponse.Details}");
                break;
            default:
                Console.WriteLine(await responseMessage.Content.ReadAsStringAsync());
                break;
        }
    }

    private string PlayerLootToString(PlayerLoot loot) => PlayerLootToString(loot.LootName, loot.RefId, loot.Count);
    private string PlayerLootToString(string lootName, string refId, long count) => $"{count}x {_nameResolver.GetLootHumanName(lootName, refId)} [{lootName}]";
}
