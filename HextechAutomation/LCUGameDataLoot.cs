namespace HextechAutomation;

public record LCUGameDataLoot(LCUGameDataLootItem[] LootItems);

public record LCUGameDataLootItem
(
    string Id,
    string Name,
    string Description,
    string Image,
    string StartDate,
    string EndDate,
    long MappedStoreId,
    long LifetimeMax,
    bool AutoRedeem,
    string Rarity,
    string Type,
    string? RecipeMenuActive,
    string? RecipeMenuTitle,
    string? RecipeMenuSubtitle
);
