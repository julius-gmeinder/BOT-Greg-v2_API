using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class RoleLinkRemoveDto
{
    [Required]
    public string BotRoleId { get; set; } = null!;

    [Required]
    public string LinkedRoleId { get; set; } = null!;
}
