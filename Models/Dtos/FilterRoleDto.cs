using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class FilterRoleDto
{
    public Guid FilterId { get; set; }

    [Required, MaxLength(22)]
    public string RoleDiscordId { get; set; } = null!;
}
