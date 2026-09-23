using BOT_Greg_v2_API.Models.External;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace BOT_Greg_v2_API.Services
{
    public class VrsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<VrsService> _logger;

        public VrsService(HttpClient httpClient, ILogger<VrsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BOT-Greg-v2");
        }

        public async Task<List<TeamVrs>> GetVrsTeamsAsync()
        {
            List<TeamVrs> teams = new();

            string? latestFilePath = await GetLatestVrsFilePathAsync();

            if (string.IsNullOrEmpty(latestFilePath))
            {
                _logger.LogError("Could not find latest VRS file");
                return teams;
            }

            string urlBase = "https://raw.githubusercontent.com/ValveSoftware/counter-strike_regional_standings/main/";
            string url = $"{urlBase}{latestFilePath}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch raw VRS file: {response.StatusCode}");
                return teams;
            }

            string markdown = await response.Content.ReadAsStringAsync();
            teams = ParseMarkdown(markdown);

            return teams;
        }

        private async Task<string?> GetLatestVrsFilePathAsync()
        {
            string url = "https://api.github.com/repos/ValveSoftware/counter-strike_regional_standings/git/trees/main?recursive=1";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);

            var treeNode = doc.RootElement.GetProperty("tree");
            List<string> globalFiles = new();

            foreach (var element in treeNode.EnumerateArray())
            {
                string? path = element.GetProperty("path").GetString();
                if (string.IsNullOrEmpty(path))
                    continue;

                if (path.Contains("standings_global") && path.EndsWith(".md"))
                    globalFiles.Add(path);
            }

            // valve naming scheme is YYYY_MM_DD
            return globalFiles.OrderByDescending(file => file).FirstOrDefault();
        }

        private List<TeamVrs> ParseMarkdown(string markdown)
        {
            List<TeamVrs> teams = new();

            string[] lines = markdown.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string line in lines)
            {
                if (!line.Contains('|'))
                    continue;

                string[] columns = line.Split('|', StringSplitOptions.TrimEntries);
                if (columns.Length < 5)
                    continue;

                // offset necessary because of leading pipes and spaces
                int offset = string.IsNullOrWhiteSpace(columns[0]) ? 1 : 0;

                string rankString = columns[0 + offset];
                string pointsString = columns[1 + offset];
                string nameString = columns[2 + offset];
                string rosterString = columns[3 + offset];

                bool isRankValid = int.TryParse(rankString, out int rank);
                bool isPointsValid = int.TryParse(pointsString, out int points);

                if (!isRankValid || !isPointsValid)
                    continue;

                var players = rosterString
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                teams.Add(new TeamVrs
                {
                    Rank = rank,
                    Points = points,
                    Name = nameString,
                    Players = players
                });
            }

            return teams;
        }
    }
}