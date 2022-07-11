using HextechAutomation;
using RiotAuth.Clients;

var username = Environment.GetEnvironmentVariable("USERNAME") ?? throw new ArgumentException("USERNAME environment variable not set");
var password = Environment.GetEnvironmentVariable("PASSWORD") ?? throw new ArgumentException("PASSWORD environment variable not set");

var leagueClient = await LeagueClient.CreateInstanceAsync(username, password);
var sessionToken = await leagueClient.GetLoginSessionTokenAsync();
var hextech = await HextechClient.CreateInstanceAsync(sessionToken);
await hextech.PrintPlayerLootAsync();
await hextech.OpenChestsAsync();
await hextech.CraftKeysAsync();
await hextech.CleanupChampionShardsAsync();
await hextech.ForgeEmotesAsync(); // Forge no-disenchant emotes into unowned emotes
await hextech.ForgeEmotesAsync(new[] { "EMOTE_3153", "EMOTE_3154", "EMOTE_3155", "EMOTE_1506" }); // Keep Bee and D'Pengu Emotes

// await hextech.CraftAsync(new PlayerLootCraftRequestDTO { LootNameRefIds = new []{new LootNameRefId {LootName = "MATERIAL_rare"}}, RecipeName = "MATERIAL_rare_forge_1_1", Repeat = 10}, false);
