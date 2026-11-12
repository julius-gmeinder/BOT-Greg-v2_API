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

    public async Task<Guild?> GetGuildAsync(string guildDiscordId)
    {
        return await _db.Guilds.FirstOrDefaultAsync(x => x.DiscordId == guildDiscordId);
    }

    public async Task AddGuildAsync(GuildDataDto guildDto)
    {
        _db.Guilds.Add(CreateGuild(guildDto));
        await _db.SaveChangesAsync();
    }

    public async Task RemoveGuildAsync(Guild guild)
    {
        // settings, filters and role links are removed by the database (cascade delete)
        _db.Guilds.Remove(guild);
        await _db.SaveChangesAsync();
    }

    public async Task ResetGuildAsync(Guild guild)
    {
        _db.Filters.RemoveRange(guild.Filters.ToList());
        _db.RoleLinks.RemoveRange(guild.RoleLinks.ToList());

        // a reset guild is in the same state as a guild that just got added
        _db.Filters.Add(CreateDefaultFilter(guild));

        guild.AlertRoleDiscordId = null;

        // alerts
        guild.MatchAlertsChannelDiscordId = null;
        guild.ResultAlertsChannelDiscordId = null;

        // settings
        guild.Settings.AlertsPaused = false;
        guild.Settings.MentionOnDelays = false;
        guild.Settings.MentionOnResults = true;
        guild.Settings.CensorResults = false;
        guild.Settings.RoleManagement = false;

        await _db.SaveChangesAsync();
    }

    // adds new guilds, updates existing guilds and removes guilds that are not part of the list anymore
    public async Task<List<GuildDataDto>> SyncGuildsAsync(List<GuildDataDto> allGuilds)
    {
        List<GuildDataDto> guildsAdded = new();

        var guildsInDb = (await GetGuildsAsync()).ToDictionary(x => x.DiscordId);

        foreach (var guildDto in allGuilds.DistinctBy(x => x.GuildDiscordId))
        {
            // .Remove() returns true if the guild was in the dictionary, so only the guilds to delete are left over at the end
            if (guildsInDb.Remove(guildDto.GuildDiscordId, out var existingGuild))
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
            MatchAlertChannelDiscordId = guild.MatchAlertsChannelDiscordId,
            ResultAlertChannelDiscordId = guild.ResultAlertsChannelDiscordId,
            AlertRoleDiscordId = guild.AlertRoleDiscordId
        };
    }

    public async Task<AlertConfigDto> SetGuildsConfigAsync(Guild guild, AlertConfigDto configDto)
    {
        guild.MatchAlertsChannelDiscordId = configDto.MatchAlertChannelDiscordId;
        guild.ResultAlertsChannelDiscordId = configDto.ResultAlertChannelDiscordId;

        // the alert role can only be replaced, not removed (it gets removed when the role management is disabled)
        // the controller makes sure, that it is only set if the role management is enabled
        if (configDto.AlertRoleDiscordId != null)
            guild.AlertRoleDiscordId = configDto.AlertRoleDiscordId;

        await _db.SaveChangesAsync();
        return GetGuildsConfig(guild);
    }

    public GuildSettingsDto GetGuildsSettings(Guild guild)
    {
        return new GuildSettingsDto()
        {
            AlertsPaused = guild.Settings.AlertsPaused,
            MentionOnDelays = guild.Settings.MentionOnDelays,
            MentionOnResults = guild.Settings.MentionOnResults,
            CensorResults = guild.Settings.CensorResults,
            RoleManagement = guild.Settings.RoleManagement
        };
    }

    private Guild CreateGuild(GuildDataDto guildDto)
    {
        Guild guild = new()
        {
            DiscordId = guildDto.GuildDiscordId,
            GuildName = guildDto.GuildName,
            GuildMemberCount = guildDto.GuildMemberCount,
            Settings = new GuildSettings()
        };

        guild.Filters.Add(CreateDefaultFilter(guild));

        return guild;
    }

    // liquipedia covers a lot more matches than hltv did, so every new guild starts with a filter for the matches that are also covered by hltv
    // it is a normal filter, that can be deleted like every other filter (a guild without filters gets all matches)
    // it has no role, because the role management of a new guild is disabled
    private Filter CreateDefaultFilter(Guild guild)
    {
        return new Filter()
        {
            HltvCoverage = true,
            Guild = guild
        };
    }
}
