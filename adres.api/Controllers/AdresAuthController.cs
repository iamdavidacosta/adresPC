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
    /// GET /api/AdresAuth/authorize?mode=testing (para modo testing con callback del backend)
    /// </summary>
    [HttpGet("authorize")]
    [AllowAnonymous]
    public IActionResult Authorize([FromQuery] string? returnUrl = null, [FromQuery] string? mode = null)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        
        // Generar PKCE
        var (authUrl, codeVerifier) = _adresAuthService.GetAuthorizationUrl(
            _configuration["AdresAuth:RedirectUri"] ?? "https://adres-autenticacion.centralspike.com/auth/callback",
            ""
        );
        
        // Si mode=testing, usar callback del backend
        if (mode == "testing")
        {
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/AdresAuth/callback-handler";
            (authUrl, codeVerifier) = _adresAuthService.GetAuthorizationUrl(callbackUrl, "");
            
            // Guardar code_verifier en sesión (solo para modo testing)
            HttpContext.Session.SetString("pkce_code_verifier", codeVerifier);
            
            _logger.LogInformation("🔄 Redirigiendo a Autentic Sign con PKCE (modo testing, callback: {Callback})", callbackUrl);
            return Redirect(authUrl);
        }
        
        // Modo normal: El frontend necesitará el code_verifier
        // Lo pasamos como parámetro seguro en el state (encriptado en Base64)
        var stateData = new
        {
            returnUrl = returnUrl ?? "/",
            cv = codeVerifier // code_verifier
        };
        
        var stateJson = System.Text.Json.JsonSerializer.Serialize(stateData);
        var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(stateJson));
        
        // Regenerar authUrl con el state que incluye el code_verifier
        (authUrl, _) = _adresAuthService.GetAuthorizationUrl(
            _configuration["AdresAuth:RedirectUri"] ?? "https://adres-autenticacion.centralspike.com/auth/callback",
            state
        );
        
        _logger.LogInformation("🔄 Redirigiendo a Autentic Sign con PKCE (modo normal, code_verifier en state)");

        return Redirect(authUrl);
    }

    /// <summary>
    /// Callback handler especial para testing - Maneja el callback de Autentic Sign y muestra tokens
    /// GET /api/AdresAuth/callback-handler
    /// </summary>
    [HttpGet("callback-handler")]
    [AllowAnonymous]
    public async Task<IActionResult> CallbackHandler([FromQuery] string code, [FromQuery] string? state = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Content(GenerateErrorHtml("No se recibió código de autorización"), "text/html");
            }

            _logger.LogInformation("📥 Callback handler recibido con código");

            // Recuperar code_verifier de la sesión
            var codeVerifier = HttpContext.Session.GetString("pkce_code_verifier");
            if (string.IsNullOrWhiteSpace(codeVerifier))
            {
                return Content(GenerateErrorHtml("PKCE code verifier not found in session. La sesión pudo haber expirado."), "text/html");
            }

            var redirectUri = $"{Request.Scheme}://{Request.Host}/api/AdresAuth/callback-handler";

            var authResponse = await _adresAuthService.ExchangeCodeForTokenAsync(code, redirectUri, codeVerifier);
            
            // Limpiar code_verifier de la sesión
            HttpContext.Session.Remove("pkce_code_verifier");

            _logger.LogInformation("✅ Token obtenido exitosamente en callback handler");

            // Retornar página HTML con los tokens
            return Content(GenerateSuccessHtml(authResponse), "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error en callback handler");
            return Content(GenerateErrorHtml(ex.Message), "text/html");
        }
    }

    private string GenerateSuccessHtml(AdresAuthResponse response)
    {
        var expiresInMinutes = response.ExpiresIn / 60;
        return @"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>✅ Autenticación Exitosa</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .container {
            background: white;
            border-radius: 16px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            padding: 40px;
            max-width: 800px;
            width: 100%;
        }
        h1 { color: #28a745; text-align: center; margin-bottom: 20px; }
        .token-section { margin: 20px 0; }
        .token-label { font-weight: bold; color: #333; margin-bottom: 8px; }
        .token-box {
            background: #f8f9fa;
            border: 2px solid #28a745;
            border-radius: 8px;
            padding: 15px;
            font-family: 'Courier New', monospace;
            font-size: 12px;
            word-break: break-all;
            position: relative;
        }
        .copy-btn {
            background: #28a745;
            color: white;
            border: none;
            padding: 10px 20px;
            border-radius: 6px;
            cursor: pointer;
            margin-top: 10px;
            transition: background 0.3s;
        }
        .copy-btn:hover { background: #218838; }
        .info-box {
            background: #e3f2fd;
            border-left: 4px solid #2196f3;
            padding: 15px;
            margin-top: 30px;
            border-radius: 4px;
        }
        code {
            background: #fff;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>✅ Autenticación Exitosa</h1>
        
        <div class=""token-section"">
            <div class=""token-label"">🔑 Access Token:</div>
            <div class=""token-box"" id=""accessToken"">" + response.AccessToken + @"</div>
            <button class=""copy-btn"" onclick=""copyToken('accessToken')"">📋 Copiar Access Token</button>
        </div>
        
        <div class=""token-section"">
            <div class=""token-label"">🔄 Refresh Token:</div>
            <div class=""token-box"" id=""refreshToken"">" + (response.RefreshToken ?? "No disponible") + @"</div>
            <button class=""copy-btn"" onclick=""copyToken('refreshToken')"">📋 Copiar Refresh Token</button>
        </div>
        
        <div class=""info-box"">
            <p><strong>📊 Información del Token:</strong></p>
            <p>• Tipo: " + response.TokenType + @"</p>
            <p>• Expira en: " + response.ExpiresIn + @" segundos (" + expiresInMinutes + @" minutos)</p>
            <p>• Scopes: " + response.Scope + @"</p>
            <p style=""margin-top: 15px;""><strong>💡 Cómo usar:</strong></p>
            <p>Copia el Access Token y úsalo en tus requests:</p>
            <p><code>Authorization: Bearer YOUR_ACCESS_TOKEN</code></p>
        </div>
    </div>
    
    <script>
        function copyToken(elementId) {
            const tokenElement = document.getElementById(elementId);
            const token = tokenElement.textContent;
            navigator.clipboard.writeText(token).then(() => {
                const btn = event.target;
                const originalText = btn.textContent;
                btn.textContent = '✅ Copiado!';
                setTimeout(() => btn.textContent = originalText, 2000);
            });
        }
    </script>
</body>
</html>";
    }

    private string GenerateErrorHtml(string errorMessage)
    {
        return @"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>❌ Error</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .container {
            background: white;
            border-radius: 16px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            padding: 40px;
            max-width: 600px;
            width: 100%;
        }
        h1 { color: #dc3545; text-align: center; margin-bottom: 20px; }
        .error-message {
            background: #f8d7da;
            border-left: 4px solid #dc3545;
            padding: 15px;
            border-radius: 4px;
            color: #721c24;
        }
        .retry-btn {
            background: #007bff;
            color: white;
            border: none;
            padding: 12px 24px;
            border-radius: 6px;
            cursor: pointer;
            margin-top: 20px;
            width: 100%;
            font-size: 16px;
        }
        .retry-btn:hover { background: #0056b3; }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>❌ Error de Autenticación</h1>
        <div class=""error-message"">
            <p><strong>Error:</strong> " + errorMessage + @"</p>
        </div>
        <button class=""retry-btn"" onclick=""window.location.href='/api/AdresAuth/authorize?mode=testing'"">
            🔄 Reintentar Autenticación
        </button>
    </div>
</body>
</html>";
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
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? User.FindFirst("preferred_username")?.Value
                      ?? User.FindFirst("sub")?.Value
                      ?? User.Identity?.Name;
        
        var email = User.FindFirst(ClaimTypes.Email)?.Value 
                   ?? User.FindFirst("email")?.Value;
        
        var name = User.FindFirst(ClaimTypes.Name)?.Value 
                  ?? User.FindFirst("name")?.Value;

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
