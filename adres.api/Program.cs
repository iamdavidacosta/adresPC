using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi.Models;
using adres.api.Data;
using adres.api.Data.Seed;
using adres.api.Services;
using adres.api.Authorization;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// TODO: Configurar variables de entorno según tu autenticador externo
// Leer configuración desde appsettings.json o variables de entorno
var jwtAuthority = Environment.GetEnvironmentVariable("AUTH_AUTHORITY") 
                   ?? builder.Configuration["Jwt:Authority"];
var jwtAudience = Environment.GetEnvironmentVariable("AUTH_AUDIENCE") 
                  ?? builder.Configuration["Jwt:Audience"];
var useJwks = bool.Parse(Environment.GetEnvironmentVariable("AUTH_USE_JWKS") 
                         ?? builder.Configuration["Jwt:UseJwks"] ?? "true");
var jwksUrl = Environment.GetEnvironmentVariable("AUTH_JWKS_URL") 
              ?? builder.Configuration["Jwt:JwksUrl"];
var pemPath = Environment.GetEnvironmentVariable("AUTH_PEM_PATH") 
              ?? builder.Configuration["Jwt:PublicKeyPemPath"];

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Configurar DbContext
builder.Services.AddDbContext<AdresAuthDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configurar servicios
builder.Services.AddScoped<IUserDirectory, UserDirectory>();

// Registrar el authorization handler personalizado
builder.Services.AddSingleton<IAuthorizationHandler, RepresentanteLegalAuthorizationHandler>();

// Configurar HttpClient para AdresAuthService
builder.Services.AddHttpClient<IAdresAuthService, AdresAuthService>();

// Configurar sesiones distribuidas en memoria para PKCE
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10); // Tiempo de expiración de sesión
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configurar CORS
var allowedCorsEnv = Environment.GetEnvironmentVariable("ALLOWED_CORS");
var allowedOrigins = !string.IsNullOrWhiteSpace(allowedCorsEnv)
    ? allowedCorsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : (builder.Configuration.GetSection("AllowedCors").Get<string[]>() 
       ?? new[] { "http://localhost:4200", "http://localhost:5173", "http://localhost:3000" });

// Log de CORS configurados
Console.WriteLine($"🔒 CORS configurado para: {string.Join(", ", allowedOrigins)}");

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configurar autenticación JWT
Console.WriteLine($"🔐 Configurando JWT Authentication:");
Console.WriteLine($"  Authority: {jwtAuthority}");
Console.WriteLine($"  JWKS URL: {jwksUrl}");
Console.WriteLine($"  Audience: {jwtAudience}");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TODO: Ajustar según tu autenticador externo (JWKS o PEM)
        if (useJwks && !string.IsNullOrWhiteSpace(jwtAuthority))
        {
            // Opción 1: Usar OpenID Connect Discovery (Authority)
            // Esto descarga automáticamente /.well-known/openid-configuration
            // y obtiene el jwks_uri de ahí
            options.Authority = jwtAuthority;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            
            // Configurar validación de tokens
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtAuthority,
                ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
                ValidAudiences = new[] { jwtAudience, $"{jwtAuthority}/resources" }, // Aceptar múltiples audiences
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                NameClaimType = "preferred_username",
                RoleClaimType = "roles"
            };
            
            Console.WriteLine($"✅ JWT configurado con Authority: {jwtAuthority}");
            Console.WriteLine($"   Descargará configuración desde: {jwtAuthority}/.well-known/openid-configuration");
        }
        else if (!string.IsNullOrWhiteSpace(pemPath) && File.Exists(pemPath))
        {
            // Opción 2: Usar archivo PEM con clave pública RSA
            var publicKeyPem = File.ReadAllText(pemPath);
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            var securityKey = new RsaSecurityKey(rsa);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtAuthority,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey
            };
        }
        else
        {
            // Configuración mínima (sin validación de firma - solo para desarrollo)
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = false,
                RequireSignedTokens = false,
                NameClaimType = "preferred_username",
                RoleClaimType = "roles"
            };
        }

        // Agregar logging de errores de autenticación
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation($"📨 Token recibido para validación");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "❌ Error al autenticar token JWT");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("✅ Token JWT validado correctamente");
                return Task.CompletedTask;
            }
        };
    });

// Configurar políticas de autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloRepresentanteLegal", policy =>
    {
        policy.Requirements.Add(new RepresentanteLegalRequirement());
    });
});

// Configurar controladores
builder.Services.AddControllers();

// Configurar Swagger con soporte para JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ADRES API",
        Version = "v1",
        Description = "API con autenticación JWT externa y autorización local"
    });

    // Configurar seguridad Bearer en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese su token JWT en el formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Aplicar migraciones y seed de datos al iniciar
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AdresAuthDbContext>();
    
    // Reintentar conexión a base de datos con backoff
    var maxRetries = 10;
    var delay = TimeSpan.FromSeconds(5);
    
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Intento {Attempt}/{MaxRetries}: Conectando a SQL Server...", i, maxRetries);
            
            // Probar conexión
            await context.Database.CanConnectAsync();
            logger.LogInformation("✅ Conexión exitosa a SQL Server");
            
            logger.LogInformation("Aplicando migraciones pendientes...");
            await context.Database.MigrateAsync();
            
            logger.LogInformation("Ejecutando seed de datos...");
            await DbSeeder.SeedAsync(context);
            
            logger.LogInformation("Base de datos lista ✅");
            break; // Salir del loop si todo salió bien
        }
        catch (Exception ex) when (i < maxRetries)
        {
            logger.LogWarning(ex, "❌ Intento {Attempt}/{MaxRetries} fallido. Reintentando en {Delay} segundos...", i, maxRetries, delay.TotalSeconds);
            await Task.Delay(delay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al inicializar la base de datos después de {MaxRetries} intentos", maxRetries);
            throw;
        }
    }
}

// Configurar pipeline HTTP
// Habilitar Swagger en todos los entornos (puede deshabilitarse en producción si es necesario)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ADRES API v1");
    options.RoutePrefix = "swagger"; // Accesible en /swagger
});

// Endpoint raíz
app.MapGet("/", () => "ADRES.API lista 🚀");

app.UseCors("LocalDev");

// Habilitar sesiones
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
