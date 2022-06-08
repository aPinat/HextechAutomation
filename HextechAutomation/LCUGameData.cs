namespace HextechAutomation;

public record LCUGameDataLoot(LCUGameDataLootItem[] LootItems);

public record LCUGameDataLootItem(string Id, string Name);

public record LCUGameDataChampion(long Id, string Name);

public record LCUGameDataSkin(long Id, string Name);

public record LCUGameDataEmote(long Id, string Name);

public record LCUGameDataWardSkin(long Id, string Name);

public record LCUGameDataSummonerIcon(long Id, string Title);
