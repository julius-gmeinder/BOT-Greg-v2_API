using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class RoleLinkRemoveDto
{
    [Required]
    public string BotRoleDiscordId { get; set; } = null!;

    [Required]
    public string LinkedRoleDiscordId { get; set; } = null!;
}
