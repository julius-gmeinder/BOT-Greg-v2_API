using BOT_Greg_v2_API.Models.Entities;

namespace BOT_Greg_v2_API.Repos;

public class RolesRepo
{
    // all roles that are managed by the bot: the alert role and one role per filter
    public List<string> GetAllRoleDiscordIds(Guild guild)
    {
        List<string> roles = new();

        if (guild.AlertRoleDiscordId != null)
            roles.Add(guild.AlertRoleDiscordId);

        roles.AddRange(guild.Filters.OrderBy(x => x.CreatedAt).Select(x => x.RoleDiscordId));

        return roles;
    }

    public bool RoleExists(Guild guild, string roleDiscordId)
    {
        return GetAllRoleDiscordIds(guild).Contains(roleDiscordId);
    }
}
