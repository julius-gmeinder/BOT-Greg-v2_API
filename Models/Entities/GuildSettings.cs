using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOT_Greg_v2_API.Models.Entities;

public class GuildSettings : BaseEntity
{
    public bool AlertsPaused { get; set; } = false;

    public bool MentionOnDelays { get; set; } = false;

    public bool MentionOnResults { get; set; } = true;

    public bool CensorResults { get; set; } = false;

    // opt-in: only if enabled, the bot creates the alert role and one role per filter (and roles can be linked)
    public bool RoleManagement { get; set; } = false;

    [Required, ForeignKey("GuildId")]
    public virtual Guild Guild { get; set; } = null!;
}
