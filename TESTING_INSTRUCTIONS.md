# 🧪 Instrucciones de Testing - PKCE Authorization Code

## 📋 El Problema del Redirect URI

Autentic Sign tiene registrado:
```
✅ https://adres-autenticacion.centralspike.com/auth/callback
```

Pero el backend necesita recibir el callback para intercambiar el código por tokens.

## ✅ Solución Implementada

He creado un **modo de testing** que usa un callback especial del backend.

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

---

## 🔄 Flujo de Testing Completo (RECOMENDADO)

### **Paso 1: Iniciar el flujo en modo testing**

Abre tu navegador y navega a:
```
https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize?mode=testing
```

**⚠️ IMPORTANTE**: El parámetro `?mode=testing` es crucial. Esto hace que:
1. El backend use su propio callback (`/api/AdresAuth/callback-handler`)
2. El callback esté registrado en Autentic Sign
3. No necesites configurar el frontend

**Qué sucede:**
1. El backend genera `code_verifier` y `code_challenge` (PKCE)
2. Guarda `code_verifier` en sesión del servidor
3. Te redirige a Autentic Sign

### **Paso 2: Login en Autentic Sign**

Ingresa tus credenciales en Autentic Sign:
- Usuario: `jorgea.hernandez@adres.gov.co` (o tu usuario)
- Password: Tu contraseña

### **Paso 3: ¡Listo! Obtienes tus tokens**

Después del login exitoso, verás una **página HTML hermosa** con:
- ✅ Access Token (copiable)
- 🔄 Refresh Token (copiable)  
- 📊 Información del token (expiración, scopes, etc.)
- � Instrucciones de uso

**Copia el Access Token** desde la interfaz y úsalo en tus requests.

---

## 🎯 Usar el Access Token

```bash
curl https://adres-autenticacion-back.centralspike.com/api/AdresAuth/me \
  -H "Authorization: Bearer TU_ACCESS_TOKEN_AQUI"
```

---

## 📊 Respuesta Esperada (Modo Testing)

Verás una página HTML con diseño profesional que muestra:

```
✅ Autenticación Exitosa

🔑 Access Token:
[Token largo con botón para copiar]

🔄 Refresh Token:
[Token de refresh con botón para copiar]

📊 Información del Token:
• Tipo: Bearer
• Expira en: 3600 segundos (60 minutos)
• Scopes: openid extended_profile

💡 Cómo usar:
Copia el Access Token y úsalo en tus requests:
Authorization: Bearer YOUR_ACCESS_TOKEN
```

---

## 🔧 Modo Normal (Para Producción con Frontend)

Cuando tengas el frontend listo, usa:
```
https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize
```

Esto:
1. Redirige a Autentic Sign
2. Autentic Sign devuelve al frontend (`https://adres-autenticacion.centralspike.com/auth/callback`)
3. El frontend llama a `POST /api/AdresAuth/token` con el código
4. El backend devuelve los tokens

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
