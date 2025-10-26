using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using adres.api.Services;
using adres.api.Models;
using System.Security.Claims;

namespace adres.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IAdresAuthService _adresAuthService;

    public AuthController(
        IConfiguration configuration, 
        ILogger<AuthController> logger,
        IAdresAuthService adresAuthService)
    {
        _configuration = configuration;
        _logger = logger;
        _adresAuthService = adresAuthService;
    }

    /// <summary>
    /// Endpoint de inicio de sesión - Redirige al autenticador externo
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        var authAuthority = _configuration["AUTH_AUTHORITY"];
        var authAudience = _configuration["AUTH_AUDIENCE"];
        var callbackUrl = _configuration["AUTH_CALLBACK_URL"];
        
        // Construir URL de autenticación del proveedor externo
        var authUrl = $"{authAuthority}/oauth/authorize?" +
                      $"client_id={authAudience}&" +
                      $"response_type=code&" +
                      $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
                      $"state={Uri.EscapeDataString(returnUrl ?? "/")}";

        _logger.LogInformation("Redirigiendo a autenticación externa: {AuthUrl}", authUrl);

        return Redirect(authUrl);
    }

    /// <summary>
    /// Callback después de autenticación exitosa
    /// URL a proporcionar al autenticador externo
    /// </summary>
    [HttpGet("callback")]
    public IActionResult Callback([FromQuery] string code, [FromQuery] string? state = null)
    {
        _logger.LogInformation("Callback recibido con código: {Code}", code);

        // Aquí procesarías el código de autorización con el autenticador externo
        // Por ahora, redirigimos al frontend con el código
        
        var frontendUrl = _configuration["FRONTEND_URL_PRODUCTION"] 
                         ?? _configuration["FRONTEND_URL_STAGING"]
                         ?? "http://localhost:3000";
        
        var redirectUrl = $"{frontendUrl}/auth/callback?code={code}&state={Uri.EscapeDataString(state ?? "")}";
        
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Endpoint de cierre de sesión - Redirige al autenticador externo para logout
    /// </summary>
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        var authAuthority = _configuration["AUTH_AUTHORITY"];
        var logoutRedirectUrl = _configuration["AUTH_LOGOUT_REDIRECT_URL"];
        
        // Construir URL de logout del proveedor externo
        var logoutUrl = $"{authAuthority}/oauth/logout?" +
                        $"post_logout_redirect_uri={Uri.EscapeDataString(logoutRedirectUrl)}";

        _logger.LogInformation("Redirigiendo a logout externo: {LogoutUrl}", logoutUrl);

        return Redirect(logoutUrl);
    }

    /// <summary>
    /// Obtiene las URLs de configuración para el frontend
    /// </summary>
    [HttpGet("config")]
    public IActionResult GetAuthConfig()
    {
        var config = new
        {
            loginUrl = $"{Request.Scheme}://{Request.Host}/api/Auth/login",
            logoutUrl = $"{Request.Scheme}://{Request.Host}/api/Auth/logout",
            callbackUrl = _configuration["AUTH_CALLBACK_URL"],
            authority = _configuration["AUTH_AUTHORITY"]
        };

        return Ok(config);
    }

    /// <summary>
    /// Manejo de errores de autenticación
    /// </summary>
    [HttpGet("error")]
    public IActionResult Error([FromQuery] string? error = null, [FromQuery] string? error_description = null)
    {
        _logger.LogError("Error de autenticación: {Error} - {Description}", error, error_description);

        var errorRedirectUrl = _configuration["AUTH_ERROR_REDIRECT_URL"];
        var redirectUrl = $"{errorRedirectUrl}?error={Uri.EscapeDataString(error ?? "unknown")}" +
                         $"&error_description={Uri.EscapeDataString(error_description ?? "")}";

        return Redirect(redirectUrl);
    }
}
