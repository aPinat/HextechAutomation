using HextechAutomation;
using RiotAuth;
using RiotAuth.Clients;

var username = Environment.GetEnvironmentVariable("USERNAME") ?? throw new ArgumentException("USERNAME environment variable not set");
var password = Environment.GetEnvironmentVariable("PASSWORD") ?? throw new ArgumentException("PASSWORD environment variable not set");

var rso = new RiotSignOn(username, password);
var leagueClient = new LeagueClient(rso);
var sessionToken = await leagueClient.GetLoginSessionTokenAsync();
var hextech = await HextechClient.CreateInstanceAsync(sessionToken);
await hextech.PrintPlayerLootAsync();
await hextech.OpenChestsAsync();
await hextech.CraftKeysAsync();
await hextech.CleanupChampionShardsAsync();
