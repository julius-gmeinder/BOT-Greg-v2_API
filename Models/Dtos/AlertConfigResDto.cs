using BOT_Greg_v2_API.Models.External;

namespace BOT_Greg_v2_API.Models.Dtos;

public class AlertConfigResDto
{
    public AlertConfigDto NewConfig { get; set; } = null!;
    public int MatResFilterCount { get; set; }
    public MatchUpcoming? GuildsNextUpcomingMatch { get; set; } = null;
}
