using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Repos;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GuildsController : ControllerBase
{
    private readonly GuildsRepo _guilds;
    private readonly RolesRepo _roles;

    public GuildsController(GuildsRepo guilds, RolesRepo roles)
    {
        _guilds = guilds;
        _roles = roles;
    }

    [HttpPost("")]
    public async Task<IActionResult> AddAsync([FromBody] GuildDataDto guildDto)
    {
        var existingGuild = await _guilds.GetGuildAsync(guildDto.GuildDiscordId);
        if (existingGuild != null)
            return Conflict();

        await _guilds.AddGuildAsync(guildDto);
        return NoContent();
    }

    [HttpDelete("")]
    public async Task<IActionResult> DeleteAsync([FromQuery] string guildDiscordId)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild == null)
            return NotFound();

        await _guilds.RemoveGuildAsync(guild);
        return NoContent();
    }

    [HttpPut("reset")]
    public async Task<IActionResult> ResetAsync([FromQuery] string guildDiscordId)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild == null)
            return NotFound();

        var rolesToDelete = _roles.GetAllRoleDiscordIds(guild);

        await _guilds.ResetGuildAsync(guild);

        return Ok(rolesToDelete);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncGuildsAsync([FromBody] List<GuildDataDto> allGuilds)
    {
        var guildsAdded = await _guilds.SyncGuildsAsync(allGuilds);
        return Ok(guildsAdded);
    }
}
