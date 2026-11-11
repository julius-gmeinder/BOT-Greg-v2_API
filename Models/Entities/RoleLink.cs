using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Entities;

public class RoleLink : BaseEntity
{
    [Required, MaxLength(22)]
    public string BotRoleDiscordId { get; set; } = null!;

    [Required, MaxLength(22)]
    public string LinkedRoleDiscordId { get; set; } = null!;

    [Required]
    public virtual Guild Guild { get; set; } = null!;
}
