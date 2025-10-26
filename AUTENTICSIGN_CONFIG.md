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
docker logs -f adres-api

# Probar configuración
curl https://adres-autenticacion-back.centralspike.com/api/AdresAuth/config
```

---

## 🧪 Probar Autenticación

### 1. Login (con credenciales reales de Autentic Sign)
```bash
curl -X POST https://adres-autenticacion-back.centralspike.com/api/AdresAuth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "tu-usuario@example.com",
    "password": "tu-password"
  }'
```

**Respuesta esperada:**
```json
{
  "access_token": "eyJhbGci...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "...",
  "scope": "openid extended_profile"
}
```

### 2. Usar el Token
```bash
curl https://adres-autenticacion-back.centralspike.com/api/AdresAuth/me \
  -H "Authorization: Bearer TU_ACCESS_TOKEN"
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

### Flujo 1: Password Grant (Implementado)
```
Frontend → POST /api/AdresAuth/login
        → Backend → POST https://idp.autenticsign.com/connect/token
        → Backend ← access_token
Frontend ← access_token
```

### Flujo 2: Authorization Code (Pendiente)
```
Frontend → GET https://idp.autenticsign.com/connect/authorize
        → User Login en Autentic Sign
        → Redirect a /auth/callback?code=...
Frontend → POST /api/AdresAuth/token con code
        → Backend → POST https://idp.autenticsign.com/connect/token
Frontend ← access_token
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
| `/api/AdresAuth/login` | POST | Login con usuario/password |
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
- [ ] Pull en servidor
- [ ] Reconstruir contenedores
- [ ] Probar login con credenciales reales
- [ ] Verificar CORS desde frontend
- [ ] Probar flujo completo

---

## 🔍 Troubleshooting

### Error: "invalid_client"
- Verificar que `ClientId` sea correcto
- Si Autentic Sign requiere `client_secret`, agregarlo

### Error: "invalid_scope"
- Verificar que los scopes sean exactamente: `openid extended_profile`
- Revisar documentación de Autentic Sign para scopes disponibles

### Error: "invalid_grant"
- Credenciales de usuario incorrectas
- Verificar que el usuario exista en Autentic Sign

### Error: "redirect_uri_mismatch"
- Verificar que el Redirect URI esté registrado en Autentic Sign
- Debe ser exactamente: `https://adres-autenticacion.centralspike.com/auth/callback`

---

**Estado**: ✅ Configuración Lista
**Servidor IDP**: Autentic Sign (https://idp.autenticsign.com)
**Protocolo**: OpenID Connect / OAuth 2.0
