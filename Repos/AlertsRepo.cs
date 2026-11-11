using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BOT_Greg_v2_API.Repos;

public class AlertsRepo
{
    private readonly BotGregDbContext _db;

    public AlertsRepo(BotGregDbContext db)
    {
        _db = db;
    }

    public async Task<List<Guild>> GetMatchAlertGuildsAsync()
    {
        return await GuildsWithFiltersAndLinks()
            .Where(x => x.MatchAlertsChannelId != null && x.AlertRoleId != null && x.AlertsPaused == false)
            .ToListAsync();
    }

    public async Task<List<Guild>> GetResultAlertGuildsAsync()
    {
        return await GuildsWithFiltersAndLinks()
            .Where(x => x.ResultAlertsChannelId != null && x.AlertRoleId != null && x.AlertsPaused == false)
            .ToListAsync();
    }

    // loads filters and role links together with the guilds, instead of lazy loading them for every single guild
    private IQueryable<Guild> GuildsWithFiltersAndLinks()
    {
        return _db.Guilds
            .Include(x => x.FiltersMatRes)
            .Include(x => x.RoleLinks)
            .AsSplitQuery();
    }
}
