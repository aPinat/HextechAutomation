using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.JsonWebTokens;
using RiotAuth;
using RiotAuth.Clients;

var username = Environment.GetEnvironmentVariable("USERNAME") ?? throw new ArgumentException("USERNAME environment variable not set");
var password = Environment.GetEnvironmentVariable("PASSWORD") ?? throw new ArgumentException("PASSWORD environment variable not set");
const string ledgeUrl = "https://euw-blue.lol.sgp.pvp.net";
const string region = "EUW1";
const string location = "lolriot.ams1.euw1";

var rso = new RiotSignOn(username, password);
var leagueClient = new LeagueClient(rso);

var sessionToken = await leagueClient.GetLoginSessionTokenAsync();
if (sessionToken is null)
    return;

var puuid = await leagueClient.GetPuuidAsync();
Console.WriteLine($"PUUID: {puuid}");

var http = new HttpClient();

var champions = await http.GetFromJsonAsync<JsonArray>("https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-summary.json");

http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);

var json = await http.GetFromJsonAsync<JsonNode>($"{ledgeUrl}/summoner-ledge/v1/regions/{region}/summoners/puuid/{puuid}/jwt");
var summonerJwt = json?.GetValue<string>();
var jwt = new JsonWebToken(summonerJwt);
var summonerId = jwt.GetPayloadValue<long>("id");
Console.WriteLine($"Summoner ID: {summonerId}");

var response = await http.PostAsJsonAsync($"{ledgeUrl}/championmastery-ledge/player/{summonerId}/champions", summonerJwt);
json = await response.Content.ReadFromJsonAsync<JsonNode>();
var championMasteries = json?.AsArray();
if (championMasteries is not null)
{
    foreach (var championMastery in championMasteries)
    {
        Console.WriteLine($"{championMastery?["championId"]}: {championMastery?["championPoints"]} Points, Level {championMastery?["championLevel"]}, {championMastery?["tokensEarned"]} Tokens");
    }
}

json = await http.GetFromJsonAsync<JsonNode>($"{ledgeUrl}/loot/v1/playerlootdefinitions/location/{location}/playerId/{summonerId}");
var lootItems = json?["playerLoot"]?.AsArray();
if (lootItems is not null)
{
    foreach (var item in lootItems)
    {
        Console.WriteLine($"{item?["lootName"]} [{item?["refId"]}]: {item?["count"]}");
    }
}
