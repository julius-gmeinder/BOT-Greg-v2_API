using BOT_Greg_v2_API.Models.Entities;

namespace BOT_Greg_v2_API.Repos;

public class RolesRepo
{
    // all roles that are managed by the bot: the alert role and one role per filter
    public List<string> GetAllRoleIds(Guild guild)
    {
        List<string> roles = new();

        if (guild.AlertRoleId != null)
            roles.Add(guild.AlertRoleId);

        roles.AddRange(guild.FiltersMatRes.OrderBy(x => x.CreatedAt).Select(x => x.RoleId));

        return roles;
    }

    public bool RoleExists(Guild guild, string roleId)
    {
        return GetAllRoleIds(guild).Contains(roleId);
    }
}
