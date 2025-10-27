using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using adres.api.Services;
using adres.api.Models;
using System.Security.Claims;
using System.Text;

namespace adres.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdresAuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdresAuthController> _logger;
    private readonly IAdresAuthService _adresAuthService;

    public AdresAuthController(
        IConfiguration configuration, 
        ILogger<AdresAuthController> logger,
        IAdresAuthService adresAuthService)
    {
        _configuration = configuration;
        _logger = logger;
        _adresAuthService = adresAuthService;
    }

    /// <summary>
    /// Inicia el flujo de Authorization Code - Redirige a Autentic Sign
    /// GET /api/AdresAuth/authorize
    /// </summary>
    [HttpGet("authorize")]
    [AllowAnonymous]
    public IActionResult Authorize([FromQuery] string? returnUrl = null)
    {
        var redirectUri = _configuration["AdresAuth:RedirectUri"] ?? "https://adres-autenticacion.centralspike.com/auth/callback";
        
        // Generar PKCE UNA SOLA VEZ
        var (authUrlWithoutState, codeVerifier) = _adresAuthService.GetAuthorizationUrl(redirectUri, "");
        
        // Incluir code_verifier en el state para que el frontend lo reciba
        var stateData = new
        {
            returnUrl = returnUrl ?? "/",
            cv = codeVerifier // code_verifier
        };
        
        var stateJson = System.Text.Json.JsonSerializer.Serialize(stateData);
        var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(stateJson));
        
        // Agregar el state a la URL (sin regenerar el code_verifier)
        var authUrl = authUrlWithoutState.Contains("?") 
            ? $"{authUrlWithoutState}&state={Uri.EscapeDataString(state)}"
            : $"{authUrlWithoutState}?state={Uri.EscapeDataString(state)}";
        
        _logger.LogInformation("🔄 Redirigiendo a Autentic Sign con PKCE");
        _logger.LogInformation("  📍 Code Verifier (primeros 10): {CV}...", codeVerifier.Substring(0, Math.Min(10, codeVerifier.Length)));

        return Redirect(authUrl);
    }

    /// <summary>
    /// Callback del flujo Authorization Code - Intercambia el código por un token
    /// GET /api/AdresAuth/callback
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? state = null, [FromQuery] bool json = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { error = "invalid_request", error_description = "Authorization code is required" });
            }

            _logger.LogInformation("📥 Callback recibido con código de autorización");

            // Recuperar code_verifier de la sesión
            var codeVerifier = HttpContext.Session.GetString("pkce_code_verifier");
            if (string.IsNullOrWhiteSpace(codeVerifier))
            {
                return BadRequest(new { error = "invalid_request", error_description = "PKCE code verifier not found in session" });
            }

            var redirectUri = _configuration["AdresAuth:RedirectUri"] 
                            ?? $"{Request.Scheme}://{Request.Host}/api/AdresAuth/callback";

            var authResponse = await _adresAuthService.ExchangeCodeForTokenAsync(code, redirectUri, codeVerifier);
            
            // Limpiar code_verifier de la sesión
            HttpContext.Session.Remove("pkce_code_verifier");

            _logger.LogInformation("✅ Token obtenido exitosamente");

            // Si se solicita respuesta JSON (para testing)
            if (json)
            {
                return Ok(new
                {
                    access_token = authResponse.AccessToken,
                    token_type = authResponse.TokenType,
                    expires_in = authResponse.ExpiresIn,
                    refresh_token = authResponse.RefreshToken,
                    scope = authResponse.Scope,
                    message = "✅ Autenticación exitosa! Guarda el access_token para usarlo en tus requests."
                });
            }

            // Decodificar el state para obtener la URL de retorno
            var returnUrl = "/";
            if (!string.IsNullOrWhiteSpace(state))
            {
                try
                {
                    returnUrl = Encoding.UTF8.GetString(Convert.FromBase64String(state));
                }
                catch
                {
                    returnUrl = "/";
                }
            }

            // Redirigir al frontend con el token
            var frontendUrl = _configuration["AdresAuth:FrontendUrl"] 
                            ?? "https://adres-autenticacion.centralspike.com";
            
            var redirectUrl = $"{frontendUrl}{returnUrl}?access_token={authResponse.AccessToken}";
            
            if (!string.IsNullOrWhiteSpace(authResponse.RefreshToken))
            {
                redirectUrl += $"&refresh_token={authResponse.RefreshToken}";
                redirectUrl += $"&expires_in={authResponse.ExpiresIn}";
            }

            _logger.LogInformation("🔄 Redirigiendo al frontend: {FrontendUrl}", frontendUrl);

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error procesando callback de autorización");
            return BadRequest(new { error = "server_error", error_description = ex.Message });
        }
    }

    /// <summary>
    /// Intercambia un código de autorización por tokens (para llamadas directas desde el frontend)
    /// POST /api/AdresAuth/token
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> ExchangeCode([FromBody] ExchangeCodeRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { error = "invalid_request", error_description = "Authorization code is required" });
            }

            if (string.IsNullOrWhiteSpace(request.CodeVerifier))
            {
                return BadRequest(new { error = "invalid_request", error_description = "PKCE code verifier is required" });
            }

            var redirectUri = request.RedirectUri 
                            ?? _configuration["AdresAuth:RedirectUri"]
                            ?? $"{Request.Scheme}://{Request.Host}/auth/callback";

            var authResponse = await _adresAuthService.ExchangeCodeForTokenAsync(request.Code, redirectUri, request.CodeVerifier);

            return Ok(new
            {
                access_token = authResponse.AccessToken,
                token_type = authResponse.TokenType,
                expires_in = authResponse.ExpiresIn,
                refresh_token = authResponse.RefreshToken,
                id_token = authResponse.IdToken,
                scope = authResponse.Scope
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = "invalid_grant", error_description = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error intercambiando código por token");
            return StatusCode(500, new { error = "server_error", error_description = "Internal server error" });
        }
    }

    /// <summary>
    /// Endpoint de login con credenciales - Integración ADRES (DEPRECADO - usar Authorization Code)
    /// POST /api/AdresAuth/login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdresLoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "invalid_request", error_description = "Username and password are required" });
            }

            _logger.LogInformation("Intentando autenticación ADRES para usuario: {Username}", request.Username);

            var authResponse = await _adresAuthService.AuthenticateAsync(request.Username, request.Password);

            _logger.LogInformation("✅ Autenticación ADRES exitosa para usuario: {Username}", request.Username);

            return Ok(new
            {
                access_token = authResponse.AccessToken,
                token_type = authResponse.TokenType,
                expires_in = authResponse.ExpiresIn,
                refresh_token = authResponse.RefreshToken,
                scope = authResponse.Scope
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("❌ Autenticación ADRES fallida para usuario {Username}: {Error}", request.Username, ex.Message);
            return Unauthorized(new { error = "invalid_grant", error_description = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en autenticación ADRES para usuario {Username}", request.Username);
            return StatusCode(500, new { error = "server_error", error_description = "Internal server error" });
        }
    }

    /// <summary>
    /// Endpoint para refrescar el token de acceso
    /// POST /api/AdresAuth/refresh
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { error = "invalid_request", error_description = "Refresh token is required" });
            }

            var authResponse = await _adresAuthService.RefreshTokenAsync(request.RefreshToken);

            return Ok(new
            {
                access_token = authResponse.AccessToken,
                token_type = authResponse.TokenType,
                expires_in = authResponse.ExpiresIn,
                refresh_token = authResponse.RefreshToken
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = "invalid_grant", error_description = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refrescando token ADRES");
            return StatusCode(500, new { error = "server_error", error_description = "Internal server error" });
        }
    }

    /// <summary>
    /// Endpoint para revocar un token
    /// POST /api/AdresAuth/revoke
    /// </summary>
    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { error = "invalid_request", error_description = "Token is required" });
            }

            var revoked = await _adresAuthService.RevokeTokenAsync(request.Token);

            if (revoked)
            {
                return Ok(new { message = "Token revoked successfully" });
            }

            return BadRequest(new { error = "invalid_request", error_description = "Failed to revoke token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revocando token ADRES");
            return StatusCode(500, new { error = "server_error", error_description = "Internal server error" });
        }
    }

    /// <summary>
    /// Endpoint de logout - Revoca el token y cierra la sesión
    /// POST /api/AdresAuth/logout
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // Obtener el token del header Authorization
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (!string.IsNullOrWhiteSpace(token))
            {
                await _adresAuthService.RevokeTokenAsync(token);
            }

            _logger.LogInformation("Usuario {Username} cerró sesión", User.Identity?.Name ?? "Unknown");

            return Ok(new { message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en logout ADRES");
            return StatusCode(500, new { error = "server_error", error_description = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtiene las claves públicas JWKS para validar tokens
    /// GET /api/AdresAuth/jwks
    /// </summary>
    [HttpGet("jwks")]
    [AllowAnonymous]
    public async Task<IActionResult> GetJwks()
    {
        try
        {
            var jwks = await _adresAuthService.GetJwksAsync();
            return Ok(jwks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo JWKS");
            return StatusCode(500, new { error = "server_error", error_description = "Unable to retrieve JWKS" });
        }
    }

    /// <summary>
    /// Valida el token actual y devuelve información del usuario
    /// GET /api/AdresAuth/validate
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public async Task<IActionResult> ValidateToken()
    {
        try
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "invalid_token", error_description = "No token provided" });
            }

            var claims = await _adresAuthService.ValidateTokenAsync(token);

            return Ok(new
            {
                valid = true,
                username = claims.PreferredUsername,
                email = claims.Email,
                name = claims.Name,
                roles = claims.Roles,
                permissions = claims.Permissions,
                expires_at = DateTimeOffset.FromUnixTimeSeconds(claims.ExpiresAt).DateTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando token ADRES");
            return Unauthorized(new { error = "invalid_token", error_description = "Token validation failed" });
        }
    }

    /// <summary>
    /// Obtiene las URLs de configuración para el frontend
    /// GET /api/AdresAuth/config
    /// </summary>
    [HttpGet("config")]
    [AllowAnonymous]
    public IActionResult GetAuthConfig()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        
        var config = new
        {
            loginUrl = $"{baseUrl}/api/AdresAuth/login",
            logoutUrl = $"{baseUrl}/api/AdresAuth/logout",
            refreshUrl = $"{baseUrl}/api/AdresAuth/refresh",
            validateUrl = $"{baseUrl}/api/AdresAuth/validate",
            revokeUrl = $"{baseUrl}/api/AdresAuth/revoke",
            jwksUrl = $"{baseUrl}/api/AdresAuth/jwks",
            meUrl = $"{baseUrl}/api/AdresAuth/me",
            authority = _configuration["AdresAuth:ServerUrl"] ?? "https://auth.adres.gov.co"
        };

        return Ok(config);
    }

    /// <summary>
    /// Obtiene información del usuario autenticado actual
    /// GET /api/AdresAuth/me
    /// Headers: Authorization: Bearer {access_token}
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            // Obtener el access_token del header Authorization
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { error = "missing_token", message = "No se proporcionó un token de acceso" });
            }

            var accessToken = authHeader.Substring("Bearer ".Length).Trim();

            // Obtener información del usuario desde el endpoint UserInfo de Autentic Sign
            var userInfo = await _adresAuthService.GetUserInfoAsync(accessToken);

            if (userInfo == null || !userInfo.Any())
            {
                return StatusCode(500, new { error = "userinfo_failed", message = "No se pudo obtener información del usuario" });
            }

            // Extraer información relevante
            var username = userInfo.GetValueOrDefault("preferred_username")?.ToString()
                        ?? userInfo.GetValueOrDefault("name")?.ToString()
                        ?? userInfo.GetValueOrDefault("sub")?.ToString();

            var email = userInfo.GetValueOrDefault("email")?.ToString();
            var name = userInfo.GetValueOrDefault("name")?.ToString();

            // Extraer roles de los claims del JWT
            var roles = User.FindAll(ClaimTypes.Role)
                           .Select(c => c.Value)
                           .Concat(User.FindAll("roles").Select(c => c.Value))
                           .Distinct()
                           .ToList();

            var permissions = User.FindAll("permissions")
                                 .Select(c => c.Value)
                                 .ToList();

            return Ok(new
            {
                username,
                email,
                name,
                roles,
                permissions,
                authenticated = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo información del usuario");
            return StatusCode(500, new { error = "internal_error", message = "Error interno del servidor" });
        }
    }
}

// Modelos de request
public class ExchangeCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RevokeTokenRequest
{
    public string Token { get; set; } = string.Empty;
}
