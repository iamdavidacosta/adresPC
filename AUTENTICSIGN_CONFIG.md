# 🔐 Configuración Autentic Sign - Quick Start

## 📋 Credenciales Proporcionadas

```
Authorization URL: https://idp.autenticsign.com/connect/authorize
Token URL: https://idp.autenticsign.com/connect/token
Client ID: 410c8553-f9e4-44b8-90e1-234dd7a8bcd4
Redirect URI: https://adres-autenticacion.centralspike.com/auth/callback
Scopes: openid extended_profile
```

---

## ⚙️ Archivos Ya Configurados

✅ **`appsettings.json`**
- ServerUrl: `https://idp.autenticsign.com`
- ClientId: `410c8553-f9e4-44b8-90e1-234dd7a8bcd4`
- Scopes: `openid extended_profile`
- Endpoints: `/connect/authorize`, `/connect/token`, etc.

✅ **`adres.api/.env.server`** (Producción)
- Todas las variables configuradas
- Redirect URI: `https://adres-autenticacion.centralspike.com/auth/callback`

✅ **`adres.api/.env.development.example`** (Desarrollo)
- Redirect URI: `http://localhost:3000/auth/callback`

---

## 🚀 Desplegar en Servidor

### 1. Pull y Configurar
```bash
cd ~/adresPC
git pull origin stg

# Copiar configuración de producción
cp adres.api/.env.server adres.api/.env
```

### 2. Verificar Configuración
```bash
cat adres.api/.env

# Deberías ver:
# AdresAuth__ServerUrl=https://idp.autenticsign.com
# AdresAuth__ClientId=410c8553-f9e4-44b8-90e1-234dd7a8bcd4
# AdresAuth__Scopes=openid extended_profile
```

### 3. Reconstruir y Desplegar
```bash
docker compose down
docker compose build api
docker compose up -d
```

### 4. Verificar
```bash
# Ver logs
ssh administrator@VPS-PERFORCE
cd ~/adresPC
git pull origin stg
docker compose down
docker compose build api
docker compose up -d

# Probar configuración
curl https://adres-autenticacion-back.centralspike.com/api/AdresAuth/config
```

---

## 🧪 Probar Autenticación

### 1. Authorization Code Flow con PKCE (RECOMENDADO)

**Desde el navegador**, navega a:
```
https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize
```

Esto te redirigirá a Autentic Sign para login. Después del login exitoso:
1. Serás redirigido de vuelta al callback
2. El backend intercambiará el código por tokens
3. Serás redirigido al frontend con los tokens

**Logs del servidor**:
```bash
docker logs adres-api --tail 50 -f
```

### 2. Password Grant (NO FUNCIONA - Autentic Sign no lo permite)
```bash
# Este endpoint retornará: "unauthorized_client"
curl -X POST https://adres-autenticacion-back.centralspike.com/api/AdresAuth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "usuario@example.com",
    "password": "password"
  }'
```

**Respuesta esperada**: 
```json
{
  "error": "unauthorized_client",
  "error_description": "Password grant is not allowed for this client"
}
```

---

## 📝 Notas Importantes

### Client Secret
- **No se proporcionó** en la documentación
- El servicio está configurado para funcionar **sin client_secret** (cliente público)
- Si Autentic Sign requiere client_secret, agregarlo en:
  ```bash
  AdresAuth__ClientSecret=tu-secret-aqui
  ```

### Scopes
- Configurados: `openid extended_profile`
- `openid`: Necesario para OpenID Connect
- `extended_profile`: Perfil extendido del usuario

### Redirect URI
- Producción: `https://adres-autenticacion.centralspike.com/auth/callback`
- Desarrollo: `http://localhost:3000/auth/callback`
- El frontend debe manejar esta ruta para recibir el código de autorización

---

## 🔄 Flujos de Autenticación Disponibles

### ✅ Flujo 1: Authorization Code con PKCE (Implementado y Recomendado)
```
Frontend → GET /api/AdresAuth/authorize
        → Backend genera code_verifier y code_challenge (PKCE)
        → Backend guarda code_verifier en sesión
        → Redirect a https://idp.autenticsign.com/connect/authorize
           con code_challenge y code_challenge_method=S256
        → User Login en Autentic Sign
        → Redirect a /api/AdresAuth/callback?code=...
Backend → Recupera code_verifier de sesión
        → POST https://idp.autenticsign.com/connect/token
           con code_verifier
Frontend ← Redirect con access_token en URL
```

**📖 Documentación completa**: Ver `PKCE_AUTHORIZATION_CODE_FLOW.md`

### ❌ Flujo 2: Password Grant (NO SOPORTADO por Autentic Sign)
```
Este flujo está deprecado y Autentic Sign no lo permite.
Error: "unauthorized_client"
```

---

## 🛠️ URLs Importantes

| Servicio | URL |
|----------|-----|
| **Autentic Sign (IDP)** | https://idp.autenticsign.com |
| **Authorization** | https://idp.autenticsign.com/connect/authorize |
| **Token** | https://idp.autenticsign.com/connect/token |
| **JWKS** | https://idp.autenticsign.com/.well-known/jwks.json |
| **UserInfo** | https://idp.autenticsign.com/connect/userinfo |
| **Revocation** | https://idp.autenticsign.com/connect/revocation |
| **Introspection** | https://idp.autenticsign.com/connect/introspect |
| | |
| **Backend API** | https://adres-autenticacion-back.centralspike.com |
| **Frontend** | https://adres-autenticacion.centralspike.com |
| **Callback** | https://adres-autenticacion.centralspike.com/auth/callback |

---

## 📊 Endpoints API Backend

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/AdresAuth/authorize` | GET | **[NUEVO]** Inicia Authorization Code flow con PKCE |
| `/api/AdresAuth/callback` | GET | **[NUEVO]** Callback OAuth - intercambia código por token |
| `/api/AdresAuth/token` | POST | **[NUEVO]** Intercambia código por token (para frontend) |
| `/api/AdresAuth/login` | POST | ~~Login con usuario/password~~ (DEPRECADO - no soportado) |
| `/api/AdresAuth/refresh` | POST | Renovar access token |
| `/api/AdresAuth/logout` | POST | Cerrar sesión |
| `/api/AdresAuth/me` | GET | Info usuario actual |
| `/api/AdresAuth/validate` | GET | Validar token |
| `/api/AdresAuth/config` | GET | Configuración para frontend |
| `/api/AdresAuth/jwks` | GET | Claves públicas JWKS |

---

## ✅ Checklist de Deployment

- [x] Configuración actualizada en `appsettings.json`
- [x] Variables de entorno en `.env.server`
- [x] Endpoints de Autentic Sign configurados
- [x] Client ID configurado
- [x] Scopes configurados
- [x] Redirect URI configurado
- [x] **PKCE implementado** (code_challenge, code_verifier)
- [x] **Sesiones habilitadas** para guardar code_verifier
- [ ] Pull en servidor
- [ ] Reconstruir contenedores
- [ ] Probar Authorization Code flow con PKCE
- [ ] Verificar CORS desde frontend
- [ ] Actualizar frontend para usar nuevo flujo
- [ ] Probar flujo completo end-to-end

---

## 🔍 Troubleshooting

### Error: "code_challenge_required"
✅ **SOLUCIONADO**: Implementado soporte PKCE
- Backend genera automáticamente `code_verifier` y `code_challenge`
- Se incluye `code_challenge_method=S256` en la URL de autorización

### Error: "unauthorized_client"
**Causa**: Intentando usar Password Grant (grant_type=password)
**Solución**: ✅ Usar Authorization Code flow (`GET /api/AdresAuth/authorize`)

### Error: "invalid_client"
- Verificar que `ClientId` sea correcto: `410c8553-f9e4-44b8-90e1-234dd7a8bcd4`
- Si Autentic Sign requiere `client_secret`, agregarlo en variables de entorno

### Error: "invalid_scope"
- Verificar que los scopes sean exactamente: `openid extended_profile`
- Revisar documentación de Autentic Sign para scopes disponibles

### Error: "invalid_grant"
- Código de autorización ya usado o expirado
- El `code_verifier` no coincide con el `code_challenge` original
- Reintentar el flujo completo desde `/authorize`

### Error: "redirect_uri_mismatch"
- Verificar que el Redirect URI esté registrado en Autentic Sign
- Debe ser exactamente: `https://adres-autenticacion.centralspike.com/auth/callback`

### Error: "PKCE code verifier not found in session"
- La sesión expiró (timeout de 10 minutos)
- Las cookies de sesión no se están compartiendo entre requests
- Solución: Reiniciar el flujo desde `/authorize`

---

**Estado**: ✅ Configuración Lista + PKCE Implementado
**Servidor IDP**: Autentic Sign (https://idp.autenticsign.com)
**Protocolo**: OpenID Connect / OAuth 2.0 con PKCE (RFC 7636)
**Flujo Activo**: Authorization Code con PKCE
**Documentación Completa**: Ver `PKCE_AUTHORIZATION_CODE_FLOW.md`
