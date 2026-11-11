using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Entities;

[Index(nameof(MatchId), IsUnique = true)]
public class HistoryResultAlert : BaseEntity
{
    [Required, MaxLength(64)]
    public string MatchId { get; set; } = null!;

    public int AlertsSent { get; set; }
}
