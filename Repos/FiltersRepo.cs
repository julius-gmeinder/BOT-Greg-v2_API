using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BOT_Greg_v2_API.Repos;

public class FiltersRepo
{
    private readonly BotGregDbContext _db;

    public FiltersRepo(BotGregDbContext db)
    {
        _db = db;
    }

    public async Task<List<Filter>> GetFiltersAsync(string guildDiscordId)
    {
        return await _db.Filters.Where(x => x.Guild.DiscordId == guildDiscordId).OrderBy(x => x.CreatedAt).ToListAsync();
    }

    public async Task AddFilterAsync(Guild guild, FilterAddDto filterDto)
    {
        _db.Filters.Add(new Filter()
        {
            Team = filterDto.Team,
            Event = filterDto.Event,
            EventTier = filterDto.EventTier,
            VrsRank = filterDto.VrsRank,
            HltvCoverage = filterDto.HltvCoverage,
            RoleDiscordId = filterDto.RoleDiscordId,
            Guild = guild
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> RemoveFiltersAsync(FilterDeleteDto filterDto)
    {
        var filters = await _db.Filters
            .Where(x => x.Guild.DiscordId == filterDto.GuildDiscordId && filterDto.RoleDiscordIds.Contains(x.RoleDiscordId))
            .ToListAsync();

        var roleDiscordIdsToDelete = filters.Select(x => x.RoleDiscordId).ToList();

        // the roles get deleted, so their links would point to nothing
        var roleLinks = await _db.RoleLinks
            .Where(x => x.Guild.DiscordId == filterDto.GuildDiscordId && roleDiscordIdsToDelete.Contains(x.BotRoleDiscordId))
            .ToListAsync();

        _db.Filters.RemoveRange(filters);
        _db.RoleLinks.RemoveRange(roleLinks);

        await _db.SaveChangesAsync();

        return roleDiscordIdsToDelete;
    }
}
