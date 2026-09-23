namespace BOT_Greg_v2_API.Models.External
{
    public class TeamVrs
    {
        public int Rank { get; init; }
        public int Points { get; init; }
        public string Name { get; init; } = null!;
        public HashSet<string> Players { get; init; } = null!;
    }
}
