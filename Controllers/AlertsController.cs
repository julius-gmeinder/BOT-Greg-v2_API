using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Repos;
using BOT_Greg_v2_API.Services.Alerts;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AlertsController : ControllerBase
{
    private readonly GuildsRepo _guilds;
    private readonly AlertsHelper _alertsHelper;
    private readonly AlertStreamService _stream;

    public AlertsController(GuildsRepo guilds, AlertsHelper alertsHelper, AlertStreamService stream)
    {
        _guilds = guilds;
        _alertsHelper = alertsHelper;
        _stream = stream;
    }

    // server-sent-events stream, that sends match alerts, match delays and result alerts to the bot
    [HttpGet("stream")]
    public IResult GetAlertStream(CancellationToken cancellationToken)
    {
        return TypedResults.ServerSentEvents(_stream.SubscribeAsync(cancellationToken));
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetGuildsConfigAsync([FromQuery] string guildId)
    {
        var guild = await _guilds.GetGuildAsync(guildId);
        if (guild == null)
            return NotFound();

        return Ok(_guilds.GetGuildsConfig(guild));
    }

    [HttpPost("config")]
    public async Task<IActionResult> SetGuildsConfigAsync([FromQuery] string guildId, [FromBody] AlertConfigDto configDto)
    {
        var guild = await _guilds.GetGuildAsync(guildId);
        if (guild == null)
            return NotFound();

        AlertConfigResDto res = new()
        {
            NewConfig = await _guilds.SetGuildsConfigAsync(guild, configDto),
            FilterCount = guild.Filters.Count,
            GuildsNextUpcomingMatch = await _alertsHelper.GetGuildsNextMatchAsync(guild)
        };

        return Ok(res);
    }
}
