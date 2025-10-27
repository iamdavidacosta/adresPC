# ✅ FIX: Error 401 en /api/AdresAuth/me

## 🎉 Progreso Previo

✅ **PKCE Fixed** - El intercambio de código por token ahora funciona perfectamente
✅ **Token Obtenido** - El access_token se recibe correctamente de Autentic Sign

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "openid extended_profile"
}
```

---

## 🔴 Nuevo Problema

**Endpoint:** `GET /api/AdresAuth/me`  
**Status:** 401 Unauthorized

El token de Autentic Sign no está siendo validado correctamente por el backend.

---

## 🔍 Causa Raíz

El token JWT de Autentic Sign tiene:
```json
{
  "aud": "https://idp.autenticsign.com/resources",
  "iss": "https://idp.autenticsign.com",
  ...
}
```

Pero el backend estaba configurado para validar:
```bash
AUTH_AUDIENCE=adres-api  # ❌ No coincide
```

**Validación fallaba** porque el `aud` del token (`https://idp.autenticsign.com/resources`) no coincide con el esperado (`adres-api`).

---

## ✅ Solución Aplicada

**Modificado:** `Program.cs` - Configuración JWT

### Cambio 1: Múltiples Audiences Aceptadas

```csharp
// ANTES:
options.Authority = jwtAuthority;
options.RequireHttpsMetadata = ...;

// DESPUÉS:
options.Authority = jwtAuthority;
options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = jwtAuthority,
    ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
    ValidAudiences = new[] { 
        jwtAudience,                      // "adres-api"
        $"{jwtAuthority}/resources"       // "https://idp.autenticsign.com/resources"
    },
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    NameClaimType = "preferred_username",
    RoleClaimType = "roles"
};
```

**Ahora acepta AMBOS audiences:**
- `adres-api` (si Autentic Sign lo envía)
- `https://idp.autenticsign.com/resources` (el que realmente envía)

### Cambio 2: Logs de Validación

Agregados eventos para debug:
```csharp
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = context =>
    {
        logger.LogError("❌ Error al autenticar token JWT");
        return Task.CompletedTask;
    },
    OnTokenValidated = context =>
    {
        logger.LogInformation("✅ Token JWT validado correctamente");
        return Task.CompletedTask;
    }
};
```

---

## 🚀 Desplegar

```bash
cd ~/adresPC

# Pull cambios
git pull

# Rebuild
docker compose down
docker compose build api
docker compose up -d

# Ver logs
docker compose logs -f api
```

---

## 🧪 Probar

1. **Iniciar sesión** en `https://adres-autenticacion.centralspike.com`
2. **Completar OAuth** con Autentic Sign
3. **Observar logs** - Deberías ver:
   ```
   ✅ Token JWT validado correctamente
   ```
4. **El dashboard debería cargar** sin error 401

---

## 📋 Verificación en Logs

**Buscar estas líneas:**

### ✅ Token Válido:
```
✅ Token JWT validado correctamente
```

### ❌ Si aún falla:
```
❌ Error al autenticar token JWT
Error: The audience 'adres-api' is invalid
```

Si ves el segundo error, significa que necesitamos ajustar más la configuración del audience.

---

## 🎯 Resumen de Cambios

| Archivo | Cambio | Razón |
|---------|--------|-------|
| `Program.cs` | Agregado `ValidAudiences` con múltiples valores | Aceptar el `aud` real de Autentic Sign |
| `Program.cs` | Movido `NameClaimType` y `RoleClaimType` a TokenValidationParameters | Evitar sobrescritura |
| `Program.cs` | Agregados eventos `OnTokenValidated` y `OnAuthenticationFailed` | Debugging |

---

## 💡 Nota sobre Audiences

El audience en OAuth define **para qué recurso es válido el token**.

**Autentic Sign está configurado para:**
- Emitir tokens con `aud: "https://idp.autenticsign.com/resources"`

**Nuestras opciones:**

1. ✅ **Aceptar el audience real** (solución aplicada)
2. ⚠️ Pedir a Autentic Sign que cambie el audience a `adres-api`
3. ⚠️ Desactivar validación de audience (no recomendado para producción)

La opción 1 es la más práctica y segura.

---

## 🔄 Si Aún Falla

Si después del deploy aún da 401, ejecutar:

```bash
# Ver logs con contexto
docker compose logs --tail=100 api | grep -E "Token|autenticar|validado"

# Ver el error específico
docker compose logs --tail=50 api
```

Y compartir los logs para análisis adicional.

---

## 📄 Archivos Relacionados

- `FIX_PKCE_MISMATCH.md` - Fix anterior (PKCE)
- `ANALISIS_INVALID_GRANT.md` - Análisis del error invalid_grant
- `FIX_JWT_AUDIENCE.md` - Este archivo

---

## 🎉 Estado Actual

✅ OAuth flow completo funciona  
✅ Código se intercambia por token  
✅ Token se recibe correctamente  
🔄 Token debería validarse (pending deploy)  
🔲 Dashboard carga con usuario autenticado
