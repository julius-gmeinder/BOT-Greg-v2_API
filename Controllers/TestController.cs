using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    // used by the docker healthcheck
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("pong");
    }
}
