using BOT_Greg_v2_API.Models.External;

namespace BOT_Greg_v2_API.Models.Dtos;

public class ResultAlertDto
{
    public class GuildData
    {
        public string ChannelDiscordId { get; set; } = null!;
        public bool MentionOnResults { get; set; }
        public bool CensorResults { get; set; }
        public List<string> RoleDiscordIds { get; set; } = new();
    }

    public MatchResult Result { get; set; } = null!;
    public List<GuildData> Guilds { get; set; } = new();
}
