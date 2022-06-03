using System.Text.Json.Serialization;

namespace HextechAutomation;

// puuid, accountId, playerId can all be missing. Must just be taken from session token.
public record PlayerLootCraftRequestDTO
{
    [JsonPropertyName("accountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? AccountId { get; init; }

    [JsonPropertyName("clientId")]
    public string ClientId { get; init; } = "aPinat-HextechAutomation";

    [JsonPropertyName("lootNameRefIds")]
    public LootNameRefId[] LootNameRefIds { get; init; } = Array.Empty<LootNameRefId>();

    [JsonPropertyName("playerId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? PlayerId { get; init; }

    [JsonPropertyName("puuid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid? Puuid { get; init; }

    [JsonPropertyName("recipeName")]
    public string RecipeName { get; init; } = null!;

    [JsonPropertyName("repeat")]
    public long Repeat { get; init; } = 1;
}

public record LootNameRefId
{
    [JsonPropertyName("lootName")]
    public string LootName { get; init; } = null!;

    [JsonPropertyName("refId")]
    public string RefId { get; init; } = string.Empty;
}
