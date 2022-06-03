namespace HextechAutomation;

public record PlayerLootCraftResponseDTO(string Status, string Details, PlayerLoot[] Added, PlayerLoot[] Removed, string[] Redeemed);
