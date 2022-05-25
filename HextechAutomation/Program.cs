using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RiotAuth;
using RiotAuth.Clients;

var username = Environment.GetEnvironmentVariable("USERNAME") ?? throw new ArgumentException("USERNAME environment variable not set");
var password = Environment.GetEnvironmentVariable("PASSWORD") ?? throw new ArgumentException("PASSWORD environment variable not set");
const string ledgeUrl = "https://euw-blue.lol.sgp.pvp.net"; // TODO: clientconfig or system.yaml, maybe part of RiotAuth
const string region = "EUW1"; // TODO: any token, maybe part of RiotAuth
const string location = "lolriot.ams1.euw1"; // TODO: read system.yaml or hardcode by region or where else can I get this?

var rso = new RiotSignOn(username, password);
var leagueClient = new LeagueClient(rso);

var sessionToken = await leagueClient.GetLoginSessionTokenAsync();
if (sessionToken is null)
    return;

var puuid = await leagueClient.GetPuuidAsync();
Console.WriteLine($"PUUID: {puuid}");

var http = new HttpClient();
http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PinatClient/0.1");
http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
http.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

var champions = await http.GetFromJsonAsync<JsonArray>("https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-summary.json");

http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {sessionToken}");

var json = await http.GetFromJsonAsync<JsonNode>($"{ledgeUrl}/summoner-ledge/v1/regions/{region}/summoners/puuid/{puuid}/jwt");
var summonerJwt = json?.GetValue<string>();
var jwt = new JsonWebToken(summonerJwt);
Console.WriteLine(Base64UrlEncoder.Decode(jwt.EncodedPayload));
var summonerId = jwt.GetPayloadValue<long>("id");
Console.WriteLine($"Summoner ID: {summonerId}");

Console.WriteLine("Champion Mastery");
var response = await http.PostAsJsonAsync($"{ledgeUrl}/championmastery-ledge/player/{summonerId}/champions", summonerJwt);
json = await response.Content.ReadFromJsonAsync<JsonNode>();
var masteries = json?.AsArray();
if (masteries is not null)
{
    foreach (var mastery in masteries)
    {
        Console.WriteLine($"{mastery?["championId"]}: {mastery?["championPoints"]} Points, Level {mastery?["championLevel"]}, {mastery?["tokensEarned"]} Tokens");
    }
}

Console.WriteLine("Loot");
json = await http.GetFromJsonAsync<JsonNode>($"{ledgeUrl}/loot/v1/playerlootdefinitions/location/{location}/playerId/{summonerId}");
var lootItems = json?["playerLoot"]?.AsArray();
if (lootItems is not null)
{
    foreach (var item in lootItems)
    {
        Console.WriteLine($"{item?["lootName"]} [{item?["refId"]}]: {item?["count"]}");
    }
}
