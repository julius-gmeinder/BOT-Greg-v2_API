using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Entities;

[Index(nameof(MatchId), IsUnique = true)]
public class HistoryMatchAlert : BaseEntity
{
    [Required, MaxLength(64)]
    public string MatchId { get; set; } = null!;

    // start time the match had when it was last alerted, used to detect delays
    [Required]
    public DateTime DateTime { get; set; }

    public int AlertsSent { get; set; }
}
