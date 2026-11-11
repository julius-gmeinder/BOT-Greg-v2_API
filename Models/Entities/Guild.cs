using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Entities;

[Index(nameof(GuildId), IsUnique = true)]
public class Guild : BaseEntity
{
    [Required, MaxLength(22)]
    public string GuildId { get; set; } = null!;

    [Required]
    public string GuildName { get; set; } = null!;

    [Required]
    public int GuildMemberCount { get; set; }

    [MaxLength(22)]
    public string? AlertRoleId { get; set; } = null;

    [MaxLength(22)]
    public string? MatchAlertsChannelId { get; set; } = null;

    [MaxLength(22)]
    public string? ResultAlertsChannelId { get; set; } = null;

    public bool AlertsPaused { get; set; } = false;

    public bool MentionOnDelays { get; set; } = false;

    public bool MentionOnResults { get; set; } = true;

    public bool CensorResults { get; set; } = false;

    public virtual List<Filter> FiltersMatRes { get; set; } = new List<Filter>();

    public virtual List<RoleLink> RoleLinks { get; set; } = new List<RoleLink>();
}
