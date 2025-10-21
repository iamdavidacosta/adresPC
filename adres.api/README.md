# 🚀 ADRES API - Backend .NET

API REST del Sistema de Gestión ADRES desarrollada con ASP.NET Core 8.0, Entity Framework Core y SQL Server.

---

## 🛠️ Tecnologías

- **ASP.NET Core** 8.0 - Framework backend
- **Entity Framework Core** 8.0 - ORM para acceso a datos
- **SQL Server** 2022 - Base de datos
- **JWT Bearer** - Autenticación y autorización
- **Swagger/OpenAPI** - Documentación interactiva de la API
- **Docker** - Contenedorización

---

## 📁 Estructura del Proyecto

```
adres.api/
├── Controllers/
│   ├── AuthController.cs         # OAuth callbacks y redirects
│   ├── MeController.cs            # Perfil de usuario autenticado
│   ├── SecureController.cs        # Endpoints protegidos (RL)
│   ├── UsersController.cs         # Gestión de usuarios
│   └── WeatherForecastController.cs  # Ejemplo
│
├── Data/
│   ├── ApplicationDbContext.cs    # DbContext principal
│   ├── DbSeeder.cs                # Seed de datos iniciales
│   └── Migrations/                # Migraciones EF Core
│
├── Models/
│   ├── User.cs                    # Modelo de usuario
│   ├── UserRole.cs                # Roles de usuario
│   └── ...
│
├── Services/
│   └── UserDirectoryService.cs    # Lógica de negocio de usuarios
│
├── appsettings.json               # Configuración base
├── appsettings.Development.json   # Config desarrollo
├── appsettings.Staging.json       # Config staging
├── appsettings.Production.json    # Config producción
├── Program.cs                     # Configuración de la aplicación
├── Dockerfile                     # Build para producción
└── adres.api.csproj              # Proyecto .NET
```

---

## 🚀 Inicio Rápido

### **Prerequisitos**
- .NET SDK 8.0+
- SQL Server 2022 (o Docker)
- Visual Studio 2022 o VS Code con extensión C#

### **Desarrollo Local (sin Docker)**

```bash
# Restaurar paquetes NuGet
dotnet restore

# Aplicar migraciones
dotnet ef database update

# Ejecutar API
dotnet run

# API disponible en: http://localhost:8080
# Swagger UI: http://localhost:8080/swagger
```

### **Desarrollo con Docker Compose**

```bash
# Desde la raíz del proyecto
docker-compose up -d sqlserver api

# Ver logs
docker-compose logs -f api

# API disponible en: http://localhost:8080
```

---

## 🔌 Endpoints Principales

### **Autenticación**

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| GET | `/api/Auth/login` | Redirect a OAuth externo | No |
| GET | `/api/Auth/callback` | Callback de OAuth | No |
| GET | `/api/Auth/logout` | Cerrar sesión | No |
| GET | `/api/Auth/config` | Configuración OAuth | No |
| GET | `/api/Auth/error` | Página de error de auth | No |

### **Usuarios**

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| GET | `/api/Users` | Listar todos los usuarios | No |
| GET | `/api/Me` | Perfil del usuario autenticado | Bearer Token |

### **Seguridad (Representante Legal)**

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| GET | `/api/Secure/solo-rl` | Acceso exclusivo RL | Bearer Token + Policy |

---

## 🔐 Autenticación JWT

La API usa JWT Bearer tokens para autenticación. Los tokens deben incluirse en el header `Authorization`:

```http
GET /api/Me HTTP/1.1
Host: localhost:8080
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

### **Claims Requeridos**

- `sub`: Identificador único del usuario
- `email`: Email del usuario
- `esRepresentanteLegal`: "true" o "false" (para acceso a `/api/Secure/solo-rl`)
- `role`: Roles del usuario (Admin, Analista, Consulta)

### **Configuración de JWT**

En `appsettings.json`:

```json
{
  "JwtSettings": {
    "Authority": "https://auth.adres.gov.co",
    "Audience": "adres-api",
    "RequireHttpsMetadata": true,
    "ValidateIssuerSigningKey": true,
    "JwksUri": "https://auth.adres.gov.co/.well-known/jwks.json"
  }
}
```

---

## 🗄️ Base de Datos

### **Modelo de Datos**

**Tabla: Users**
- `Id` (int, PK): ID autoincremental
- `Sub` (string): Identificador OAuth (único)
- `Username` (string): Nombre de usuario
- `Email` (string): Email
- `EsRepresentanteLegal` (bool): Si es Representante Legal
- `Roles` (List<UserRole>): Roles del usuario

**Tabla: UserRoles**
- `Id` (int, PK)
- `UserId` (int, FK)
- `Role` (string): Nombre del rol

### **Migraciones**

```bash
# Crear nueva migración
dotnet ef migrations add NombreMigracion

# Aplicar migraciones
dotnet ef database update

# Rollback a migración específica
dotnet ef database update NombreMigracionAnterior

# Eliminar última migración (si no se ha aplicado)
dotnet ef migrations remove
```

### **Seed de Datos**

Al iniciar la aplicación, se ejecuta `DbSeeder` que crea:

- **Juan Pérez** - Admin, Representante Legal
- **María Gómez** - Consulta

---

## ⚙️ Configuración

### **CORS**

Configurado para aceptar peticiones desde:
- `http://localhost:3000` (Frontend React)
- `http://localhost:4200` (Angular - legacy)
- `http://localhost:5173` (Vite)
- `http://localhost:5174` (Vite alternativo)

Para producción/staging, se configuran dominios específicos en `appsettings.Production.json` y `appsettings.Staging.json`.

### **Variables de Entorno**

**Desarrollo:**
```bash
ASPNETCORE_ENVIRONMENT=Development
ENABLE_SWAGGER=true
ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=AdresDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

**Producción:**
```bash
ASPNETCORE_ENVIRONMENT=Production
ENABLE_SWAGGER=false
ConnectionStrings__DefaultConnection="Server=sql.adres.gov.co;Database=AdresDb;User Id=sa;Password=${SQL_PASSWORD};TrustServerCertificate=True"
JwtSettings__Authority="https://auth.adres.gov.co"
JwtSettings__Audience="adres-api"
ALLOWED_CORS="https://app.adres.gov.co"
```

---

## 🐳 Docker

### **Build Manual**

```bash
# Construir imagen
docker build -t adres-api:latest .

# Ejecutar contenedor
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=sqlserver;..." \
  adres-api:latest
```

### **Docker Compose**

Ver el `docker-compose.yml` en la raíz del proyecto para orquestación completa.

---

## 📝 Políticas de Autorización

### **SoloRepresentanteLegal**

Política que verifica el claim `esRepresentanteLegal == "true"`:

```csharp
[Authorize(Policy = "SoloRepresentanteLegal")]
public IActionResult AccesoExclusivo()
{
    return Ok("Acceso concedido a Representante Legal");
}
```

### **Roles**

- **Admin**: Acceso completo a la administración
- **Analista**: Acceso a análisis y reportes
- **Consulta**: Solo lectura

```csharp
[Authorize(Roles = "Admin,Analista")]
public IActionResult AdminOnly()
{
    return Ok("Solo Admin o Analista");
}
```

---

## 🧪 Testing

```bash
# Ejecutar tests unitarios
dotnet test

# Con coverage
dotnet test /p:CollectCoverage=true
```

---

## 📊 Swagger / OpenAPI

Swagger UI está disponible en:
- **Desarrollo**: http://localhost:8080/swagger
- **Staging**: https://api.staging.adres.gov.co/swagger (si está habilitado)
- **Producción**: Deshabilitado por seguridad

### **Probar con Swagger**

1. Ir a http://localhost:8080/swagger
2. Click en "Authorize" 🔒
3. Ingresar: `Bearer {tu-token-jwt}`
4. Probar endpoints

---

## 🔧 Troubleshooting

### Error: "Cannot connect to SQL Server"
```bash
# Verificar que SQL Server esté corriendo
docker-compose ps sqlserver

# Ver logs de SQL Server
docker-compose logs sqlserver

# Reiniciar contenedor
docker-compose restart sqlserver
```

### Error: "JWT Bearer error"
- Verificar que `JwtSettings:Authority` sea correcto
- Verificar que el token no esté expirado
- Verificar claims requeridos (`sub`, `email`)

### Error de CORS
Agregar origen en `appsettings.json`:
```json
{
  "AllowedCors": [
    "http://localhost:3000",
    "https://tu-dominio.com"
  ]
}
```

---

## 📚 Documentación Adicional

- **Deployment**: Ver `../DEPLOYMENT.md`
- **Authentication URLs**: Ver `../AUTHENTICATION_URLS.md`
- **Frontend**: Ver `../adres-web/README.md`

---

## 📞 Soporte

- **Issues**: https://github.com/tu-usuario/adres-api/issues
- **Email**: dev@adres.gov.co

---

**Última Actualización**: Octubre 2025  
**Versión**: 1.0.0
