using BOT_Greg_v2_API.Models.External;

namespace BOT_Greg_v2_API.Models.Dtos;

public class MatchAlertDto
{
    public class GuildData
    {
        public string ChannelId { get; set; } = null!;
        public List<string> RoleIds { get; set; } = new();
    }

    public MatchUpcoming Match { get; set; } = null!;
    public List<GuildData> Guilds { get; set; } = new();
}
