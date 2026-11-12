using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class FilterDeleteDto
{
    [Required]
    public string GuildDiscordId { get; set; } = null!;

    // filters are identified by their internal id, because they only have a role if the role management is enabled
    public List<Guid> FilterIds { get; set; } = new();
}
