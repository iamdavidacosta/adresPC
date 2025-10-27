# 🔐 URLs de Autenticación para Configurar

## 📋 URLs para el Equipo de Autenticación

Estas son las URLs que debes proporcionar al equipo que maneja tu servidor de autenticación (OAuth 2.0 / OpenID Connect).

---

## 🎯 URLs Principales

### 1️⃣ **Redirect URI (Callback después del login exitoso)**

```
https://adres-autenticacion.centralspike.com/auth/callback
```

**Descripción:**  
Después de que el usuario se autentica correctamente en el servidor de autenticación, será redirigido a esta URL con el token de acceso o código de autorización.

**También conocido como:**
- Redirect URI
- Callback URL
- Return URL
- Reply URL (Azure AD)

---

### 2️⃣ **Logout Redirect URI (Después del logout)**

```
https://adres-autenticacion.centralspike.com/auth/logout
```

**Descripción:**  
Después de cerrar sesión, el usuario será redirigido a esta URL.

**También conocido como:**
- Post Logout Redirect URI
- Sign-out URL
- Logout Callback URL

---

### 3️⃣ **Error Redirect URI (En caso de error de autenticación)**

```
https://adres-autenticacion.centralspike.com/auth/error
```

**Descripción:**  
Si hay un error durante la autenticación, el usuario será redirigido aquí.

---

## 🌐 Información Adicional

### **Dominio de la Aplicación (Frontend)**
```
https://adres-autenticacion.centralspike.com
```

### **Dominio de la API (Backend)**
```
https://adres-autenticacion-back.centralspike.com
```

### **Allowed Origins para CORS**
```
https://adres-autenticacion.centralspike.com
https://adres-autenticacion-back.centralspike.com
```

---

## 📝 Configuración Típica por Proveedor

### **Auth0**
```
Application Type: Single Page Application (SPA)
Allowed Callback URLs: https://adres-autenticacion.centralspike.com/auth/callback
Allowed Logout URLs: https://adres-autenticacion.centralspike.com/auth/logout
Allowed Web Origins: https://adres-autenticacion.centralspike.com
Allowed Origins (CORS): https://adres-autenticacion.centralspike.com
```

### **Azure AD (Entra ID)**
```
Platform: Single-page application
Redirect URIs: https://adres-autenticacion.centralspike.com/auth/callback
Front-channel logout URL: https://adres-autenticacion.centralspike.com/auth/logout
```

### **Keycloak**
```
Client Type: public
Valid Redirect URIs: https://adres-autenticacion.centralspike.com/auth/callback
Valid Post Logout Redirect URIs: https://adres-autenticacion.centralspike.com/auth/logout
Web Origins: https://adres-autenticacion.centralspike.com
```

### **Okta**
```
Application Type: Single-Page App
Login redirect URIs: https://adres-autenticacion.centralspike.com/auth/callback
Logout redirect URIs: https://adres-autenticacion.centralspike.com/auth/logout
Initiate login URI: https://adres-autenticacion.centralspike.com
```

---

## 🔑 Información que Necesitas del Servidor de Autenticación

Una vez configurado, el equipo de autenticación debe proporcionarte:

### 1️⃣ **Authority (Issuer)**
```
Ejemplo: https://auth.adres.gov.co
```

### 2️⃣ **JWKS URI (JSON Web Key Set)**
```
Ejemplo: https://auth.adres.gov.co/.well-known/jwks.json
```

### 3️⃣ **Client ID**
```
Ejemplo: adres-web-app
```

### 4️⃣ **Audience (para la API)**
```
Ejemplo: adres-api
```

### 5️⃣ **OpenID Configuration (Discovery endpoint)**
```
Ejemplo: https://auth.adres.gov.co/.well-known/openid-configuration
```

---

## 📧 Email Template para el Equipo de Autenticación

```
Asunto: Configuración de Cliente OAuth 2.0 para ADRES Web App

Hola,

Necesitamos configurar un nuevo cliente OAuth 2.0 / OpenID Connect para nuestra aplicación web.

Información de la aplicación:
- Nombre: ADRES Web App
- Tipo: Single Page Application (SPA)
- Framework: React

URLs de configuración:
- Redirect URI (Callback): https://adres-autenticacion.centralspike.com/auth/callback
- Logout Redirect URI: https://adres-autenticacion.centralspike.com/auth/logout
- Error Redirect URI: https://adres-autenticacion.centralspike.com/auth/error
- Origen permitido (CORS): https://adres-autenticacion.centralspike.com

Información del backend API:
- Dominio: https://adres-autenticacion-back.centralspike.com
- Audience: adres-api

Por favor, proporciónennos:
1. Authority/Issuer URL
2. JWKS URI
3. Client ID
4. Discovery endpoint (.well-known/openid-configuration)

Gracias,
[Tu nombre]
```

---

## 🔄 Flujo de Autenticación

```
1. Usuario visita: https://adres-autenticacion.centralspike.com
   ↓
2. Click en "Iniciar Sesión"
   ↓
3. Redirige a servidor de autenticación (ej: https://auth.adres.gov.co/login)
   ↓
4. Usuario ingresa credenciales
   ↓
5. Servidor de autenticación valida credenciales
   ↓
6. Redirige a: https://adres-autenticacion.centralspike.com/auth/callback?code=xxx
   ↓
7. Frontend intercambia código por token
   ↓
8. Frontend usa token para llamar a la API
```

---

## 🛠️ Implementación en el Frontend

Una vez que tengas la información del servidor de autenticación, necesitarás implementar un cliente OAuth 2.0. Puedes usar:

### **Opción 1: OIDC Client (Recomendado)**
```bash
npm install oidc-client-ts
```

### **Opción 2: Auth0 React SDK**
```bash
npm install @auth0/auth0-react
```

### **Opción 3: React OIDC Context**
```bash
npm install @axa-fr/react-oidc
```

---

## 📋 Checklist

Información a proporcionar al equipo de auth:
- ✅ Redirect URI: `https://adres-autenticacion.centralspike.com/auth/callback`
- ✅ Logout URI: `https://adres-autenticacion.centralspike.com/auth/logout`
- ✅ Error URI: `https://adres-autenticacion.centralspike.com/auth/error`
- ✅ Allowed Origins: `https://adres-autenticacion.centralspike.com`
- ✅ Tipo de aplicación: Single Page Application (SPA)

Información que debes recibir:
- 🔲 Authority/Issuer URL
- 🔲 JWKS URI
- 🔲 Client ID
- 🔲 Discovery endpoint
- 🔲 Scopes disponibles

---

## 💡 Notas Importantes

1. **Todas las URLs deben usar HTTPS** en producción
2. **No incluir trailing slash** al final de las URLs (a menos que sea requerido)
3. **Verificar que el servidor de auth permita CORS** para tu dominio
4. **El simulador actual será reemplazado** por el flujo OAuth real
5. **Probar en staging antes de producción**

---

## 📞 Siguiente Paso

Una vez que el equipo de autenticación te proporcione la configuración, actualizar:

**Backend (`adres.api/.env.production`):**
```bash
AUTH_AUTHORITY=https://[URL-DEL-SERVIDOR-AUTH]
AUTH_AUDIENCE=adres-api
AUTH_USE_JWKS=true
AUTH_JWKS_URL=https://[URL-DEL-SERVIDOR-AUTH]/.well-known/jwks.json
```

**Frontend (nuevo archivo de configuración):**
```javascript
// src/config/auth.js
export const authConfig = {
  authority: 'https://[URL-DEL-SERVIDOR-AUTH]',
  client_id: '[CLIENT-ID]',
  redirect_uri: 'https://adres-autenticacion.centralspike.com/auth/callback',
  post_logout_redirect_uri: 'https://adres-autenticacion.centralspike.com/auth/logout',
  response_type: 'code',
  scope: 'openid profile email'
};
```
