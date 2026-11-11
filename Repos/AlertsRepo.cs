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
        return await GuildsWithSettingsFiltersAndLinks()
            .Where(x => x.MatchAlertsChannelDiscordId != null && x.AlertRoleDiscordId != null && x.Settings.AlertsPaused == false)
            .ToListAsync();
    }

    public async Task<List<Guild>> GetResultAlertGuildsAsync()
    {
        return await GuildsWithSettingsFiltersAndLinks()
            .Where(x => x.ResultAlertsChannelDiscordId != null && x.AlertRoleDiscordId != null && x.Settings.AlertsPaused == false)
            .ToListAsync();
    }

    // loads settings, filters and role links together with the guilds, instead of lazy loading them for every single guild
    private IQueryable<Guild> GuildsWithSettingsFiltersAndLinks()
    {
        return _db.Guilds
            .Include(x => x.Settings)
            .Include(x => x.Filters)
            .Include(x => x.RoleLinks)
            .AsSplitQuery();
    }
}
