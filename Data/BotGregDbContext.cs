using BOT_Greg_v2_API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BOT_Greg_v2_API.Data
{
    public class BotGregDbContext : DbContext
    {
        public BotGregDbContext(DbContextOptions<BotGregDbContext> options) : base(options)
        {

        }

        public DbSet<Guild> Guilds { get; set; }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        // auto updates UpdatedAt (and CreatedAt) timestamp
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker
                .Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            var utcNow = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = null;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Entity.UpdatedAt = utcNow;
                }
            }
        }
    }
}
