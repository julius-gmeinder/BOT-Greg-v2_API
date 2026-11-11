using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class FilterDeleteDto
{
    [Required]
    public string GuildDiscordId { get; set; } = null!;

    public List<string> RoleDiscordIds { get; set; } = new();
}
