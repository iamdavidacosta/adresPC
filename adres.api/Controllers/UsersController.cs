using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using adres.api.Data;

namespace adres.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AdresAuthDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AdresAuthDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la lista de todos los usuarios disponibles (para el selector de login)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Select(u => new
                {
                    id = u.Id,
                    sub = u.Sub,
                    username = u.Username,
                    email = u.Email,
                    esRepresentanteLegal = u.EsRepresentanteLegal,
                    roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            _logger.LogInformation("Devolviendo {Count} usuarios", users.Count);

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios");
            return StatusCode(500, new { error = "Error al obtener usuarios" });
        }
    }
}
