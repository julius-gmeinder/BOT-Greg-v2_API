using BOT_Greg_v2_API.Helpers;
using BOT_Greg_v2_API.Models.Enums;
using BOT_Greg_v2_API.Models.External;
using System.Net.Http.Headers;
using System.Text.Json;
using Stream = BOT_Greg_v2_API.Models.External.Stream;

namespace BOT_Greg_v2_API.Services
{
    public class LiquipediaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LiquipediaService> _logger;
        private readonly string _apiKey;

        public LiquipediaService(HttpClient httpClient, IConfiguration config, ILogger<LiquipediaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = config["ApiKeys:Liquipedia"]!;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Apikey", _apiKey);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BOT-Greg-v2/1.0 (julius.gmeinder@proton.me)");
        }

        public async Task<List<MatchUpcoming>> GetMatchesAsync()
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

        public async Task<List<MatchResult>> GetResultsAsync()
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
            JsonTools.LogJson(json);

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

        public async Task<List<Event>> GetEventsAsync()
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

        public async Task<List<Team>> GetTeamsAsync()
        {
            List<Team> teams = new();

            string conditions = "[[game::cs2]]";
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

                Team team = new()
                {
                    TeamId = element.GetProperty("pageid").GetInt32(),
                    Name = element.GetProperty("name").GetString()!,
                    RelUrl = element.GetProperty("pagename").GetString()!,

                    NameShort = element.TryGetProperty("shortname", out var snProp) ? snProp.GetString() : null,

                    Region = region,
                    Country = country
                };

                teams.Add(team);
            }

            return teams;
        }

    }
}
