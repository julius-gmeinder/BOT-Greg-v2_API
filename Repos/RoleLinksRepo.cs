using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BOT_Greg_v2_API.Repos;

public class RoleLinksRepo
{
    private readonly BotGregDbContext _db;
    private readonly RolesRepo _roles;

    public RoleLinksRepo(BotGregDbContext db, RolesRepo roles)
    {
        _db = db;
        _roles = roles;
    }

    public async Task<List<RoleLink>> GetRoleLinksAsync(string guildId)
    {
        return await _db.RoleLinks.Where(x => x.Guild.GuildId == guildId).ToListAsync();
    }

    public async Task<RoleLink?> GetRoleLinkAsync(string botRoleId, string linkedRoleId)
    {
        return await _db.RoleLinks.FirstOrDefaultAsync(x => x.BotRoleId == botRoleId && x.LinkedRoleId == linkedRoleId);
    }

    public async Task LinkRolesAsync(Guild guild, RoleLinkAddDto linkDto)
    {
        _db.RoleLinks.Add(new RoleLink()
        {
            BotRoleId = linkDto.BotRoleId,
            LinkedRoleId = linkDto.LinkedRoleId,
            Guild = guild
        });

        await _db.SaveChangesAsync();
    }

    public async Task UnlinkRolesAsync(RoleLink link)
    {
        _db.RoleLinks.Remove(link);
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetLinkedRoleIdsAsync(string botRoleId)
    {
        return await _db.RoleLinks.Where(x => x.BotRoleId == botRoleId).Select(x => x.LinkedRoleId).ToListAsync();
    }

    public List<string> GetDistinctLinkedBotRoleIds(Guild guild)
    {
        return guild.RoleLinks.Select(x => x.BotRoleId).Distinct().ToList();
    }

    public List<RoleLinksTreeItemDto> GetRoleLinksTree(Guild guild)
    {
        List<RoleLinksTreeItemDto> tree = new();

        foreach (var botRoleId in _roles.GetAllRoleIds(guild))
        {
            tree.Add(new RoleLinksTreeItemDto()
            {
                BotRoleId = botRoleId,
                LinkedRoleIds = guild.RoleLinks.Where(x => x.BotRoleId == botRoleId).Select(x => x.LinkedRoleId).ToList()
            });
        }

        return tree;
    }
}
