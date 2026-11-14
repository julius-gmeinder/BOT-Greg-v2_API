using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class GuildSettingsSetDto : GuildSettingsDto
{
    // the roles below are only needed (and only allowed) if the role management gets enabled by this request,
    // the bot has to create them in discord before it calls the api

    [MaxLength(22)]
    public string? AlertRoleDiscordId { get; set; } = null;

    // one role for every filter of the guild
    public List<FilterRoleDto> FilterRoles { get; set; } = new();
}
