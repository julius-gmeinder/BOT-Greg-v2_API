using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BOT_Greg_v2_API.Repos;

public class HistoryResultAlertsRepo
{
    private readonly BotGregDbContext _db;

    public HistoryResultAlertsRepo(BotGregDbContext db)
    {
        _db = db;
    }

    public async Task<List<HistoryResultAlert>> GetHistoryAsync(IEnumerable<string> matchIds)
    {
        return await _db.HistoryResultAlerts.Where(x => matchIds.Contains(x.MatchId)).ToListAsync();
    }

    public async Task AddBulkAsync(List<ResultAlertDto> alerts)
    {
        foreach (var alert in alerts)
        {
            _db.HistoryResultAlerts.Add(new HistoryResultAlert()
            {
                MatchId = alert.Result.MatchId,
                AlertsSent = alert.Guilds.Count
            });
        }

        await _db.SaveChangesAsync();
    }
}
