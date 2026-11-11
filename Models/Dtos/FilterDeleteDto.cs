using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class FilterDeleteDto
{
    [Required]
    public string GuildId { get; set; } = null!;

    public List<string> RoleIds { get; set; } = new();
}
