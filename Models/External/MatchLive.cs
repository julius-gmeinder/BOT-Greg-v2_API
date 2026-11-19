namespace BOT_Greg_v2_API.Models.External;

public class MatchLive : MatchUpcoming
{
    // maps won so far, a map in progress does not count
    public int TeamXMapScore { get; init; }
    public int TeamYMapScore { get; init; }
    public List<Map> Maps { get; init; } = null!;
}
