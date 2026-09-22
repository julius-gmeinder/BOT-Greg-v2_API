namespace BOT_Greg_v2_API.Models.External
{
    public class MatchBase
    {
        public string MatchId { get; init; } = null!;

        public int EventId { get; init; }
        public string EventName { get; init; } = null!;
        public string EventRelUrl { get; init; } = null!;
        public string EventUrl => $"{Constants.LpBaseUrl}/{EventRelUrl}";

        public DateTime DateTime { get; init; }

        public int BestOf { get; init; }
        public string Environment { get; init; } = null!;
        public string Stage { get; init; } = null!;

        
        public string LpTier { get; init; } = null!;
        public string ValveTier { get; init; } = null!;

        public string? HltvUrl { get; init; } = null;
        public string? FaceitUrl { get; init; } = null;

        public MatchTeam? TeamX { get; init; } = null!;
        public MatchTeam? TeamY { get; init; } = null!;
    }

    public class MatchTeam
    {
        public string Name { get; init; } = null!;
        public string NameShort { get; init; } = null!;
        public List<MatchPlayer> Players { get; init; } = null!;
    }

    public class MatchPlayer
    {
        public string Name { get; init; } = null!;
        public string Country { get; init; } = null!;
    }
}
