using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.Entities;

namespace BOT_Greg_v2_API.Repos;

public class RolesRepo
{
    private readonly BotGregDbContext _db;

    public RolesRepo(BotGregDbContext db)
    {
        _db = db;
    }

    // all roles that are managed by the bot: the alert role and one role per filter
    // always empty if the role management of the guild is disabled
    public List<string> GetAllRoleDiscordIds(Guild guild)
    {
        List<string> roles = new();

        if (guild.AlertRoleDiscordId != null)
            roles.Add(guild.AlertRoleDiscordId);

        foreach (var filter in guild.Filters.OrderBy(x => x.CreatedAt))
        {
            if (filter.RoleDiscordId != null)
                roles.Add(filter.RoleDiscordId);
        }

        return roles;
    }

    public bool RoleExists(Guild guild, string roleDiscordId)
    {
        return GetAllRoleDiscordIds(guild).Contains(roleDiscordId);
    }

    // the roles were already created in discord by the bot, the api only saves which role belongs to what
    // does not save, the caller saves it together with the other settings
    public void EnableRoleManagement(Guild guild, string alertRoleDiscordId, List<FilterRoleDto> filterRoles)
    {
        guild.AlertRoleDiscordId = alertRoleDiscordId;

        foreach (var filter in guild.Filters)
        {
            var filterRole = filterRoles.First(x => x.FilterId == filter.Id);
            filter.RoleDiscordId = filterRole.RoleDiscordId;
        }

        guild.Settings.RoleManagement = true;
    }

    // returns the roles that are no longer needed, the bot has to delete them in discord
    // does not save, the caller saves it together with the other settings
    public List<string> DisableRoleManagement(Guild guild)
    {
        var roleDiscordIdsToDelete = GetAllRoleDiscordIds(guild).Distinct().ToList();

        guild.AlertRoleDiscordId = null;

        foreach (var filter in guild.Filters)
            filter.RoleDiscordId = null;

        // the roles get deleted, so their links would point to nothing
        _db.RoleLinks.RemoveRange(guild.RoleLinks.ToList());

        guild.Settings.RoleManagement = false;

        return roleDiscordIdsToDelete;
    }
}
