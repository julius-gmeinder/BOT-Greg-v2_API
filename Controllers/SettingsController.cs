using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.Entities;
using BOT_Greg_v2_API.Repos;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SettingsController : ControllerBase
{
    private readonly GuildsRepo _guilds;
    private readonly BotGregDbContext _db;

    public SettingsController(GuildsRepo guilds, BotGregDbContext db)
    {
        _guilds = guilds;
        _db = db;
    }

    [HttpPatch("ToggleMentionOnDelays")]
    public async Task<IActionResult> ToggleMentionOnDelaysAsync([FromBody] ToggleSettingDto toggleDto)
    {
        return await ToggleSettingAsync(toggleDto, guild => guild.MentionOnDelays, (guild, value) => guild.MentionOnDelays = value);
    }

    [HttpPatch("ToggleMentionOnResults")]
    public async Task<IActionResult> ToggleMentionOnResultsAsync([FromBody] ToggleSettingDto toggleDto)
    {
        return await ToggleSettingAsync(toggleDto, guild => guild.MentionOnResults, (guild, value) => guild.MentionOnResults = value);
    }

    [HttpPatch("ToggleCensorResults")]
    public async Task<IActionResult> ToggleCensorResultsAsync([FromBody] ToggleSettingDto toggleDto)
    {
        return await ToggleSettingAsync(toggleDto, guild => guild.CensorResults, (guild, value) => guild.CensorResults = value);
    }

    [HttpPatch("TogglePauseAlerts")]
    public async Task<IActionResult> TogglePauseAlertsAsync([FromBody] ToggleSettingDto toggleDto)
    {
        return await ToggleSettingAsync(toggleDto, guild => guild.AlertsPaused, (guild, value) => guild.AlertsPaused = value);
    }

    // all settings are toggled the same way, they only differ in the property of the guild that is read and set
    private async Task<IActionResult> ToggleSettingAsync(ToggleSettingDto toggleDto, Func<Guild, bool> getSetting, Action<Guild, bool> setSetting)
    {
        var guild = await _guilds.GetGuildAsync(toggleDto.GuildId);
        if (guild == null)
            return NotFound();

        if (toggleDto.NewValue == getSetting(guild))
            return Conflict();

        setSetting(guild, toggleDto.NewValue);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
