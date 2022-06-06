namespace HextechAutomation;

public record ChampionMasteryResponseDTO(long PlayerId,
    long ChampionId,
    long ChampionLevel,
    long ChampionPoints,
    long LastPlayTime,
    long ChampionPointsSinceLastLevel,
    long ChampionPointsUntilNextLevel,
    bool ChestGranted,
    long TokensEarned,
    string? HighestGrade);
