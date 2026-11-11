using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Entities;

[Index(nameof(DiscordId), IsUnique = true)]
public class Guild : BaseEntity
{
    [Required, MaxLength(22)]
    public string DiscordId { get; set; } = null!;

    [Required]
    public string GuildName { get; set; } = null!;

    [Required]
    public int GuildMemberCount { get; set; }

    [MaxLength(22)]
    public string? AlertRoleDiscordId { get; set; } = null;

    [MaxLength(22)]
    public string? MatchAlertsChannelDiscordId { get; set; } = null;

    [MaxLength(22)]
    public string? ResultAlertsChannelDiscordId { get; set; } = null;

    public virtual GuildSettings Settings { get; set; } = null!;
    public virtual List<Filter> Filters { get; set; } = new List<Filter>();

    public virtual List<RoleLink> RoleLinks { get; set; } = new List<RoleLink>();
}
