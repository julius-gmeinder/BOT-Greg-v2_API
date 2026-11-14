using System.Text.Json.Serialization;

namespace BOT_Greg_v2_API.Models.Dtos;

public class GuildSettingsDto
{
    public bool AlertsPaused { get; set; }

    public bool MentionOnDelays { get; set; }

    public bool MentionOnResults { get; set; }

    public bool CensorResults { get; set; }

    public bool RoleManagement { get; set; }
}
