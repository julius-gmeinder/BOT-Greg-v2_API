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

    public async Task<List<Filter>> GetFiltersAsync(string guildId)
    {
        return await _db.Filters.Where(x => x.Guild.GuildId == guildId).OrderBy(x => x.CreatedAt).ToListAsync();
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
            RoleId = filterDto.RoleId,
            Guild = guild
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> RemoveFiltersAsync(FilterDeleteDto filterDto)
    {
        var filters = await _db.Filters
            .Where(x => x.Guild.GuildId == filterDto.GuildId && filterDto.RoleIds.Contains(x.RoleId))
            .ToListAsync();

        var roleIdsToDelete = filters.Select(x => x.RoleId).ToList();

        // the roles get deleted, so their links would point to nothing
        var roleLinks = await _db.RoleLinks
            .Where(x => x.Guild.GuildId == filterDto.GuildId && roleIdsToDelete.Contains(x.BotRoleId))
            .ToListAsync();

        _db.Filters.RemoveRange(filters);
        _db.RoleLinks.RemoveRange(roleLinks);

        await _db.SaveChangesAsync();

        return roleIdsToDelete;
    }
}
