namespace HextechAutomation;

public record LootDefinition(LootItemList LootItemList, RecipeList RecipeList, QueryResult QueryResult, PlayerLoot[] PlayerLoot);

public record LootItemList(LootItem[] LootItems, long LastUpdate);

public record LootItem(string LootName,
    string? Asset,
    string Type,
    string Rarity,
    long Value,
    long? StoreItemId,
    string Tags,
    string DisplayCategories,
    string UpgradeLootName,
    long? RentalSeconds,
    long? RentalGames);

public record PlayerLoot(string LootName, string RefId, long Count);

public record QueryResult(Dictionary<string, string[]> QueryToLootNames, long LastUpdate);

public record RecipeList(Recipe[] Recipes, long LastUpdate);

public record Recipe(string RecipeName, string Type, string DisplayCategories, Slot[] Slots, Output[] Outputs, string CrafterName, Metadata Metadata);

public record Metadata(Description[] GuaranteedDescriptions, Description[] BonusDescriptions, bool TooltipsDisabled);

public record Description(string LootName, string[] ChildLootTableNames);

public record Output(string LootName, string QuantityExpression, double Probability, bool AllowDuplicates);

public record Slot(long SlotNumber, string Query, string QuantityExpression);
