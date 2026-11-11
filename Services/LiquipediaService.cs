using BOT_Greg_v2_API.Helpers;
using BOT_Greg_v2_API.Models.Enums;
using BOT_Greg_v2_API.Models.External;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text.Json;
using Stream = BOT_Greg_v2_API.Models.External.Stream;

namespace BOT_Greg_v2_API.Services;

public class LiquipediaService
{
    private readonly HttpClient _httpClient;
    private readonly VrsService _vrs;
    private readonly ILogger<LiquipediaService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _apiKey;

    private static readonly TimeSpan MatchesCacheTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResultsCacheTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EventsCacheTime = TimeSpan.FromHours(24);
    private static readonly TimeSpan TeamsCacheTime = TimeSpan.FromHours(24);

    public LiquipediaService(HttpClient httpClient, IConfiguration config, ILogger<LiquipediaService> logger, VrsService vrs, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _vrs = vrs;
        _cache = cache;
        _apiKey = config["ApiKeys:Liquipedia"]!;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Apikey", _apiKey);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BOT-Greg-v2/1.0 (julius.gmeinder@proton.me)");
    }

    // refetch = true skips the cache and fetches the newest data (which then replaces the cached data)
    public Task<List<MatchUpcoming>> GetMatchesAsync(bool refetch = false) => GetCachedAsync("lp_matches", MatchesCacheTime, FetchMatchesAsync, refetch);

    public Task<List<MatchResult>> GetResultsAsync(bool refetch = false) => GetCachedAsync("lp_results", ResultsCacheTime, FetchResultsAsync, refetch);

    public Task<List<Event>> GetEventsAsync(bool refetch = false) => GetCachedAsync("lp_events", EventsCacheTime, FetchEventsAsync, refetch);

    public Task<List<Team>> GetTeamsAsync(bool refetch = false) => GetCachedAsync("lp_teams", TeamsCacheTime, FetchTeamsAsync, refetch);

    private async Task<List<T>> GetCachedAsync<T>(string key, TimeSpan expiration, Func<Task<List<T>>> fetch, bool refetch)
    {
        if (!refetch && _cache.TryGetValue(key, out List<T>? cached) && cached != null)
            return cached;

        var data = await fetch();

        // empty most likely means the request failed, so fetch again next time instead of caching an empty list
        if (data.Count > 0)
            _cache.Set(key, data, expiration);

        return data;
    }


    private async Task<List<MatchUpcoming>> FetchMatchesAsync()
    {
        List<MatchUpcoming> matches = new();

        string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        string future = DateTime.UtcNow.AddHours(25).ToString("yyyy-MM-dd HH:mm:ss");

        string conditions = $"[[game::cs2]] AND [[date::>{now}]] AND [[date::<{future}]]";
        string limit = "100";

        string url = $"{Constants.LpApiBaseUrl}/match?wiki=counterstrike&limit={limit}&conditions={Uri.EscapeDataString(conditions)}&order=date%20ASC";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Fetching matches failed: {response.StatusCode} - {response.ReasonPhrase}");
            return matches;
        }

        var data = await response.Content.ReadAsStringAsync();
        using JsonDocument json = JsonDocument.Parse(data);
        //JsonTools.LogJson(json);

        var resultNode = json.RootElement.GetProperty("result");

        foreach (var element in resultNode.EnumerateArray())
        {
            List<Stream> streams = new();
            var streamNode = element.GetProperty("stream");

            HashSet<(StreamPlatform, string)> seenStreams = new();

            // if an object is empty, php serializes it as an empty array [] instead of an empty object {}
            if (streamNode.ValueKind != JsonValueKind.Array)
            {
                foreach (var streamElement in streamNode.EnumerateObject())
                {
                    string platformString = streamElement.Name.Split('_')[0];

                    if (!Enum.TryParse<StreamPlatform>(platformString, ignoreCase: true, out var platform))
                        continue;

                    string channel = streamElement.Value.GetString()!;

                    // .add() returns true if the item was added, false if it was already present
                    if (seenStreams.Add((platform, channel)))
                    {
                        streams.Add(new Stream
                        {
                            Platform = platform,
                            Channel = channel
                        });
                    }

                }
            }

            MatchTeam?[] teams = { null, null };
            var teamsNode = element.GetProperty("match2opponents");

            for (int i = 0; i <= 1; i++)
            {
                var teamNode = teamsNode[i];

                if (teamNode.GetProperty("type").GetString() == "team")
                {
                    teams[i] = new MatchTeam()
                    {
                        Name = teamNode.GetProperty("teamtemplate").GetProperty("name").GetString()!,
                        NameShort = teamNode.GetProperty("teamtemplate").GetProperty("shortname").GetString()!,
                        Players = teamNode.GetProperty("match2players").EnumerateArray().Select(p => new MatchPlayer
                        {
                            Name = p.GetProperty("name").GetString()!,
                            Country = p.GetProperty("flag").GetString()!,
                        }).ToList()
                    };
                }
            }

            string? hltvUrl = null;
            string? faceitUrl = null;
            var linksNode = element.GetProperty("links");

            if (linksNode.ValueKind != JsonValueKind.Array)
            {
                if (linksNode.TryGetProperty("hltv", out var hltvProperty))
                {
                    hltvUrl = hltvProperty.GetProperty("1").GetProperty("1").GetString();
                }

                if (linksNode.TryGetProperty("faceit", out var faceitProperty))
                {
                    faceitUrl = faceitProperty.GetProperty("1").GetProperty("1").GetString();
                }
            }


            MatchUpcoming match = new()
            {
                MatchId = element.GetProperty("match2id").GetString()!,

                EventId = element.GetProperty("pageid").GetInt32()!,
                EventName = element.GetProperty("tournament").GetString()!,
                EventRelUrl = element.GetProperty("parent").GetString()!,

                DateTime = DateTime.Parse(element.GetProperty("date").GetString()!),
                BestOf = element.GetProperty("bestof").GetInt32(),
                Environment = element.GetProperty("type").GetString()!,
                Stage = element.GetProperty("section").GetString()!,

                LpTier = element.GetProperty("liquipediatier").GetString()!,
                ValveTier = element.GetProperty("publishertier").GetString()!,

                HltvUrl = hltvUrl,
                FaceitUrl = faceitUrl,

                Streams = streams,

                TeamX = teams[0],
                TeamY = teams[1],
            };

            matches.Add(match);
        }

        return matches;
    }

    private async Task<List<MatchResult>> FetchResultsAsync()
    {
        List<MatchResult> results = new();

        string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        string past = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");

        string conditions = $"[[game::cs2]] AND [[date::<{now}]] AND [[date::>{past}]] AND [[finished::1]]";
        string limit = "100";

        string url = $"{Constants.LpApiBaseUrl}/match?wiki=counterstrike&limit={limit}&conditions={Uri.EscapeDataString(conditions)}&order=date%20DESC";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Fetching results failed: {response.StatusCode} - {response.ReasonPhrase}");
            return results;
        }

        var data = await response.Content.ReadAsStringAsync();
        using JsonDocument json = JsonDocument.Parse(data);
        //JsonTools.LogJson(json);

        var resultNode = json.RootElement.GetProperty("result");

        foreach (var element in resultNode.EnumerateArray())
        {
            MatchTeam?[] teams = { null, null };
            var teamsNode = element.GetProperty("match2opponents");

            bool isForfeitMatch = false;
            string? matchWinner = null;

            if (element.TryGetProperty("resulttype", out var resultTypeNode))
            {
                string? rt = resultTypeNode.GetString()?.ToLowerInvariant();
                if (rt == "ff" || rt == "forfeit" || rt == "walkover")
                    isForfeitMatch = true;
            }

            if (element.TryGetProperty("walkover", out var walkoverNode) && !string.IsNullOrWhiteSpace(walkoverNode.GetString()))
            {
                isForfeitMatch = true;
                matchWinner = walkoverNode.GetString();
            }

            if (element.TryGetProperty("winner", out var winnerNode))
            {
                matchWinner = winnerNode.GetString();
            }

            for (int i = 0; i <= 1; i++)
            {
                var teamNode = teamsNode[i];

                string? scoreStr = null;
                if (teamNode.TryGetProperty("score", out var scoreNode))
                {

                    scoreStr = scoreNode.ValueKind == JsonValueKind.Number
                        ? scoreNode.GetInt32().ToString()
                        : scoreNode.GetString()?.ToUpperInvariant();
                }

                string? statusStr = null;
                if (teamNode.TryGetProperty("status", out var statusNode) && statusNode.ValueKind == JsonValueKind.String)
                    statusStr = statusNode.GetString()?.ToUpperInvariant();

                if (scoreStr == "W" || scoreStr == "FF" || statusStr == "W" || statusStr == "FF")
                {
                    isForfeitMatch = true;
                    if (scoreStr == "W" || statusStr == "W")
                        matchWinner = (i + 1).ToString();
                }

                if (teamNode.GetProperty("type").GetString() == "team")
                {
                    teams[i] = new MatchTeam()
                    {
                        Name = teamNode.GetProperty("teamtemplate").GetProperty("name").GetString()!,
                        NameShort = teamNode.GetProperty("teamtemplate").GetProperty("shortname").GetString()!,
                        Players = teamNode.GetProperty("match2players").EnumerateArray().Select(p => new MatchPlayer
                        {
                            Name = p.GetProperty("name").GetString()!,
                            Country = p.GetProperty("flag").GetString()!,
                        }).ToList()
                    };
                }
            }

            string? hltvUrl = null;
            string? faceitUrl = null;
            var linksNode = element.GetProperty("links");

            if (linksNode.ValueKind != JsonValueKind.Array)
            {
                if (linksNode.TryGetProperty("hltv", out var hltvProperty))
                    hltvUrl = hltvProperty.GetProperty("1").GetProperty("1").GetString();

                if (linksNode.TryGetProperty("faceit", out var faceitProperty))
                    faceitUrl = faceitProperty.GetProperty("1").GetProperty("1").GetString();
            }

            List<Map> maps = new();
            if (element.TryGetProperty("match2games", out var gamesNode) && gamesNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var gameNode in gamesNode.EnumerateArray())
                {
                    int[] mapScores = { 0, 0 };

                    if (gameNode.TryGetProperty("scores", out var scoresNode) && scoresNode.ValueKind == JsonValueKind.Array)
                    {
                        int index = 0;
                        foreach (var score in scoresNode.EnumerateArray())
                        {
                            if (index <= 1)
                            {
                                if (score.ValueKind == JsonValueKind.Number)
                                    mapScores[index] = score.GetInt32();
                                else if (score.ValueKind == JsonValueKind.String)
                                    int.TryParse(score.GetString(), out mapScores[index]);
                            }

                            index++;
                        }
                    }

                    bool mapPlayed = (gameNode.TryGetProperty("winner", out var winnerProp) && !string.IsNullOrWhiteSpace(winnerProp.GetString()))
                                    || mapScores[0] > 0
                                    || mapScores[1] > 0;

                    maps.Add(new Map
                    {
                        Name = gameNode.TryGetProperty("map", out var mapProp) ? mapProp.GetString()! : "Unknown",
                        TeamXScore = mapPlayed ? mapScores[0] : null,
                        TeamYScore = mapPlayed ? mapScores[1] : null
                    });
                }
            }

            int teamXMapScore = maps.Count(m => m.TeamXScore > m.TeamYScore);
            int teamYMapScore = maps.Count(m => m.TeamYScore > m.TeamXScore);

            if (isForfeitMatch)
            {
                teamXMapScore = matchWinner == "1" ? 1 : 0;
                teamYMapScore = matchWinner == "2" ? 1 : 0;
            }

            MatchResult match = new()
            {
                MatchId = element.GetProperty("match2id").GetString()!,

                EventId = element.GetProperty("pageid").GetInt32()!,
                EventName = element.GetProperty("tournament").GetString()!,
                EventRelUrl = element.GetProperty("parent").GetString()!,

                DateTime = DateTime.Parse(element.GetProperty("date").GetString()!),
                BestOf = element.GetProperty("bestof").GetInt32(),
                Environment = element.GetProperty("type").GetString()!,
                Stage = element.GetProperty("section").GetString()!,

                LpTier = element.GetProperty("liquipediatier").GetString()!,
                ValveTier = element.GetProperty("publishertier").GetString()!,

                HltvUrl = hltvUrl,
                FaceitUrl = faceitUrl,

                TeamX = teams[0],
                TeamY = teams[1],

                TeamXMapScore = teamXMapScore,
                TeamYMapScore = teamYMapScore,
                isForfeit = isForfeitMatch,
                Maps = maps
            };

            results.Add(match);
        }

        return results;
    }

    private async Task<List<Event>> FetchEventsAsync()
    {
        List<Event> events = new();

        string now = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string past = DateTime.UtcNow.AddDays(-14).ToString("yyyy-MM-dd"); // restricting startdate, to filter "event-groups"

        string conditions = $"[[game::cs2]] AND [[enddate::>{now}]] AND [[startdate::>{past}]]";
        string limit = "100";

        string url = $"{Constants.LpApiBaseUrl}/tournament?wiki=counterstrike&limit={limit}&conditions={Uri.EscapeDataString(conditions)}&order=startdate%20ASC";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Fetching evemts failed: {response.StatusCode} - {response.ReasonPhrase}");
            return events;
        }

        var data = await response.Content.ReadAsStringAsync();
        using JsonDocument json = JsonDocument.Parse(data);
        //JsonTools.LogJson(json);

        var resultNode = json.RootElement.GetProperty("result");

        foreach (var element in resultNode.EnumerateArray())
        {
            string lpTier = element.GetProperty("liquipediatier").GetString()!;
            string valveTier = element.GetProperty("publishertier").GetString()!;
            int tier = valveTier == "Major Championship" ? 0 : Convert.ToInt32(lpTier);

            var locationNode = element.GetProperty("locations");
            string? region = null;
            string? country = null;
            string? city = null;

            if (locationNode.ValueKind != JsonValueKind.Array)
            {
                region = locationNode.TryGetProperty("region1", out var regionProp) ? regionProp.GetString() : null;

                string? countryCode = locationNode.TryGetProperty("country1", out var countryProp) ? countryProp.GetString() : null;
                country = CountryMapper.GetName(countryCode);

                city = locationNode.TryGetProperty("city1", out var cityProp) ? cityProp.GetString() : null;
            }

            Event ev = new()
            {
                EventId = element.GetProperty("pageid").GetInt32(),
                EventName = element.GetProperty("name").GetString()!,
                EventRelUrl = element.GetProperty("pagename").GetString()!,

                StartDate = element.TryGetProperty("startdate", out var sdProp) && DateTime.TryParse(sdProp.GetString(), out var sd) ? sd : null,
                EndDate = element.TryGetProperty("enddate", out var edProp) && DateTime.TryParse(edProp.GetString(), out var ed) ? ed : null,

                Environment = element.GetProperty("type").GetString()!,

                Tier = (EventTier)tier,
                LpTier = lpTier,
                ValveTier = valveTier,

                PrizePool = element.TryGetProperty("prizepool", out var ppProp) && ppProp.ValueKind == JsonValueKind.Number ? ppProp.GetDouble() : null,
                TeamsAttending = element.TryGetProperty("participantsnumber", out var partProp) && partProp.TryGetInt32(out var count) && count > 0 ? count : null,

                Region = region,
                Country = country,
                City = city
            };

            events.Add(ev);
        }

        return events;
    }

    private async Task<List<Team>> FetchTeamsAsync()
    {
        List<Team> teams = new();

        var vrsTeams = await _vrs.GetVrsTeamsAsync();

        if (vrsTeams.Count == 0)
            return teams;

        var squads = await GetSquadPlayersAsync();
        var vrsByPage = MatchSquadsToVrs(squads, vrsTeams);

        // chunked to keep the url short
        foreach (var pageNames in vrsByPage.Keys.Chunk(50))
        {
            teams.AddRange(await GetTeamsByPageNamesAsync(pageNames, vrsByPage));
        }

        return teams.OrderBy(t => t.Vrs?.Rank ?? int.MaxValue).ToList();
    }

    private async Task<List<Team>> GetTeamsByPageNamesAsync(string[] pageNames, Dictionary<string, TeamVrs> vrsByPage)
    {
        List<Team> teams = new();

        string pageFilter = string.Join(" OR ", pageNames.Select(p => $"[[pagename::{p}]]"));
        string conditions = $"[[status::active]] AND ({pageFilter})";
        string limit = "100";

        string url = $"{Constants.LpApiBaseUrl}/team?wiki=counterstrike&limit={limit}&conditions={Uri.EscapeDataString(conditions)}&order=name%20ASC";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Fetching teams failed: {response.StatusCode} - {response.ReasonPhrase}");
            return teams;
        }

        var data = await response.Content.ReadAsStringAsync();
        using JsonDocument json = JsonDocument.Parse(data);
        //JsonTools.LogJson(json);

        var resultNode = json.RootElement.GetProperty("result");

        foreach (var element in resultNode.EnumerateArray())
        {
            string? region = null;
            string? country = null;

            if (element.TryGetProperty("location", out var locationNode))
            {
                // Some endpoints return location as an object, others as a raw string flag code 
                if (locationNode.ValueKind == JsonValueKind.Object)
                {
                    region = locationNode.TryGetProperty("region", out var rProp) ? rProp.GetString() : null;

                    string? countryCode = locationNode.TryGetProperty("country", out var ccProp) ? ccProp.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(countryCode))
                        country = CountryMapper.GetName(countryCode);
                }
                else if (locationNode.ValueKind == JsonValueKind.String)
                {
                    string? countryCode = locationNode.GetString();
                    if (!string.IsNullOrWhiteSpace(countryCode))
                        country = CountryMapper.GetName(countryCode);
                }
            }

            string lpRelativeUrl = element.GetProperty("pagename").GetString()!;
            vrsByPage.TryGetValue(lpRelativeUrl, out var vrsTeam);

            Team team = new()
            {
                TeamId = element.GetProperty("pageid").GetInt32(),
                Name = element.GetProperty("name").GetString()!,
                RelUrl = lpRelativeUrl,

                NameShort = element.TryGetProperty("shortname", out var snProp) ? snProp.GetString() : null,

                Region = region,
                Country = country,

                Vrs = vrsTeam
            };

            teams.Add(team);
        }

        return teams;
    }

    private async Task<Dictionary<string, HashSet<string>>> GetSquadPlayersAsync()
    {
        Dictionary<string, HashSet<string>> squads = new(StringComparer.OrdinalIgnoreCase);

        const int limit = 1000; // max page size of the liquipedia api
        string conditions = "[[type::player]] AND [[status::active]]";

        for (int offset = 0; ; offset += limit)
        {
            string url = $"{Constants.LpApiBaseUrl}/squadplayer?wiki=counterstrike&query=pagename,id&limit={limit}&offset={offset}&conditions={Uri.EscapeDataString(conditions)}&order=pagename%20ASC,id%20ASC";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Fetching squad players failed: {response.StatusCode} - {response.ReasonPhrase}");
                break;
            }

            var data = await response.Content.ReadAsStringAsync();
            using JsonDocument json = JsonDocument.Parse(data);

            int count = 0;

            foreach (var element in json.RootElement.GetProperty("result").EnumerateArray())
            {
                count++;

                string? pageName = element.GetProperty("pagename").GetString();
                string? player = element.GetProperty("id").GetString();

                if (string.IsNullOrWhiteSpace(pageName) || string.IsNullOrWhiteSpace(player))
                    continue;

                if (!squads.ContainsKey(pageName))
                    squads[pageName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                squads[pageName].Add(player.Trim());
            }

            if (count < limit)
                break;
        }

        return squads;
    }

    private Dictionary<string, TeamVrs> MatchSquadsToVrs(Dictionary<string, HashSet<string>> squads, List<TeamVrs> vrsTeams)
    {
        Dictionary<string, TeamVrs> matches = new(StringComparer.OrdinalIgnoreCase);
        int minOverlap = 3;

        foreach (var vrsTeam in vrsTeams)
        {
            string? bestPage = null;
            int bestOverlap = 0;

            foreach (var (page, players) in squads)
            {
                int overlap = vrsTeam.Players.Count(players.Contains);

                if (overlap >= minOverlap && overlap > bestOverlap)
                {
                    bestPage = page;
                    bestOverlap = overlap;
                }
            }

            // vrs teams are ordered by rank, so the better ranked team keeps the page
            if (bestPage != null)
                matches.TryAdd(bestPage, vrsTeam);
        }

        return matches;
    }
}
