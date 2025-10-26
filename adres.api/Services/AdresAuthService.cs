using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using adres.api.Models;

namespace adres.api.Services;

/// <summary>
/// Servicio para integración con el sistema de autenticación ADRES
/// </summary>
public interface IAdresAuthService
{
    Task<AdresAuthResponse> AuthenticateAsync(string username, string password);
    Task<AdresAuthResponse> ExchangeCodeForTokenAsync(string code, string redirectUri, string codeVerifier);
    (string authUrl, string codeVerifier) GetAuthorizationUrl(string redirectUri, string state = "");
    Task<AdresAuthResponse> RefreshTokenAsync(string refreshToken);
    Task<AdresTokenClaims> ValidateTokenAsync(string accessToken);
    Task<JwksResponse> GetJwksAsync();
    Task<bool> RevokeTokenAsync(string token);
}

public class AdresAuthService : IAdresAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdresAuthService> _logger;

    private string AuthServerUrl => _configuration["AdresAuth:ServerUrl"] ?? "https://idp.autenticsign.com";
    private string ClientId => _configuration["AdresAuth:ClientId"] ?? "410c8553-f9e4-44b8-90e1-234dd7a8bcd4";
    private string ClientSecret => _configuration["AdresAuth:ClientSecret"] ?? "";
    private string TokenEndpoint => $"{AuthServerUrl}{_configuration["AdresAuth:TokenEndpoint"] ?? "/connect/token"}";
    private string JwksEndpoint => $"{AuthServerUrl}{_configuration["AdresAuth:JwksEndpoint"] ?? "/.well-known/jwks.json"}";
    private string RevokeEndpoint => $"{AuthServerUrl}{_configuration["AdresAuth:RevokeEndpoint"] ?? "/connect/revocation"}";
    private string IntrospectEndpoint => $"{AuthServerUrl}{_configuration["AdresAuth:IntrospectEndpoint"] ?? "/connect/introspect"}";
    private string UserInfoEndpoint => $"{AuthServerUrl}{_configuration["AdresAuth:UserInfoEndpoint"] ?? "/connect/userinfo"}";
    private string Scopes => _configuration["AdresAuth:Scopes"] ?? "openid extended_profile";

    public AdresAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<AdresAuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }
    /// <summary>
    /// Autentica un usuario contra el servidor ADRES
    /// </summary>
    public async Task<AdresAuthResponse> AuthenticateAsync(string username, string password)
    {
        try
        {
            var request = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", username },
                { "password", password },
                { "client_id", ClientId },
                { "scope", Scopes }
            };

            // Solo agregar client_secret si está configurado
            if (!string.IsNullOrWhiteSpace(ClientSecret))
            {
                request.Add("client_secret", ClientSecret);
            }

            var content = new FormUrlEncodedContent(request);
            
            _logger.LogInformation("Autenticando usuario {Username} en {Endpoint}", username, TokenEndpoint);

            var response = await _httpClient.PostAsync(TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error en autenticación Autentic Sign: {Status} - {Body}", response.StatusCode, responseBody);
                
                var errorResponse = JsonSerializer.Deserialize<AdresErrorResponse>(responseBody);
                throw new UnauthorizedAccessException(errorResponse?.ErrorDescription ?? "Authentication failed");
            }

            var authResponse = JsonSerializer.Deserialize<AdresAuthResponse>(responseBody);
            
            if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
            {
                throw new InvalidOperationException("Invalid response from ADRES auth server");
            }

            _logger.LogInformation("Autenticación exitosa para usuario {Username}", username);
            
            return authResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de conexión con servidor ADRES");
            throw new InvalidOperationException("Unable to connect to ADRES authentication server", ex);
        }
    }

    /// <summary>
    /// Genera la URL de autorización para el flujo Authorization Code con PKCE
    /// </summary>
    public (string authUrl, string codeVerifier) GetAuthorizationUrl(string redirectUri, string state = "")
    {
        var authEndpoint = $"{AuthServerUrl}{_configuration["AdresAuth:AuthorizationEndpoint"] ?? "/connect/authorize"}";
        
        // Generar PKCE code_verifier y code_challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        var queryParams = new Dictionary<string, string>
        {
            { "client_id", ClientId },
            { "redirect_uri", redirectUri },
            { "response_type", "code" },
            { "scope", Scopes },
            { "code_challenge", codeChallenge },
            { "code_challenge_method", "S256" }
        };

        if (!string.IsNullOrWhiteSpace(state))
        {
            queryParams.Add("state", state);
        }

        var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        
        var authUrl = $"{authEndpoint}?{queryString}";
        
        _logger.LogInformation("URL de autorización generada con PKCE");
        
        return (authUrl, codeVerifier);
    }

    /// <summary>
    /// Genera un code_verifier aleatorio para PKCE
    /// </summary>
    private string GenerateCodeVerifier()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Base64UrlEncode(randomBytes);
    }

    /// <summary>
    /// Genera el code_challenge a partir del code_verifier usando SHA256
    /// </summary>
    private string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(challengeBytes);
    }

    /// <summary>
    /// Codifica bytes en Base64 URL-safe (sin padding)
    /// </summary>
    private string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        // Convertir a Base64 URL-safe
        base64 = base64.Replace("+", "-");
        base64 = base64.Replace("/", "_");
        base64 = base64.TrimEnd('=');
        return base64;
    }

    /// <summary>
    /// Intercambia el código de autorización por un access token (Authorization Code Flow con PKCE)
    /// </summary>
    public async Task<AdresAuthResponse> ExchangeCodeForTokenAsync(string code, string redirectUri, string codeVerifier)
    {
        try
        {
            var request = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", ClientId },
                { "code_verifier", codeVerifier }
            };

            // Solo agregar client_secret si está configurado
            if (!string.IsNullOrWhiteSpace(ClientSecret))
            {
                request.Add("client_secret", ClientSecret);
            }

            var content = new FormUrlEncodedContent(request);
            
            _logger.LogInformation("Intercambiando código de autorización por token en {Endpoint} con PKCE", TokenEndpoint);

            var response = await _httpClient.PostAsync(TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error intercambiando código: {Status} - {Body}", response.StatusCode, responseBody);
                
                var errorResponse = JsonSerializer.Deserialize<AdresErrorResponse>(responseBody);
                throw new UnauthorizedAccessException(errorResponse?.ErrorDescription ?? "Code exchange failed");
            }

            var authResponse = JsonSerializer.Deserialize<AdresAuthResponse>(responseBody);
            
            if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
            {
                throw new InvalidOperationException("Invalid response from auth server");
            }

            _logger.LogInformation("✅ Código intercambiado exitosamente por token");
            
            return authResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error intercambiando código de autorización");
            throw new InvalidOperationException("Unable to exchange authorization code", ex);
        }
    }

    /// <summary>
    /// Refresca un token usando el refresh_token
    /// </summary>
    public async Task<AdresAuthResponse> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var request = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", ClientId }
            };

            // Solo agregar client_secret si está configurado
            if (!string.IsNullOrWhiteSpace(ClientSecret))
            {
                request.Add("client_secret", ClientSecret);
            }

            var content = new FormUrlEncodedContent(request);
            var response = await _httpClient.PostAsync(TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<AdresErrorResponse>(responseBody);
                throw new UnauthorizedAccessException(errorResponse?.ErrorDescription ?? "Token refresh failed");
            }

            var authResponse = JsonSerializer.Deserialize<AdresAuthResponse>(responseBody);
            
            if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
            {
                throw new InvalidOperationException("Invalid response from ADRES auth server");
            }

            return authResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error refrescando token");
            throw new InvalidOperationException("Unable to refresh token", ex);
        }
    }

    /// <summary>
    /// Valida un token y obtiene los claims
    /// </summary>
    public async Task<AdresTokenClaims> ValidateTokenAsync(string accessToken)
    {
        try
        {
            var request = new Dictionary<string, string>
            {
                { "token", accessToken },
                { "client_id", ClientId }
            };

            // Solo agregar client_secret si está configurado
            if (!string.IsNullOrWhiteSpace(ClientSecret))
            {
                request.Add("client_secret", ClientSecret);
            }

            var content = new FormUrlEncodedContent(request);
            var response = await _httpClient.PostAsync(IntrospectEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new UnauthorizedAccessException("Token validation failed");
            }

            var claims = JsonSerializer.Deserialize<AdresTokenClaims>(responseBody);
            
            if (claims == null)
            {
                throw new InvalidOperationException("Invalid token claims");
            }

            return claims;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error validando token");
            throw new InvalidOperationException("Unable to validate token", ex);
        }
    }

    /// <summary>
    /// Obtiene las claves públicas JWKS para validar tokens
    /// </summary>
    public async Task<JwksResponse> GetJwksAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(JwksEndpoint);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Failed to retrieve JWKS");
            }

            var jwks = JsonSerializer.Deserialize<JwksResponse>(responseBody);
            
            if (jwks == null || jwks.Keys == null || !jwks.Keys.Any())
            {
                throw new InvalidOperationException("Invalid JWKS response");
            }

            return jwks;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error obteniendo JWKS");
            throw new InvalidOperationException("Unable to retrieve JWKS", ex);
        }
    }

    /// <summary>
    /// Revoca un token (access o refresh)
    /// </summary>
    public async Task<bool> RevokeTokenAsync(string token)
    {
        try
        {
            var request = new Dictionary<string, string>
            {
                { "token", token },
                { "client_id", ClientId }
            };

            // Solo agregar client_secret si está configurado
            if (!string.IsNullOrWhiteSpace(ClientSecret))
            {
                request.Add("client_secret", ClientSecret);
            }

            var content = new FormUrlEncodedContent(request);
            var response = await _httpClient.PostAsync(RevokeEndpoint, content);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error revocando token");
            return false;
        }
    }
}
