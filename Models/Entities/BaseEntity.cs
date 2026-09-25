using System.ComponentModel.DataAnnotations;

namespace BOT_Greg_v2_API.Models.Entities
{
    public abstract class BaseEntity
    {
        [Key, Required]
        public Guid Id { get; set; } = Guid.CreateVersion7();

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = null;
    }
}
