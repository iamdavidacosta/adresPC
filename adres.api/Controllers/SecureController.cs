using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace adres.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    private readonly ILogger<SecureController> _logger;

    public SecureController(ILogger<SecureController> logger)
    {
        _logger = logger;
    }

    [HttpGet("solo-rl")]
    [Authorize(Policy = "SoloRepresentanteLegal")]
    public IActionResult SoloRepresentanteLegal()
    {
        var username = User.Identity?.Name ?? "Anónimo";
        _logger.LogInformation("Acceso permitido a solo-rl para usuario: {Username}", username);

        return Ok(new
        {
            message = "✅ Acceso permitido. Usted es un representante legal.",
            timestamp = DateTime.UtcNow
        });
    }
}
