using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using adres.api.Services;
using System.Security.Claims;

namespace adres.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IUserDirectory _userDirectory;
    private readonly ILogger<MeController> _logger;

    public MeController(IUserDirectory userDirectory, ILogger<MeController> logger)
    {
        _userDirectory = userDirectory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Log de todos los claims recibidos
            _logger.LogInformation("=== CLAIMS RECIBIDOS DEL TOKEN ===");
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation($"  {claim.Type}: {claim.Value}");
            }
            _logger.LogInformation("=================================");
            
            // Extraer sub y email del token JWT
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;

            var email = User.FindFirst(ClaimTypes.Email)?.Value
                        ?? User.FindFirst("email")?.Value
                        ?? User.FindFirst("upn")?.Value
                        ?? User.FindFirst("preferred_username")?.Value;

            _logger.LogInformation("MeController: sub={Sub}, email={Email}", sub, email);

            // Buscar usuario en base de datos local
            var userInfo = await _userDirectory.FindBySubOrEmailAsync(sub, email);

            if (userInfo == null)
            {
                return NotFound(new { message = "Usuario no encontrado en el directorio local" });
            }

            // Extraer todos los claims del token
            var claims = User.Claims.ToDictionary(
                c => c.Type,
                c => c.Value
            );

            // Construir respuesta
            var response = new
            {
                sub = userInfo.Sub,
                username = userInfo.Username,
                email = userInfo.Email,
                esRepresentanteLegal = userInfo.EsRepresentanteLegal,
                roles = userInfo.Roles,
                permissions = userInfo.Permissions,
                claims = claims
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener información del usuario");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}
