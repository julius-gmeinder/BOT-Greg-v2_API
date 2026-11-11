using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BOT_Greg_v2_API.Repos;

public class GuildsRepo
{
    private readonly BotGregDbContext _db;

    public GuildsRepo(BotGregDbContext db)
    {
        _db = db;
    }

    public async Task<List<Guild>> GetGuildsAsync()
    {
        return await _db.Guilds.ToListAsync();
    }

    public async Task<Guild?> GetGuildAsync(string guildId)
    {
        return await _db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guildId);
    }

    public async Task AddGuildAsync(GuildDataDto guildDto)
    {
        _db.Guilds.Add(CreateGuild(guildDto));
        await _db.SaveChangesAsync();
    }

    public async Task RemoveGuildAsync(Guild guild)
    {
        // filters and role links are removed by the database (cascade delete)
        _db.Guilds.Remove(guild);
        await _db.SaveChangesAsync();
    }

    public async Task ResetGuildAsync(Guild guild)
    {
        _db.Filters.RemoveRange(guild.Filters.ToList());
        _db.RoleLinks.RemoveRange(guild.RoleLinks.ToList());

        guild.AlertRoleId = null;

        // alerts
        guild.MatchAlertsChannelId = null;
        guild.ResultAlertsChannelId = null;

        // settings
        guild.AlertsPaused = false;
        guild.MentionOnDelays = false;
        guild.MentionOnResults = true;
        guild.CensorResults = false;

        await _db.SaveChangesAsync();
    }

    // adds new guilds, updates existing guilds and removes guilds that are not part of the list anymore
    public async Task<List<GuildDataDto>> SyncGuildsAsync(List<GuildDataDto> allGuilds)
    {
        List<GuildDataDto> guildsAdded = new();

        var guildsInDb = (await GetGuildsAsync()).ToDictionary(x => x.GuildId);

        foreach (var guildDto in allGuilds.DistinctBy(x => x.GuildId))
        {
            // .Remove() returns true if the guild was in the dictionary, so only the guilds to delete are left over at the end
            if (guildsInDb.Remove(guildDto.GuildId, out var existingGuild))
            {
                existingGuild.GuildName = guildDto.GuildName;
                existingGuild.GuildMemberCount = guildDto.GuildMemberCount;
            }
            else
            {
                _db.Guilds.Add(CreateGuild(guildDto));
                guildsAdded.Add(guildDto);
            }
        }

        _db.Guilds.RemoveRange(guildsInDb.Values);

        await _db.SaveChangesAsync();
        return guildsAdded;
    }

    public AlertConfigDto GetGuildsConfig(Guild guild)
    {
        return new AlertConfigDto()
        {
            MatchAlertChannelId = guild.MatchAlertsChannelId,
            ResultAlertChannelId = guild.ResultAlertsChannelId,
            AlertRoleId = guild.AlertRoleId
        };
    }

    public async Task<AlertConfigDto> SetGuildsConfigAsync(Guild guild, AlertConfigDto configDto)
    {
        guild.MatchAlertsChannelId = configDto.MatchAlertChannelId;
        guild.ResultAlertsChannelId = configDto.ResultAlertChannelId;

        // the alert role can only be replaced, not removed
        if (configDto.AlertRoleId != null)
            guild.AlertRoleId = configDto.AlertRoleId;

        await _db.SaveChangesAsync();
        return GetGuildsConfig(guild);
    }

    private Guild CreateGuild(GuildDataDto guildDto)
    {
        return new Guild()
        {
            GuildId = guildDto.GuildId,
            GuildName = guildDto.GuildName,
            GuildMemberCount = guildDto.GuildMemberCount,
        };
    }
}
