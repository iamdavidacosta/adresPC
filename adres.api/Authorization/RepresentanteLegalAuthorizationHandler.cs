using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using adres.api.Data;
using System.Security.Claims;

namespace adres.api.Authorization;

/// <summary>
/// Requirement para validar que el usuario sea Representante Legal
/// </summary>
public class RepresentanteLegalRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Handler que valida contra la base de datos si el usuario es RL
/// </summary>
public class RepresentanteLegalAuthorizationHandler : AuthorizationHandler<RepresentanteLegalRequirement>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RepresentanteLegalAuthorizationHandler> _logger;

    public RepresentanteLegalAuthorizationHandler(
        IServiceProvider serviceProvider,
        ILogger<RepresentanteLegalAuthorizationHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RepresentanteLegalRequirement requirement)
    {
        // Obtener el 'sub' del JWT
        var sub = context.User.FindFirst("sub")?.Value 
               ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(sub))
        {
            _logger.LogWarning("⚠️ No se encontró 'sub' en el token");
            context.Fail();
            return;
        }

        // Crear scope para acceder al DbContext
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdresAuthDbContext>();

        try
        {
            // Buscar usuario en la BD
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Sub == sub);

            if (user == null)
            {
                _logger.LogWarning("⚠️ Usuario con sub {Sub} no encontrado en BD", sub);
                context.Fail();
                return;
            }

            if (!user.EsRepresentanteLegal)
            {
                _logger.LogWarning("⚠️ Usuario {Username} NO es Representante Legal", user.Username);
                context.Fail();
                return;
            }

            _logger.LogInformation("✅ Usuario {Username} es Representante Legal - acceso permitido", user.Username);
            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error validando Representante Legal para sub {Sub}", sub);
            context.Fail();
        }
    }
}
