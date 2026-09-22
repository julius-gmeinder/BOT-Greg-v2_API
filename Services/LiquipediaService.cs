using BOT_Greg_v2_API.Helpers;
using BOT_Greg_v2_API.Models.Enums;
using BOT_Greg_v2_API.Models.External;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Stream = BOT_Greg_v2_API.Models.External.Stream;

namespace BOT_Greg_v2_API.Services
{
    public class LiquipediaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public LiquipediaService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["ApiKeys:Liquipedia"]!;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Apikey", _apiKey);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BOT-Greg-v2/1.0 (julius.gmeinder@proton.me)");
        }

        public async Task<List<MatchUpcoming>> GetMatchesAsync()
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            string future = DateTime.UtcNow.AddHours(25).ToString("yyyy-MM-dd HH:mm:ss");

            string conditions = $"[[game::cs2]] AND [[date::>{now}]] AND [[date::<{future}]]";
            string limit = "100";

            var url = $"{Constants.LpApiBaseUrl}/match?wiki=counterstrike&limit={limit}&conditions={Uri.EscapeDataString(conditions)}&order=date%20ASC";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                using JsonDocument json = JsonDocument.Parse(data);
                //DumpJson(json);

                List<MatchUpcoming> matches = new();
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
                            StreamPlatform platform = Enum.Parse<StreamPlatform>(streamElement.Name.Split('_')[0], ignoreCase: true);
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
            else
            {
                Console.WriteLine($"Failed: {response.StatusCode} - {response.ReasonPhrase}");
                return new List<MatchUpcoming>();
            }
        }

        public async Task<List<MatchResult>> GetResultsAsync()
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            string past = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");

            string conditions = $"[[game::cs2]] AND [[date::<{now}]] AND [[date::>{past}]] AND [[finished::1]]";
            string limit = "100";

            var url = $"{Constants.LpApiBaseUrl}/match?wiki=counterstrike&limit={limit}&conditions={Uri.EscapeDataString(conditions)}&order=date%20DESC";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                using JsonDocument json = JsonDocument.Parse(data);
                //DumpJson(json);

                List<MatchResult> matches = new();
                var resultNode = json.RootElement.GetProperty("result");

                foreach (var element in resultNode.EnumerateArray())
                {
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
                                        {
                                            mapScores[index] = score.GetInt32();
                                        }
                                        else if (score.ValueKind == JsonValueKind.String)
                                        {
                                            int.TryParse(score.GetString(), out mapScores[index]);
                                        }
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

                        TeamXMapScore = maps.Count(m => m.TeamXScore > m.TeamYScore),
                        TeamYMapScore = maps.Count(m => m.TeamYScore > m.TeamXScore),
                        Maps = maps
                    };

                    matches.Add(match);
                }

                return matches;
            }
            else
            {
                Console.WriteLine($"Failed: {response.StatusCode} - {response.ReasonPhrase}");
                return new List<MatchResult>();
            }
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string past = DateTime.UtcNow.AddDays(-14).ToString("yyyy-MM-dd"); // restricting startdate, to filter "event-groups"

            string conditions = $"[[game::cs2]] AND [[enddate::>{now}]] AND [[startdate::>{past}]]";
            string limit = "100";

            var url = $"{Constants.LpApiBaseUrl}/tournament?wiki=counterstrike&limit={limit}&conditions={Uri.EscapeDataString(conditions)}&order=startdate%20ASC";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                using JsonDocument json = JsonDocument.Parse(data);
                //DumpJson(json);

                List<Event> events = new();
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
            else
            {
                Console.WriteLine($"Failed: {response.StatusCode} - {response.ReasonPhrase}");
                return new List<Event>();
            }
        }

        public async Task<List<Team>> GetTeamsAsync()
        {
            string conditions = "[[game::cs2]]";
            string limit = "100";

            var url = $"{Constants.LpApiBaseUrl}/team?wiki=counterstrike&limit={limit}&conditions={Uri.EscapeDataString(conditions)}&order=name%20ASC";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                using JsonDocument json = JsonDocument.Parse(data);
                DumpJson(json);

                List<Team> teams = new();
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
            else
            {
                Console.WriteLine($"Failed: {response.StatusCode} - {response.ReasonPhrase}");
                return new List<Team>();
            }
        }

        private void DumpJson(JsonDocument json)
        {
            string formatted = JsonSerializer.Serialize(json, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            Console.WriteLine(formatted);
        }

    }
}
