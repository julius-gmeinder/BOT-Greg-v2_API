using BOT_Greg_v2_API.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class FilterAddDto
{
    [Required]
    public string GuildId { get; set; } = null!;

    [Required, MaxLength(22)]
    public string RoleId { get; set; } = null!;

    [MaxLength(64)]
    public string? Team { get; set; } = null;

    [MaxLength(128)]
    public string? Event { get; set; } = null;

    [EnumDataType(typeof(EventTier))]
    public EventTier? EventTier { get; set; } = null;

    [Range(1, 1000)]
    public int? VrsRank { get; set; } = null;

    public bool? HltvCoverage { get; set; } = null;
}
