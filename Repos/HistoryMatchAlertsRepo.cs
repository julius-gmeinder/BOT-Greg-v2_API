using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BOT_Greg_v2_API.Repos;

public class HistoryMatchAlertsRepo
{
    private readonly BotGregDbContext _db;

    public HistoryMatchAlertsRepo(BotGregDbContext db)
    {
        _db = db;
    }

    public async Task<List<HistoryMatchAlert>> GetHistoryAsync(IEnumerable<string> matchIds)
    {
        return await _db.HistoryMatchAlerts.Where(x => matchIds.Contains(x.MatchId)).ToListAsync();
    }

    public async Task AddBulkAsync(List<MatchAlertDto> alerts)
    {
        foreach (var alert in alerts)
        {
            _db.HistoryMatchAlerts.Add(new HistoryMatchAlert()
            {
                MatchId = alert.Match.MatchId,
                DateTime = alert.Match.DateTime,
                AlertsSent = alert.Guilds.Count
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
