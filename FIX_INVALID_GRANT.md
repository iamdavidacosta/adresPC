# 🔍 Fix: Error 401 - invalid_grant en OAuth Code Exchange

## ❌ El Error

```
POST https://adres-autenticacion-back.centralspike.com/api/AdresAuth/token
Status: 401 Unauthorized
Response: {"error":"invalid_grant","error_description":""}
```

## 🔎 Causa Raíz

El error `invalid_grant` en el intercambio de código por token generalmente ocurre por:

### 1️⃣ **Redirect URI no coincide** (Causa más común)
El `redirect_uri` enviado al intercambiar el código **DEBE ser exactamente igual** al usado cuando se generó el código de autorización.

**Posibles discrepancias:**
- `http://` vs `https://`
- Con/sin trailing slash: `/auth/callback` vs `/auth/callback/`
- Puerto incluido o no: `:443` vs sin puerto
- Subdominios diferentes

### 2️⃣ **Código ya usado o expirado**
- Los códigos de autorización son de un solo uso
- Típicamente expiran en 1-5 minutos

### 3️⃣ **Code Verifier incorrecto (PKCE)**
- El `code_verifier` enviado no coincide con el `code_challenge` usado en la autorización

### 4️⃣ **Client ID incorrecto**
- El cliente configurado no coincide con el registrado

---

## ✅ Soluciones

### Solución 1: Verificar Redirect URI Exacta

El frontend debe enviar **EXACTAMENTE** la misma URI que está registrada en Autentic Sign.

**Verificar en el frontend que la URL sea:**
```javascript
redirectUri: "https://adres-autenticacion.centralspike.com/auth/callback"
```

**SIN:**
- Trailing slash
- Puerto :443
- Parámetros query string

### Solución 2: Agregar Logs Detallados

Agregar logs temporales para debug en el backend:

**En `AdresAuthService.cs`, línea ~196:**
```csharp
_logger.LogInformation("🔍 DEBUG - Intercambiando código");
_logger.LogInformation("  Code: {Code}", code.Substring(0, Math.Min(20, code.Length)) + "...");
_logger.LogInformation("  RedirectUri: {RedirectUri}", redirectUri);
_logger.LogInformation("  CodeVerifier: {CodeVerifier}", codeVerifier.Substring(0, Math.Min(10, codeVerifier.Length)) + "...");
_logger.LogInformation("  ClientId: {ClientId}", ClientId);
_logger.LogInformation("  TokenEndpoint: {TokenEndpoint}", TokenEndpoint);
```

### Solución 3: Verificar Configuración en Autentic Sign

En el panel de administración de Autentic Sign, verificar:

1. **Client ID:** `410c8553-f9e4-44b8-90e1-234dd7a8bcd4`
2. **Redirect URIs permitidas:** Debe incluir EXACTAMENTE:
   ```
   https://adres-autenticacion.centralspike.com/auth/callback
   ```
3. **PKCE habilitado:** Sí
4. **Client Secret:** Vacío (para clientes públicos/SPA)

### Solución 4: Probar con Client Secret (si es confidencial)

Si Autentic Sign requiere un `client_secret`:

**Actualizar `.env.server`:**
```bash
AdresAuth__ClientSecret=<SECRET_PROPORCIONADO_POR_AUTENTIC_SIGN>
```

---

## 🧪 Testing y Debugging

### Paso 1: Verificar en Logs del Backend

```bash
cd ~/adresPC
docker compose logs -f api | grep -E "Intercambiando|DEBUG|Error"
```

Buscar líneas como:
```
🔍 DEBUG - Intercambiando código
  RedirectUri: https://adres-autenticacion.centralspike.com/auth/callback
Error intercambiando código: 401 - {"error":"invalid_grant"...}
```

### Paso 2: Verificar Request Real

En el navegador (DevTools > Network):

**Request Payload a `/api/AdresAuth/token`:**
```json
{
  "code": "99DB5C4B7A98B...",
  "codeVerifier": "wkrbWJfsQxO7...",
  "redirectUri": "https://adres-autenticacion.centralspike.com/auth/callback"
}
```

**Verificar:**
- ✅ `code` no está vacío
- ✅ `codeVerifier` tiene ~43 caracteres
- ✅ `redirectUri` es exactamente la registrada

### Paso 3: Probar Flujo Completo de Nuevo

1. Borrar cookies y localStorage
2. Iniciar sesión desde cero
3. Observar en Network la petición a Autentic Sign
4. Copiar el `redirect_uri` usado en la autorización
5. Verificar que coincida con el enviado al intercambio

---

## 📋 Checklist de Verificación

En Autentic Sign (Panel de Admin):
- 🔲 Client ID correcto: `410c8553-f9e4-44b8-90e1-234dd7a8bcd4`
- 🔲 Redirect URI registrada: `https://adres-autenticacion.centralspike.com/auth/callback`
- 🔲 PKCE habilitado
- 🔲 Tipo de cliente: Public (SPA) o verificar si requiere secret

En el Backend (`.env.server`):
- 🔲 `AdresAuth__ClientId` coincide
- 🔲 `AdresAuth__RedirectUri` es exacta
- 🔲 `AdresAuth__ServerUrl=https://idp.autenticsign.com`
- 🔲 `AdresAuth__TokenEndpoint=/connect/token`

En el Frontend:
- 🔲 `redirectUri` enviada coincide exactamente
- 🔲 `code` y `codeVerifier` se están enviando
- 🔲 No hay trailing slash ni parámetros extra

---

## 🔧 Fix Temporal para Debug

Si quieres ver el error detallado del servidor de auth, actualiza el servicio:

**`AdresAuthService.cs`, línea ~201:**
```csharp
if (!response.IsSuccessStatusCode)
{
    _logger.LogWarning("❌ Error intercambiando código: {Status}", response.StatusCode);
    _logger.LogWarning("📄 Response body: {Body}", responseBody);
    _logger.LogWarning("🔍 Request data: code={Code}, redirect_uri={RedirectUri}, code_verifier={CodeVerifier}", 
        code.Substring(0, 20) + "...", 
        redirectUri, 
        codeVerifier.Substring(0, 10) + "...");
    
    var errorResponse = JsonSerializer.Deserialize<AdresErrorResponse>(responseBody);
    throw new UnauthorizedAccessException(errorResponse?.ErrorDescription ?? $"Code exchange failed: {responseBody}");
}
```

Esto mostrará el error completo en los logs.

---

## 🎯 Solución Más Probable

El problema es que el `redirect_uri` no coincide exactamente. 

**Acción inmediata:**

1. Verificar en Autentic Sign panel que la URL registrada sea:
   ```
   https://adres-autenticacion.centralspike.com/auth/callback
   ```
   (Sin trailing slash, sin puerto, sin http)

2. Si está registrada diferente, actualizarla o cambiar el frontend para usar la registrada

3. Rebuild y restart del backend:
   ```bash
   docker compose down
   docker compose build api
   docker compose up -d
   docker compose logs -f api
   ```

4. Probar de nuevo el flujo completo

---

## 📞 Siguiente Paso

Si el error persiste después de verificar el redirect_uri:

1. Contactar al equipo de Autentic Sign para:
   - Confirmar Client ID exacto
   - Confirmar Redirect URIs registradas
   - Verificar si requiere client_secret
   - Obtener logs del error desde su lado

2. Agregar los logs detallados en el backend y compartir la salida

---

## 💡 Notas

- Los códigos de autorización expiran rápido (1-5 min), no se pueden reusar
- El error `error_description=""` vacío sugiere que Autentic Sign no está dando detalles (por seguridad)
- PKCE es obligatorio en este flujo, el code_verifier debe coincidir exactamente con el challenge
