using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class AlertConfigDto
{
    [MaxLength(22)]
    public string? MatchAlertChannelDiscordId { get; set; } = null;

    [MaxLength(22)]
    public string? ResultAlertChannelDiscordId { get; set; } = null;

    [MaxLength(22)]
    public string? AlertRoleDiscordId { get; set; } = null;
}
