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

    public async Task<List<RoleLink>> GetRoleLinksAsync(string guildDiscordId)
    {
        return await _db.RoleLinks.Where(x => x.Guild.DiscordId == guildDiscordId).ToListAsync();
    }

    public async Task<RoleLink?> GetRoleLinkAsync(string botRoleDiscordId, string linkedRoleDiscordId)
    {
        return await _db.RoleLinks.FirstOrDefaultAsync(x => x.BotRoleDiscordId == botRoleDiscordId && x.LinkedRoleDiscordId == linkedRoleDiscordId);
    }

    public async Task LinkRolesAsync(Guild guild, RoleLinkAddDto linkDto)
    {
        _db.RoleLinks.Add(new RoleLink()
        {
            BotRoleDiscordId = linkDto.BotRoleDiscordId,
            LinkedRoleDiscordId = linkDto.LinkedRoleDiscordId,
            Guild = guild
        });

        await _db.SaveChangesAsync();
    }

    public async Task UnlinkRolesAsync(RoleLink link)
    {
        _db.RoleLinks.Remove(link);
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetLinkedRoleDiscordIdsAsync(string botRoleDiscordId)
    {
        return await _db.RoleLinks.Where(x => x.BotRoleDiscordId == botRoleDiscordId).Select(x => x.LinkedRoleDiscordId).ToListAsync();
    }

    public List<string> GetDistinctLinkedBotRoleDiscordIds(Guild guild)
    {
        return guild.RoleLinks.Select(x => x.BotRoleDiscordId).Distinct().ToList();
    }

    public List<RoleLinksTreeItemDto> GetRoleLinksTree(Guild guild)
    {
        List<RoleLinksTreeItemDto> tree = new();

        foreach (var botRoleDiscordId in _roles.GetAllRoleDiscordIds(guild))
        {
            tree.Add(new RoleLinksTreeItemDto()
            {
                BotRoleDiscordId = botRoleDiscordId,
                LinkedRoleDiscordIds = guild.RoleLinks.Where(x => x.BotRoleDiscordId == botRoleDiscordId).Select(x => x.LinkedRoleDiscordId).ToList()
            });
        }

        return tree;
    }
}
