namespace BOT_Greg_v2_API.Models.External
{
    public class MatchResult : MatchBase
    {
        public int TeamXMapScore { get; init; }
        public int TeamYMapScore { get; init; }
        public bool isForfeit { get; init; }
        public List<Map> Maps { get; init; } = null!;
    }

    public class Map
    {
        public string Name { get; init; } = null!;
        public int? TeamXScore { get; init; } = null;
        public int? TeamYScore { get; init; } = null;
        public bool MapPlayed => TeamXScore.HasValue && TeamYScore.HasValue;
    }
}
