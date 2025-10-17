using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using adres.api.Data;
using adres.api.Data.Seed;
using adres.api.Services;
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

// Configurar CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedCors").Get<string[]>() 
    ?? new[] { "http://localhost:4200", "http://localhost:5173", "http://localhost:3000" };
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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TODO: Ajustar según tu autenticador externo (JWKS o PEM)
        if (useJwks && !string.IsNullOrWhiteSpace(jwksUrl))
        {
            // Opción 1: Usar JWKS endpoint
            options.MetadataAddress = jwksUrl;
            options.Authority = jwtAuthority;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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
                RequireSignedTokens = false
            };
        }

        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = "roles";

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Error al autenticar token JWT");
                return Task.CompletedTask;
            }
        };
    });

// Configurar políticas de autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloRepresentanteLegal", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var esClaim = context.User.FindFirst("esRepresentanteLegal")?.Value;
            if (string.IsNullOrWhiteSpace(esClaim))
            {
                return false;
            }
            // Aceptar: "true", "True", "1"
            return esClaim.Equals("true", StringComparison.OrdinalIgnoreCase) || esClaim == "1";
        });
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
    try
    {
        var context = services.GetRequiredService<AdresAuthDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Aplicando migraciones pendientes...");
        await context.Database.MigrateAsync();
        
        logger.LogInformation("Ejecutando seed de datos...");
        await DbSeeder.SeedAsync(context);
        
        logger.LogInformation("Base de datos lista ✅");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos");
        throw;
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
