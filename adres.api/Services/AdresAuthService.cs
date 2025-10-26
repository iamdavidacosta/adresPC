using System.Net.Http.Headers;
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

    private string AuthServerUrl => _configuration["AdresAuth:ServerUrl"] ?? "https://auth.adres.gov.co";
    private string ClientId => _configuration["AdresAuth:ClientId"] ?? throw new InvalidOperationException("ClientId not configured");
    private string ClientSecret => _configuration["AdresAuth:ClientSecret"] ?? throw new InvalidOperationException("ClientSecret not configured");
    private string TokenEndpoint => $"{AuthServerUrl}/oauth/token";
    private string JwksEndpoint => $"{AuthServerUrl}/.well-known/jwks.json";
    private string RevokeEndpoint => $"{AuthServerUrl}/oauth/revoke";
    private string IntrospectEndpoint => $"{AuthServerUrl}/oauth/introspect";

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
                { "client_secret", ClientSecret },
                { "scope", "openid profile email" }
            };

            var content = new FormUrlEncodedContent(request);
            
            _logger.LogInformation("Autenticando usuario {Username} en {Endpoint}", username, TokenEndpoint);

            var response = await _httpClient.PostAsync(TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error en autenticación ADRES: {Status} - {Body}", response.StatusCode, responseBody);
                
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
                { "client_id", ClientId },
                { "client_secret", ClientSecret }
            };

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
                { "client_id", ClientId },
                { "client_secret", ClientSecret }
            };

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
                { "client_id", ClientId },
                { "client_secret", ClientSecret }
            };

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
