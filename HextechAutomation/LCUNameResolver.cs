using System.Net.Http.Json;
using System.Text.Json;

namespace HextechAutomation;

public class LCUNameResolver
{
    private readonly LCUGameDataChampion[]? _champions;
    private readonly LCUGameDataEmote[]? _emotes;
    private readonly LCUGameDataLoot? _loot;
    private readonly Dictionary<string, string>? _lootTrans;
    private readonly Dictionary<long, LCUGameDataSkin>? _skins;
    private readonly LCUGameDataSummonerIcon[]? _summonerIcons;
    private readonly LCUGameDataWardSkin[]? _wardSkins;

    private LCUNameResolver(LCUGameDataChampion[]? champions,
        LCUGameDataEmote[]? emotes,
        LCUGameDataLoot? loot,
        Dictionary<string, string>? lootTrans,
        Dictionary<long, LCUGameDataSkin>? skins,
        LCUGameDataSummonerIcon[]? summonerIcons,
        LCUGameDataWardSkin[]? wardSkins)
    {
        _champions = champions;
        _emotes = emotes;
        _loot = loot;
        _lootTrans = lootTrans;
        _skins = skins;
        _summonerIcons = summonerIcons;
        _wardSkins = wardSkins;
    }

    public static async Task<LCUNameResolver> CreateInstanceAsync()
    {
        var http = new HttpClient();
        var championsTask = http.GetFromJsonAsync<LCUGameDataChampion[]>("https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/champion-summary.json");
        var emotesTask = http.GetFromJsonAsync<LCUGameDataEmote[]>("https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/summoner-emotes.json");
        var lootTask = http.GetFromJsonAsync<LCUGameDataLoot>("https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/loot.json");
        var lootTransTask = http.GetFromJsonAsync<Dictionary<string, string>>("https://raw.communitydragon.org/pbe/plugins/rcp-fe-lol-loot/global/default/trans.json",
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = false });
        var skinsTask = http.GetFromJsonAsync<Dictionary<long, LCUGameDataSkin>>("https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/skins.json");
        var summonerIconsTask = http.GetFromJsonAsync<LCUGameDataSummonerIcon[]>("https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/summoner-icons.json");
        var wardSkinsTask = http.GetFromJsonAsync<LCUGameDataWardSkin[]>("https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/ward-skins.json");
        return new LCUNameResolver(await championsTask, await emotesTask, await lootTask, await lootTransTask, await skinsTask, await summonerIconsTask, await wardSkinsTask);
    }

    public string GetChampionName(long championId) => _champions?.FirstOrDefault(champion => champion.Id == championId)?.Name ?? "UNKNOWN";
    public string GetChampionName(string championId) => GetChampionName(long.Parse(championId));

    public string GetChampionSkinName(long skinId) => _skins?.GetValueOrDefault(skinId)?.Name ?? "UNKNOWN";
    public string GetChampionSkinName(string skinId) => GetChampionSkinName(long.Parse(skinId));

    public string GetEmoteName(long emoteId) => _emotes?.FirstOrDefault(emote => emote.Id == emoteId)?.Name ?? "UNKNOWN";
    public string GetEmoteName(string emoteId) => GetEmoteName(long.Parse(emoteId));

    public string GetWardSkinName(long wardSkinId) => _wardSkins?.FirstOrDefault(wardSkin => wardSkin.Id == wardSkinId)?.Name ?? "UNKNOWN";
    public string GetWardSkinName(string wardSkinId) => GetWardSkinName(long.Parse(wardSkinId));

    public string GetSummonerIconName(long summonerIconId) => _summonerIcons?.FirstOrDefault(summonerIcon => summonerIcon.Id == summonerIconId)?.Title ?? "UNKNOWN";
    public string GetSummonerIconName(string summonerIconId) => GetSummonerIconName(long.Parse(summonerIconId));

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
            return _loot?.LootItems.FirstOrDefault(item => item.Id == lootName)?.Name ?? _lootTrans?.GetValueOrDefault($"loot_name_{lootName.ToLower()}") ?? "UNKNOWN";

        if (lootName.StartsWith("TOURNAMENTLOGO"))
            return _lootTrans?.GetValueOrDefault($"loot_name_{lootName.ToLower()}") ?? "UNKNOWN" + " Logo";

        if (lootName.StartsWith("CHAMPION_RENTAL"))
            return GetChampionName(lootName.Remove(0, 16)) + " Champion Shard";

        if (lootName.StartsWith("CHAMPION_TOKEN"))
            return (_lootTrans?.GetValueOrDefault($"loot_name_{lootName.ToLower()}") ?? "UNKNOWN") + GetChampionName(refId);

        if (lootName.StartsWith("CHAMPION_SKIN_RENTAL"))
            return GetChampionSkinName(lootName.Remove(0, 21)) + " Skin Shard";

        if (lootName.StartsWith("CHAMPION_SKIN"))
            return GetChampionSkinName(lootName.Remove(0, 14)) + " Skin Permanent";

        if (lootName.StartsWith("CHAMPION"))
            return GetChampionName(lootName.Remove(0, 9)) + " Champion Permanent";

        if (lootName.StartsWith("EMOTE_RENTAL"))
            return GetEmoteName(lootName.Remove(0, 13)) + " Emote Shard";

        if (lootName.StartsWith("EMOTE"))
            return GetEmoteName(lootName.Remove(0, 6)) + " Emote Permanent";

        if (lootName.StartsWith("WARD_SKIN_RENTAL"))
            return GetWardSkinName(lootName.Remove(0, 17)) + " Skin Shard";

        if (lootName.StartsWith("WARD_SKIN"))
            return GetWardSkinName(lootName.Remove(0, 10)) + " Skin Permanent";

        if (lootName.StartsWith("SUMMONER_ICON"))
            return GetSummonerIconName(lootName.Remove(0, 14)) + " Summoner Icon";

        if (lootName.StartsWith("CHROMA_RENTAL"))
            return "UNKNOWN" + " Chroma Shard";

        if (lootName.StartsWith("CHROMA"))
            return "UNKNOWN" + " Chroma";

        if (lootName.StartsWith("COMPANION"))
            return "UNKNOWN" + " Tactician Permanent";

        return "UNKNOWN";
    }
}
