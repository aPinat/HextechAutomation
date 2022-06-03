using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HextechAutomation;

public class LCUNameResolver
{
    private readonly JsonArray? _champions;
    private readonly LCUGameDataLoot? _gameDataLoot;
    private readonly JsonNode? _lootTrans;
    private readonly JsonNode? _skins;

    private LCUNameResolver(LCUGameDataLoot? gameDataLoot, JsonNode? lootTrans, JsonArray? champions, JsonNode? skins)
    {
        _gameDataLoot = gameDataLoot;
        _lootTrans = lootTrans;
        _champions = champions;
        _skins = skins;
    }

    public static async Task<LCUNameResolver> CreateInstanceAsync()
    {
        var http = new HttpClient();
        var gameDataLootTask = http.GetFromJsonAsync<LCUGameDataLoot>("https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/loot.json");
        var lootTransTask = http.GetFromJsonAsync<JsonNode>("https://raw.communitydragon.org/pbe/plugins/rcp-fe-lol-loot/global/default/trans.json", new JsonSerializerOptions());
        var championsTask = http.GetFromJsonAsync<JsonArray>("https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/champion-summary.json");
        var skinsTask = http.GetFromJsonAsync<JsonNode>("https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/skins.json");
        return new LCUNameResolver(await gameDataLootTask, await lootTransTask, await championsTask, await skinsTask);
    }

    public string GetChampionName(string championId) => _champions?.First(node => node?["id"]?.GetValue<int>().ToString() == championId)?["name"]?.GetValue<string>() ?? "UNKNOWN";

    public string GetChampionSkinName(string skinId) => _skins?[skinId]?["name"]?.GetValue<string>() ?? "UNKNOWN";

    public string GetLootHumanName(string lootName, string refId = "")
    {
        switch (lootName)
        {
            case "MATERIAL_key_fragment":
                return "Key Fragment";
            case "CURRENCY_champion":
                return "Blue Essence";
            case "CURRENCY_cosmetic":
                return "Orange Essence";
        }

        if (lootName.StartsWith("CHEST") || lootName.StartsWith("MATERIAL") || lootName.StartsWith("STATSTONE") || lootName.StartsWith("CURRENCY"))
            return _gameDataLoot?.LootItems.FirstOrDefault(item => item.Id == lootName)?.Name ?? _lootTrans?[$"loot_name_{lootName.ToLower()}"]?.GetValue<string>() ?? "UNKNOWN";

        if (lootName.StartsWith("TOURNAMENTLOGO"))
            return _lootTrans?[$"loot_name_{lootName.ToLower()}"]?.GetValue<string>() ?? "UNKNOWN" + " Logo";

        if (lootName.StartsWith("CHAMPION_RENTAL"))
            return GetChampionName(lootName.Remove(0, 16)) + " Champion Shard";

        if (lootName.StartsWith("CHAMPION_TOKEN"))
            return (_lootTrans?[$"loot_name_{lootName.ToLower()}"]?.GetValue<string>() ?? "UNKNOWN") + GetChampionName(refId);

        if (lootName.StartsWith("CHAMPION_SKIN_RENTAL"))
            return GetChampionSkinName(lootName.Remove(0, 21)) + " Skin Shard";

        if (lootName.StartsWith("CHAMPION_SKIN"))
            return GetChampionSkinName(lootName.Remove(0, 14)) + " Skin Permanent";

        if (lootName.StartsWith("CHAMPION"))
            return GetChampionName(lootName.Remove(0, 9)) + " Champion Permanent";

        // TODO

        if (lootName.StartsWith("EMOTE_RENTAL"))
            return "UNKNOWN" + " Emote Shard";

        if (lootName.StartsWith("EMOTE"))
            return "UNKNOWN" + " Emote Permanent";

        if (lootName.StartsWith("WARD_SKIN_RENTAL"))
            return "UNKNOWN" + " Ward Skin Shard";

        if (lootName.StartsWith("WARD_SKIN"))
            return "UNKNOWN" + " Ward Skin Permanent";

        if (lootName.StartsWith("SUMMONER_ICON"))
            return "UNKNOWN" + " Summoner Icon";

        if (lootName.StartsWith("CHROMA_RENTAL"))
            return "UNKNOWN" + " Chroma Shard";

        if (lootName.StartsWith("CHROMA"))
            return "UNKNOWN" + " Chroma";

        if (lootName.StartsWith("COMPANION"))
            return "UNKNOWN" + " Tactician Permanent";

        return "UNKNOWN";
    }
}
