using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Entities
{
    [Index(nameof(GuildId), IsUnique = true)]
    public class Guild : BaseEntity
    {
        [Required, MaxLength(22)]
        public string GuildId { get; set; } = null!;

        [Required]
        public string GuildName { get; set; } = null!;

        [Required]
        public int GuildMemberCount { get; set; }

    }
}
