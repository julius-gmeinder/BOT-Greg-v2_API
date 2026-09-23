using BOT_Greg_v2_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LiquipediaController : ControllerBase
    {
        private readonly LiquipediaService _liquipedia;
        private readonly VrsService _vrs;

        public LiquipediaController(LiquipediaService liquipedia, VrsService vrs)
        {
            _liquipedia = liquipedia;
            _vrs = vrs;
        }

        [HttpGet("matches")]
        public async Task<IActionResult> GetMatchesAsync() {
            return Ok(await _liquipedia.GetMatchesAsync());
        }

        [HttpGet("results")]
        public async Task<IActionResult> GetResultsAsync() {
            return Ok(await _liquipedia.GetResultsAsync());
        }

        [HttpGet("events")]
        public async Task<IActionResult> GetEventsAsync()
        {
            return Ok(await _liquipedia.GetEventsAsync());
        }

        [HttpGet("teams")]
        public async Task<IActionResult> GetTeamsAsync()
        {
            return Ok(await _liquipedia.GetTeamsAsync());
        }

        [HttpGet("valve-vrs")]
        public async Task<IActionResult> GetValveVrsAsync()
        {
            return Ok(await _vrs.GetVrsTeamsAsync());
        }
    }
}
