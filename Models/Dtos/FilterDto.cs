using BOT_Greg_v2_API.Models.Enums;

namespace BOT_Greg_v2_API.Models.Dtos;

public class FilterDto
{
    public Guid Id { get; set; }

    public int Nr { get; set; }

    // null if the role management of the guild is disabled
    public string? RoleDiscordId { get; set; }

    public string? Team { get; set; }

    public string? Event { get; set; }

    public EventTier? EventTier { get; set; }
    public string? EventTierString => EventTier?.GetString();

    public int? VrsRank { get; set; }

    public bool? HltvCoverage { get; set; }
}
