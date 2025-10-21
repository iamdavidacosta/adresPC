# 🏛️ ADRES - Sistema de Gestión

Sistema de gestión con autenticación externa JWT, autorización basada en roles y permisos, con frontend React y backend ASP.NET Core 8.

---

## � Estructura del Proyecto

```
adres.api/
├── adres.api/              # 🔧 Backend - ASP.NET Core 8 API
│   ├── Controllers/        # Controladores REST
│   ├── Data/              # DbContext y Seed
│   ├── Domain/            # Entidades (User, Role, Permission)
│   ├── Services/          # Lógica de negocio
│   ├── Migrations/        # Migraciones EF Core
│   ├── Dockerfile         # Docker para API
│   └── appsettings.json   # Configuración
│
├── adres-web/             # ⚛️ Frontend - React + Tailwind CSS
│   ├── public/            # Archivos estáticos
│   ├── src/
│   │   ├── components/    # Componentes reutilizables (Button, Card)
│   │   ├── pages/         # Páginas (HomePage, UserSelector, Dashboards)
│   │   ├── services/      # Servicios API
│   │   ├── lib/           # Utilidades
│   │   ├── App.js         # Router principal
│   │   └── index.js       # Punto de entrada
│   ├── Dockerfile         # Docker para Frontend
│   ├── package.json       # Dependencias npm
│   └── tailwind.config.js # Configuración Tailwind
│
├── docker-compose.yml     # 🐳 Orquestación completa
├── docker-compose.dev.yml # Desarrollo local
├── docker-compose.prod.yml # Producción
│
├── .env.staging          # Variables de entorno - Staging
├── .env.production       # Variables de entorno - Producción
│
├── DEPLOYMENT.md         # 📖 Guía de despliegue
├── AUTHENTICATION_URLS.md # URLs para autenticador
└── README.md             # Este archivo
```

---

## 🚀 Inicio Rápido

### **Opción 1: Docker Compose (Recomendado)**

```bash
# Clonar repositorio
git clone https://github.com/iamdavidacosta/adresPC.git
cd adresPC

# Levantar todos los servicios (Backend + Frontend + SQL Server)
docker-compose up -d

# Acceder a la aplicación
# Frontend: http://localhost:3000
# Backend API: http://localhost:8080
# Swagger: http://localhost:8080/swagger
```

### **Opción 2: Desarrollo Local**

#### **Backend**
```bash
cd adres.api

# Restaurar paquetes
dotnet restore

# Ejecutar migraciones
dotnet ef database update

# Ejecutar API
dotnet run

# API disponible en: http://localhost:8080
```

#### **Frontend**
```bash
cd adres-web

# Instalar dependencias
npm install

# Iniciar servidor de desarrollo
npm start

# Frontend disponible en: http://localhost:3000
```

---

## 🔧 Características

### **Backend (ASP.NET Core 8)**
- ✅ Autenticación JWT (RS256) con JWKS o PEM público
- ✅ Autorización local con roles y permisos en SQL Server
- ✅ Entity Framework Core con SQL Server
- ✅ Swagger UI con soporte Bearer token
- ✅ CORS configurado dinámicamente
- ✅ Migraciones automáticas en startup
- ✅ Seed de datos inicial
- ✅ Health checks
- ✅ Endpoints RESTful

### **Frontend (React 18)**
- ✅ React con Create React App
- ✅ Tailwind CSS 3 para estilos
- ✅ React Router 6 para navegación
- ✅ Componentes estilo shadcn/ui
- ✅ Lucide React para iconos
- ✅ Autenticación con JWT
- ✅ Dashboards diferenciados por rol
- ✅ Usuarios dinámicos desde BD
- ✅ Diseño responsive y minimalista

---

## 📋 Requisitos Previos

- **Docker Desktop** 20.10+ (para deployment con contenedores)
- **Node.js** 18+ y npm 9+ (para desarrollo frontend)
- **.NET 8 SDK** (para desarrollo backend)
- **SQL Server** 2019+ o Docker con imagen SQL Server
- **Git** para control de versiones

---
  "Audience": "adres-api",                            // TODO: Cambiar
  "UseJwks": true,
  "JwksUrl": "https://tu-autenticador.com/.well-known/jwks.json",  // TODO: Cambiar
  "PublicKeyPemPath": "Keys/autentic_public.pem"
}
```

También puedes usar **variables de entorno**:

- `AUTH_AUTHORITY`
- `AUTH_AUDIENCE`
- `AUTH_USE_JWKS`
- `AUTH_JWKS_URL`
- `AUTH_PEM_PATH`

### 3. Configurar Base de Datos

La cadena de conexión por defecto usa SQL Server en Docker (`sqlserver,1433`).

Para desarrollo local (sin Docker), usa `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=AdresAuthDb;User ID=sa;Password=Your_strong_password_123;TrustServerCertificate=True;Encrypt=False"
}
```

O con variable de entorno:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;..."
```

## 🐳 Ejecución con Docker Compose (Recomendado)

Desde la raíz del repositorio (`adres.api/`):

```powershell
docker compose up --build
```

Esto levantará:

1. **SQL Server** en `localhost:1433`
2. **API** en `http://localhost:8080`

### Verificar que esté funcionando

- API raíz: http://localhost:8080/
- Swagger: http://localhost:8080/swagger

## 🖥️ Ejecución Local (sin Docker)

### 1. Levantar SQL Server

Puedes usar Docker solo para SQL Server:

```powershell
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_strong_password_123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Aplicar migraciones (si no se aplicaron automáticamente)

```powershell
cd adres.api
dotnet ef database update
```

### 3. Ejecutar la API

```powershell
cd adres.api
dotnet run
```

La API estará disponible en: https://localhost:7000 (o el puerto configurado)

## 🧪 Pruebas con Swagger

1. Abre http://localhost:8080/swagger
2. Haz clic en **Authorize** (candado verde)
3. Ingresa tu token JWT en formato: `Bearer {tu_token_aqui}`
4. Prueba los endpoints:
   - `GET /api/me` → Devuelve tu perfil + roles/permisos locales
   - `GET /api/secure/solo-rl` → Solo accesible si `esRepresentanteLegal=true`

## 📊 Datos de Prueba (Seed)

El sistema carga automáticamente estos usuarios:

| Sub       | Username  | Email                | Es Rep. Legal | Roles             | Permisos                       |
|-----------|-----------|----------------------|---------------|-------------------|--------------------------------|
| u-12345   | j.perez   | juan@adres.gov.co    | ✅ Sí         | Admin, Analista   | CONSULTAR_PAGOS, CREAR_SOLICITUD |
| u-67890   | m.gomez   | maria@adres.gov.co   | ❌ No         | Consulta          | CONSULTAR_PAGOS                |

### Generar un JWT de Prueba (Mock)

Si no tienes un autenticador externo configurado, puedes generar un token JWT mock en https://jwt.io con este payload:

```json
{
  "sub": "u-12345",
  "email": "juan@adres.gov.co",
  "esRepresentanteLegal": "true",
  "exp": 9999999999
}
```

**NOTA:** En producción, el token debe venir de tu autenticador externo real.

## 🔧 Migraciones de Base de Datos

### Crear una nueva migración

```powershell
cd adres.api
dotnet ef migrations add NombreDeLaMigracion
```

### Aplicar migraciones

```powershell
dotnet ef database update
```

### Revertir última migración

```powershell
dotnet ef migrations remove
```

## 🌐 Variables de Entorno (Docker Compose)

Edita `docker-compose.yml` para configurar:

```yaml
environment:
  AUTH_AUTHORITY: "https://tu-autenticador.com"
  AUTH_AUDIENCE: "adres-api"
  AUTH_USE_JWKS: "true"
  AUTH_JWKS_URL: "https://tu-autenticador.com/.well-known/jwks.json"
  ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;..."
```

## 🛡️ Políticas de Autorización

### `SoloRepresentanteLegal`

Valida que el claim `esRepresentanteLegal` en el JWT sea `"true"`, `"True"` o `"1"`.

Ejemplo de uso:

```csharp
[Authorize(Policy = "SoloRepresentanteLegal")]
public IActionResult MiEndpoint() { ... }
```

## 📝 Endpoints Principales

| Método | Endpoint                 | Descripción                                      | Requiere Auth |
|--------|--------------------------|--------------------------------------------------|---------------|
| GET    | `/`                      | Health check (raíz)                              | ❌ No         |
| GET    | `/swagger`               | Documentación Swagger                            | ❌ No         |
| GET    | `/api/me`                | Perfil del usuario + roles/permisos locales      | ✅ Sí         |
| GET    | `/api/secure/solo-rl`    | Solo para representantes legales                 | ✅ Sí + Policy |

## 🐞 Troubleshooting

### Error: "No se puede conectar a SQL Server"

- Verifica que SQL Server esté corriendo: `docker ps`
- Espera 30 segundos después de `docker compose up` (SQL tarda en iniciar)

### Error: "Token inválido"

- Verifica que `AUTH_AUTHORITY` y `AUTH_AUDIENCE` coincidan con los del token
- Revisa los logs con: `docker compose logs api`

### Migración no aplicada

- Si la API no aplica migraciones automáticamente, ejecútalas manualmente:
  ```powershell
  dotnet ef database update
  ```

## 📚 Recursos

- [Documentación ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [JWT.io](https://jwt.io) - Decodificar/generar tokens JWT

## 📄 Licencia

Proyecto desarrollado para ADRES.

---

**¡Listo para usar! 🚀**
