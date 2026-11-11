using BOT_Greg_v2_API.Models.External;

namespace BOT_Greg_v2_API.Models.Dtos;

public class MatchAlertDto
{
    public class GuildData
    {
        public string ChannelDiscordId { get; set; } = null!;
        public List<string> RoleDiscordIds { get; set; } = new();
    }

    public MatchUpcoming Match { get; set; } = null!;
    public List<GuildData> Guilds { get; set; } = new();
}
