using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using adres.api.Data;

namespace adres.api.Services;

/// <summary>
/// Transforma los claims del JWT agregando información de la base de datos local
/// </summary>
public class AdresClaimsTransformation : IClaimsTransformation
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdresClaimsTransformation> _logger;

    public AdresClaimsTransformation(
        IServiceProvider serviceProvider,
        ILogger<AdresClaimsTransformation> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Si no está autenticado, no hacemos nada
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        // Obtener el 'sub' del JWT
        var sub = principal.FindFirst("sub")?.Value 
               ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(sub))
        {
            return principal;
        }

        // Crear un nuevo scope para acceder al DbContext
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdresAuthDbContext>();

        try
        {
            // Buscar el usuario en la base de datos
            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Sub == sub);

            if (user == null)
            {
                _logger.LogDebug("Usuario con sub {Sub} no encontrado en BD - no se agregan claims adicionales", sub);
                return principal;
            }

            // Crear una nueva identidad clonada
            var identity = new ClaimsIdentity(principal.Identity);

            // Agregar claims personalizados desde la base de datos
            if (user.EsRepresentanteLegal)
            {
                identity.AddClaim(new Claim("esRepresentanteLegal", "true"));
                _logger.LogDebug("✅ Claim 'esRepresentanteLegal' agregado para {Username}", user.Username);
            }

            // Agregar otros claims útiles
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            }

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, user.FullName));
            }

            // Agregar roles desde la BD
            foreach (var userRole in user.UserRoles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, userRole.Role.Name));
            }

            // Agregar permisos como claims
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Key)
                .Distinct();

            foreach (var permission in permissions)
            {
                identity.AddClaim(new Claim("permission", permission));
            }

            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transformando claims para usuario {Sub}", sub);
            return principal;
        }
    }
}
