# 🧪 Instrucciones de Testing - PKCE Authorization Code

## 📋 Contexto

Ya realizaste el login exitosamente y Autentic Sign te devolvió un código de autorización:
```
code: 21377C5037735786670343B63FC428BFCD968CD64F6B9D71A94E21D6FB353280
```

Pero ese código **ya expiró** (los códigos de autorización solo duran ~5 minutos).

---

## 🚀 Deployment en Servidor

### 1. Conectar al servidor por SSH

```bash
ssh administrator@VPS-PERFORCE
```

### 2. Ejecutar deployment

```bash
cd ~/adresPC
git pull origin stg
cp adres.api/.env.server adres.api/.env
docker compose down
docker compose build api
docker compose up -d
```

### 3. Verificar que esté corriendo

```bash
docker logs adres-api --tail 50
```

Deberías ver:
```
✅ PKCE implementado
🔒 CORS configurado para: ...
📊 AdresAuth configurado
```

---

## 🔄 Flujo de Testing Completo

### **Paso 1: Iniciar el flujo desde el navegador**

Abre tu navegador y navega a:
```
https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize
```

**Qué sucede:**
1. El backend genera `code_verifier` y `code_challenge` (PKCE)
2. Guarda `code_verifier` en sesión del servidor
3. Te redirige a Autentic Sign con la URL:
   ```
   https://idp.autenticsign.com/connect/authorize?
     client_id=410c8553-f9e4-44b8-90e1-234dd7a8bcd4&
     redirect_uri=https://adres-autenticacion-back.centralspike.com/api/AdresAuth/callback&
     response_type=code&
     scope=openid extended_profile&
     code_challenge=XXXXXXX&
     code_challenge_method=S256
   ```

### **Paso 2: Login en Autentic Sign**

Ingresa tus credenciales en Autentic Sign:
- Usuario: `jorgea.hernandez@adres.gov.co` (o tu usuario)
- Password: Tu contraseña

### **Paso 3: Callback automático**

Después del login exitoso, Autentic Sign te redirige automáticamente a:
```
https://adres-autenticacion-back.centralspike.com/api/AdresAuth/callback?code=NUEVO_CODIGO
```

El backend automáticamente:
1. Recupera `code_verifier` de la sesión
2. Intercambia el código por tokens con Autentic Sign
3. Te muestra los tokens en formato JSON

---

## 📊 Respuesta Esperada (Opción 1 - JSON para Testing)

Para obtener la respuesta en JSON (ideal para testing), agrega `?json=true` al callback:

**URL modificada:**
```
https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize
```

Después del login, el callback automáticamente mostrará:

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjQ5...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "def502007e3a9c1f...",
  "scope": "openid extended_profile",
  "message": "✅ Autenticación exitosa! Guarda el access_token para usarlo en tus requests."
}
```

**Copia el `access_token`** y úsalo en tus requests:

```bash
curl https://adres-autenticacion-back.centralspike.com/api/AdresAuth/me \
  -H "Authorization: Bearer TU_ACCESS_TOKEN_AQUI"
```

---

## 🎯 Respuesta Esperada (Opción 2 - Redirect al Frontend)

Si NO agregas `?json=true`, el backend te redirigirá automáticamente al frontend:

```
https://adres-autenticacion.centralspike.com/?
  access_token=eyJhbGci...&
  refresh_token=def5020...&
  expires_in=3600
```

El frontend debe:
1. Extraer los tokens de la URL
2. Guardarlos en `localStorage`
3. Limpiar la URL

---

## ⚙️ Configuración Actual

**Redirect URI configurado:**
```
https://adres-autenticacion-back.centralspike.com/api/AdresAuth/callback
```

Esto significa que el **backend** maneja todo el callback. El frontend solo recibe los tokens ya procesados.

**Para cambiar a que el frontend maneje el callback:**
1. Editar `adres.api/.env.server`:
   ```bash
   AdresAuth__RedirectUri=https://adres-autenticacion.centralspike.com/auth/callback
   ```
2. Re-desplegar: `docker compose down && docker compose up -d`

---

## 🔍 Debugging

### Ver logs en tiempo real

```bash
ssh administrator@VPS-PERFORCE
docker logs adres-api --tail 100 -f
```

**Logs esperados:**
```
🔄 Redirigiendo a Autentic Sign con PKCE
📥 Callback recibido con código de autorización
✅ Token obtenido exitosamente
🔄 Redirigiendo al frontend: https://adres-autenticacion.centralspike.com
```

### Errores comunes

**"PKCE code verifier not found in session"**
- Causa: La sesión expiró (timeout 10 minutos) o las cookies no se comparten
- Solución: Reiniciar el flujo completo desde `/authorize`

**"invalid_grant"**
- Causa: El código de autorización ya fue usado o expiró
- Solución: Obtener un nuevo código iniciando desde `/authorize`

**"redirect_uri_mismatch"**
- Causa: El `redirect_uri` en Autentic Sign no coincide con el configurado
- Solución: Verificar que Autentic Sign tenga registrado: `https://adres-autenticacion-back.centralspike.com/api/AdresAuth/callback`

---

## 📝 Endpoints Disponibles

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/AdresAuth/authorize` | Inicia el flujo OAuth (redirige a Autentic Sign) |
| `GET /api/AdresAuth/callback` | Callback OAuth - intercambia código por token |
| `GET /api/AdresAuth/callback?json=true` | Callback que retorna JSON en lugar de redirect |
| `GET /api/AdresAuth/me` | Obtiene info del usuario autenticado (requiere token) |
| `POST /api/AdresAuth/refresh` | Renueva el access token usando refresh token |
| `GET /api/AdresAuth/config` | Configuración pública para frontend |

---

## ✅ Próximos Pasos

1. **Desplegar en servidor** (ver comandos arriba)
2. **Probar flujo completo** en navegador
3. **Copiar access_token** de la respuesta
4. **Usar el token** en tus requests API
5. **Actualizar frontend** para manejar el callback automáticamente

---

## 🎯 Uso del Access Token

Una vez que tengas el `access_token`, úsalo así:

### En curl:
```bash
curl https://adres-autenticacion-back.centralspike.com/api/AdresAuth/me \
  -H "Authorization: Bearer eyJhbGci..."
```

### En JavaScript (fetch):
```javascript
const response = await fetch('https://adres-autenticacion-back.centralspike.com/api/AdresAuth/me', {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});
const userData = await response.json();
```

### En Axios:
```javascript
const response = await axios.get('https://adres-autenticacion-back.centralspike.com/api/AdresAuth/me', {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});
```

---

**Estado**: ✅ Código listo para deployment
**Siguiente**: Ejecutar deployment en servidor y probar
