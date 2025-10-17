# 🚀 Comandos Rápidos - ADRES.API (Windows PowerShell)

## 📋 Comandos Esenciales

### 🐳 Docker Compose

```powershell
# Levantar todo (API + SQL Server)
docker compose up --build

# Levantar en background (detached)
docker compose up -d --build

# Ver logs en tiempo real
docker compose logs -f

# Ver logs solo de la API
docker compose logs -f api

# Ver logs solo de SQL Server
docker compose logs -f sqlserver

# Detener todos los servicios
docker compose down

# Detener y eliminar volúmenes (¡borra la base de datos!)
docker compose down -v

# Reiniciar solo la API (sin reconstruir)
docker compose restart api

# Reconstruir solo la API
docker compose up --build api
```

### 🔨 Compilación y Ejecución Local

```powershell
# Compilar el proyecto
cd adres.api
dotnet build

# Compilar en Release
dotnet build --configuration Release

# Ejecutar la API (modo Development)
dotnet run

# Ejecutar en modo Release
dotnet run --configuration Release

# Limpiar artefactos de compilación
dotnet clean
```

### 🗄️ Entity Framework (Migraciones)

```powershell
# Agregar una nueva migración
cd adres.api
dotnet ef migrations add NombreMigracion

# Aplicar migraciones pendientes
dotnet ef database update

# Revertir a una migración específica
dotnet ef database update MigracionAnterior

# Eliminar la última migración (sin aplicar)
dotnet ef migrations remove

# Ver lista de migraciones
dotnet ef migrations list

# Generar script SQL de las migraciones
dotnet ef migrations script

# Eliminar la base de datos completamente
dotnet ef database drop
```

### 📦 NuGet (Paquetes)

```powershell
# Restaurar paquetes
dotnet restore

# Agregar un paquete
dotnet add package NombrePaquete

# Agregar paquete con versión específica
dotnet add package NombrePaquete --version 8.0.0

# Actualizar todos los paquetes
dotnet list package --outdated
dotnet add package NombrePaquete
```

### 🐳 Docker (Solo SQL Server)

```powershell
# Levantar SQL Server standalone
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_strong_password_123" -p 1433:1433 -d --name adres-sql mcr.microsoft.com/mssql/server:2022-latest

# Ver contenedores corriendo
docker ps

# Ver todos los contenedores (incluyendo detenidos)
docker ps -a

# Detener SQL Server
docker stop adres-sql

# Iniciar SQL Server
docker start adres-sql

# Ver logs de SQL Server
docker logs adres-sql

# Eliminar contenedor
docker rm adres-sql

# Eliminar contenedor forzadamente
docker rm -f adres-sql
```

### 🧹 Limpieza

```powershell
# Limpiar compilación
cd adres.api
dotnet clean

# Eliminar carpetas bin y obj
Remove-Item -Recurse -Force bin, obj

# Limpiar Docker (¡CUIDADO!)
docker system prune -a --volumes

# Limpiar solo imágenes no usadas
docker image prune -a

# Limpiar solo volúmenes no usados
docker volume prune
```

### 🔍 Diagnóstico

```powershell
# Ver versión de .NET
dotnet --version

# Ver versión de dotnet-ef
dotnet ef --version

# Ver información del proyecto
dotnet --info

# Ver paquetes instalados
dotnet list package

# Ver referencias del proyecto
dotnet list reference

# Verificar que Docker está corriendo
docker --version
docker ps
```

### 📊 SQL Server (desde PowerShell)

```powershell
# Conectarse a SQL Server con sqlcmd (si está instalado)
sqlcmd -S localhost,1433 -U sa -P Your_strong_password_123

# Ejecutar una query directamente
sqlcmd -S localhost,1433 -U sa -P "Your_strong_password_123" -Q "SELECT * FROM AdresAuthDb.dbo.Users"

# Ejecutar script SQL
sqlcmd -S localhost,1433 -U sa -P "Your_strong_password_123" -i script.sql
```

### 🌐 Pruebas de API (con curl)

```powershell
# Health check
curl http://localhost:8080/

# GET /api/me (sin token - debe fallar)
curl http://localhost:8080/api/me

# GET /api/me (con token)
$token = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiJ1LTEyMzQ1IiwiZW1haWwiOiJqdWFuQGFkcmVzLmdvdi5jbyIsImVzUmVwcmVzZW50YW50ZUxlZ2FsIjoidHJ1ZSIsImV4cCI6OTk5OTk5OTk5OSwiaWF0IjoxNzAwMDAwMDAwfQ."
curl -H "Authorization: Bearer $token" http://localhost:8080/api/me

# GET /api/secure/solo-rl (con token)
curl -H "Authorization: Bearer $token" http://localhost:8080/api/secure/solo-rl
```

### 🔄 Workflow Completo de Desarrollo

```powershell
# 1. Detener servicios si están corriendo
docker compose down

# 2. Limpiar compilación
cd adres.api
dotnet clean

# 3. Restaurar paquetes
dotnet restore

# 4. Compilar
dotnet build

# 5. Generar migración (si hiciste cambios en entidades)
dotnet ef migrations add CambiosEnEntidades

# 6. Levantar Docker
cd ..
docker compose up --build

# 7. Probar en Swagger
# Abrir http://localhost:8080/swagger
```

### 🛠️ Solución de Problemas

```powershell
# Si Docker no compila la API
docker compose down
docker rmi adres-api
docker compose up --build

# Si SQL Server no inicia
docker compose down -v
docker compose up sqlserver

# Si hay problemas de permisos en Docker
# Ejecutar PowerShell como Administrador

# Si dotnet ef no funciona
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef

# Si hay errores de migración
dotnet ef database drop --force
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
docker compose up --build
```

### 📝 Variables de Entorno (PowerShell)

```powershell
# Configurar variables de entorno temporalmente
$env:AUTH_AUTHORITY = "https://mi-auth.com"
$env:AUTH_AUDIENCE = "adres-api"
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;..."

# Ejecutar con variables
dotnet run

# Ver variables actuales
Get-ChildItem Env: | Where-Object { $_.Name -like "AUTH_*" }
```

### 🎯 Atajos Útiles

```powershell
# Alias para compilar y ejecutar
function Start-AdresApi {
    cd adres.api
    dotnet build
    dotnet run
}

# Alias para Docker
function Start-AdresDocker {
    docker compose up --build
}

# Alias para limpiar todo
function Clean-Adres {
    docker compose down -v
    cd adres.api
    dotnet clean
    Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue
    cd ..
}

# Usar los alias
Start-AdresApi
Start-AdresDocker
Clean-Adres
```

### 📚 Recursos Adicionales

```powershell
# Abrir Swagger en el navegador
Start-Process "http://localhost:8080/swagger"

# Abrir Azure Data Studio (si está instalado)
azuredatastudio

# Abrir Visual Studio Code en el proyecto
code .
```

---

**💡 Tip:** Guarda estos comandos en un script `.ps1` para reutilizarlos fácilmente.

Ejemplo: `scripts/run-dev.ps1`

```powershell
# run-dev.ps1
Write-Host "🚀 Iniciando ADRES.API en modo desarrollo..." -ForegroundColor Green
docker compose down
docker compose up --build
```

Ejecutar con: `.\scripts\run-dev.ps1`
