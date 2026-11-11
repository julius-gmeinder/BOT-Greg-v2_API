using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Dtos;

public class GuildDataDto
{
    [Required, MaxLength(22)]
    public string GuildId { get; set; } = null!;

    [Required]
    public string GuildName { get; set; } = null!;

    public int GuildMemberCount { get; set; }
}
