# 🔐 URLs para el Equipo de Autenticación

## Información para Proporcionar

### ✅ **1. Redirect URI (después del login exitoso)**
```
https://adres-autenticacion.centralspike.com/auth/callback
```

### ✅ **2. Logout Redirect URI (después del logout)**
```
https://adres-autenticacion.centralspike.com/auth/logout
```

### ✅ **3. Error Redirect URI (en caso de error)**
```
https://adres-autenticacion.centralspike.com/auth/error
```

### ✅ **4. Allowed Origins (CORS)**
```
https://adres-autenticacion.centralspike.com
https://adres-autenticacion-back.centralspike.com
```

### ✅ **5. Tipo de Aplicación**
```
Single Page Application (SPA) / React
```

---

## 📧 Copiar y Pegar para el Email

```
Hola,

Necesitamos configurar OAuth 2.0 para nuestra aplicación ADRES.

URLs de configuración:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Redirect URI (Callback):
  https://adres-autenticacion.centralspike.com/auth/callback

✓ Logout Redirect URI:
  https://adres-autenticacion.centralspike.com/auth/logout

✓ Error Redirect URI:
  https://adres-autenticacion.centralspike.com/auth/error

✓ Allowed Origins (CORS):
  https://adres-autenticacion.centralspike.com
  https://adres-autenticacion-back.centralspike.com

✓ Tipo: Single Page Application (SPA)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Por favor proporciónennos:
- Authority/Issuer URL
- JWKS URI
- Client ID
- Discovery endpoint (.well-known/openid-configuration)

Backend API necesita validar tokens JWT.
Audience sugerido: "adres-api"

Gracias!
```

---

## 📋 Información que Recibirás

Una vez configurado, el equipo de auth te dará:

| Dato | Ejemplo | Dónde se usa |
|------|---------|--------------|
| **Authority** | `https://auth.adres.gov.co` | Backend `.env` |
| **JWKS URI** | `https://auth.adres.gov.co/.well-known/jwks.json` | Backend `.env` |
| **Client ID** | `adres-web-app` | Frontend config |
| **Discovery** | `https://auth.adres.gov.co/.well-known/openid-configuration` | Frontend config |

---

## 🔄 Flujo Simplificado

```
1. Usuario → https://adres-autenticacion.centralspike.com
                ↓
2. Click "Login" → Redirige a servidor de auth
                ↓
3. Usuario ingresa credenciales
                ↓
4. ✅ Login exitoso → Redirige a /auth/callback
                ↓
5. Frontend obtiene token
                ↓
6. Frontend llama a API con token
```

---

## 📞 Documentación Completa

Ver: `AUTH_URLS.md` para detalles completos y ejemplos por proveedor (Auth0, Azure AD, Keycloak, etc.)
