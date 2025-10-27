using System.Text.Json.Serialization;

namespace adres.api.Models;

/// <summary>
/// Modelo de respuesta del endpoint de autenticación ADRES
/// </summary>
public class AdresAuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

/// <summary>
/// Modelo de solicitud de login ADRES
/// </summary>
public class AdresLoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "password";

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
}

/// <summary>
/// Modelo de claims del token ADRES
/// </summary>
public class AdresTokenClaims
{
    [JsonPropertyName("sub")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("preferred_username")]
    public string PreferredUsername { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("roles")]
    public List<string>? Roles { get; set; }

    [JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }

    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    [JsonPropertyName("exp")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("iss")]
    public string Issuer { get; set; } = string.Empty;

    [JsonPropertyName("aud")]
    public string Audience { get; set; } = string.Empty;
}

/// <summary>
/// Modelo de respuesta de error ADRES
/// </summary>
public class AdresErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; } = string.Empty;

    [JsonPropertyName("error_uri")]
    public string? ErrorUri { get; set; }
}

/// <summary>
/// Modelo de configuración JWKS (JSON Web Key Set)
/// </summary>
public class JwksResponse
{
    [JsonPropertyName("keys")]
    public List<JsonWebKey> Keys { get; set; } = new();
}

/// <summary>
/// Modelo de una clave pública JSON Web Key
/// </summary>
public class JsonWebKey
{
    [JsonPropertyName("kty")]
    public string KeyType { get; set; } = string.Empty;

    [JsonPropertyName("use")]
    public string Use { get; set; } = string.Empty;

    [JsonPropertyName("kid")]
    public string KeyId { get; set; } = string.Empty;

    [JsonPropertyName("alg")]
    public string Algorithm { get; set; } = string.Empty;

    [JsonPropertyName("n")]
    public string Modulus { get; set; } = string.Empty;

    [JsonPropertyName("e")]
    public string Exponent { get; set; } = string.Empty;

    [JsonPropertyName("x5c")]
    public List<string>? X509Chain { get; set; }

    [JsonPropertyName("x5t")]
    public string? X509Thumbprint { get; set; }
}
