# Instrucciones de Uso - ADRES.API

## 🎯 Pasos para Probar el Sistema

### 1. Levantar el sistema con Docker

```powershell
# Desde la raíz del repositorio
cd c:\Users\dacos\source\repos\adres.api
docker compose up --build
```

Espera hasta ver estos mensajes en la consola:
- ✅ SQL Server: `SQL Server is now ready for client connections`
- ✅ API: `Base de datos lista ✅`

### 2. Verificar que funciona

Abre tu navegador en:
- http://localhost:8080/ → Debe mostrar "ADRES.API lista 🚀"
- http://localhost:8080/swagger → Interfaz de Swagger

### 3. Generar un JWT de Prueba (Mock)

Como aún no tienes configurado un autenticador externo real, puedes crear un token JWT mock:

#### Opción A: Usando jwt.io

1. Ve a https://jwt.io
2. En la sección PAYLOAD, pega este JSON:

```json
{
  "sub": "u-12345",
  "email": "juan@adres.gov.co",
  "esRepresentanteLegal": "true",
  "exp": 9999999999,
  "iat": 1700000000
}
```

3. En VERIFY SIGNATURE, desmarca la verificación (o usa una clave mock)
4. Copia el token generado (sección izquierda, en azul)

#### Opción B: Token de ejemplo pre-generado (sin firma - solo para desarrollo)

**ADVERTENCIA:** Este token NO tiene firma válida. Solo funcionará si configuras la API en modo de desarrollo sin validación de firma.

```
eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiJ1LTEyMzQ1IiwiZW1haWwiOiJqdWFuQGFkcmVzLmdvdi5jbyIsImVzUmVwcmVzZW50YW50ZUxlZ2FsIjoidHJ1ZSIsImV4cCI6OTk5OTk5OTk5OSwiaWF0IjoxNzAwMDAwMDAwfQ.
```

### 4. Probar en Swagger

1. En http://localhost:8080/swagger, haz clic en **Authorize** (botón verde con candado)
2. En el cuadro "Value", escribe:
   ```
   Bearer eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiJ1LTEyMzQ1IiwiZW1haWwiOiJqdWFuQGFkcmVzLmdvdi5jbyIsImVzUmVwcmVzZW50YW50ZUxlZ2FsIjoidHJ1ZSIsImV4cCI6OTk5OTk5OTk5OSwiaWF0IjoxNzAwMDAwMDAwfQ.
   ```
3. Haz clic en **Authorize** y luego **Close**
4. Prueba estos endpoints:
   - **GET /api/me** → Debe devolver los datos del usuario "j.perez" con roles y permisos
   - **GET /api/secure/solo-rl** → Debe devolver 200 OK porque `esRepresentanteLegal=true`

### 5. Probar con Usuario sin Representante Legal

Genera otro token con este payload:

```json
{
  "sub": "u-67890",
  "email": "maria@adres.gov.co",
  "esRepresentanteLegal": "false",
  "exp": 9999999999,
  "iat": 1700000000
}
```

- **GET /api/me** → Devuelve datos de "m.gomez"
- **GET /api/secure/solo-rl** → Debe devolver **403 Forbidden** porque no es representante legal

---

## 🔧 Configurar JWT Real (Producción)

Cuando tengas tu autenticador externo configurado:

### Paso 1: Obtén la información de tu proveedor OAuth/OIDC

Necesitas:
- **Authority** (Issuer): `https://auth.tu-empresa.com`
- **Audience**: `adres-api` (o el valor que configure tu proveedor)
- **JWKS URL**: `https://auth.tu-empresa.com/.well-known/jwks.json`

### Paso 2: Actualiza `appsettings.json`

```json
"Jwt": {
  "Authority": "https://auth.tu-empresa.com",
  "Audience": "adres-api",
  "UseJwks": true,
  "JwksUrl": "https://auth.tu-empresa.com/.well-known/jwks.json"
}
```

### Paso 3: Actualiza `docker-compose.yml`

```yaml
environment:
  AUTH_AUTHORITY: "https://auth.tu-empresa.com"
  AUTH_AUDIENCE: "adres-api"
  AUTH_USE_JWKS: "true"
  AUTH_JWKS_URL: "https://auth.tu-empresa.com/.well-known/jwks.json"
```

### Paso 4: Reinicia Docker

```powershell
docker compose down
docker compose up --build
```

Ahora la API validará tokens reales firmados con RS256 🔒

---

## 📊 Consultar la Base de Datos

### Conectarse a SQL Server con Azure Data Studio o SSMS

- **Server**: `localhost,1433`
- **Authentication**: SQL Server Authentication
- **Username**: `sa`
- **Password**: `Your_strong_password_123`
- **Database**: `AdresAuthDb`

### Queries útiles

```sql
-- Ver todos los usuarios
SELECT * FROM Users;

-- Ver roles
SELECT * FROM Roles;

-- Ver permisos
SELECT * FROM Permissions;

-- Ver usuarios con sus roles
SELECT u.Username, r.Name AS Role
FROM Users u
INNER JOIN UserRoles ur ON u.Id = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.Id;

-- Ver roles con sus permisos
SELECT r.Name AS Role, p.[Key] AS Permission
FROM Roles r
INNER JOIN RolePermissions rp ON r.Id = rp.RoleId
INNER JOIN Permissions p ON rp.PermissionId = p.Id;
```

---

## 🧹 Limpiar y Reiniciar

### Borrar todo y empezar de nuevo

```powershell
# Detener contenedores
docker compose down

# Borrar volúmenes (¡esto borra la base de datos!)
docker compose down -v

# Reconstruir
docker compose up --build
```

---

## ✅ Checklist de Producción

Antes de desplegar a producción:

- [ ] Configurar `AUTH_AUTHORITY` real
- [ ] Configurar `AUTH_AUDIENCE` real
- [ ] Configurar `AUTH_JWKS_URL` real
- [ ] Cambiar contraseña de SQL Server
- [ ] Revisar `AllowedCors` para permitir tu frontend
- [ ] Configurar HTTPS con certificados válidos
- [ ] Revisar logs y monitoreo
- [ ] Hacer backup de la base de datos

---

**¡Todo listo para probar! 🎉**
