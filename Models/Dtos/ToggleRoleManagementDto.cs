using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class ToggleRoleManagementDto
{
    public class FilterRole
    {
        public Guid FilterId { get; set; }

        [Required, MaxLength(22)]
        public string RoleDiscordId { get; set; } = null!;
    }

    [Required]
    public string GuildDiscordId { get; set; } = null!;

    public bool NewValue { get; set; }

    // the roles below are only needed (and only used) to enable the role management,
    // the bot has to create them in discord before it calls the api

    [MaxLength(22)]
    public string? AlertRoleDiscordId { get; set; } = null;

    // one role for every filter of the guild
    public List<FilterRole> FilterRoles { get; set; } = new();
}
