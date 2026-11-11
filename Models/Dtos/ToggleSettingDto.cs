using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class ToggleSettingDto
{
    [Required]
    public string GuildId { get; set; } = null!;

    public bool NewValue { get; set; }
}
