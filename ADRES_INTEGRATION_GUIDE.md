# 🔐 Guía de Integración con Sistema de Autenticación ADRES

## 📋 Descripción General

Este documento describe la integración del sistema de autenticación con el servidor OAuth2/OpenID Connect de ADRES.

---

## 🏗️ Arquitectura de Integración

### Componentes Creados

1. **AdresAuthService** (`Services/AdresAuthService.cs`)
   - Servicio para comunicación con el servidor de autenticación ADRES
   - Maneja tokens, refresh, validación y revocación

2. **AdresAuthController** (`Controllers/AdresAuthController.cs`)
   - Endpoints RESTful para autenticación
   - Compatible con flujo OAuth2 Password Grant

3. **Modelos** (`Models/AdresAuthModels.cs`)
   - `AdresAuthResponse`: Respuesta de autenticación con tokens
   - `AdresTokenClaims`: Claims del JWT
   - `JwksResponse`: Claves públicas para validar tokens

---

## 🔧 Configuración

### 1. Variables de Entorno (`.env` o `appsettings.json`)

```bash
# Servidor de Autenticación ADRES
AdresAuth__ServerUrl=https://auth.adres.gov.co
AdresAuth__ClientId=tu-client-id
AdresAuth__ClientSecret=tu-client-secret

# Endpoints (si difieren del estándar)
AdresAuth__TokenEndpoint=/oauth/token
AdresAuth__JwksEndpoint=/.well-known/jwks.json
AdresAuth__RevokeEndpoint=/oauth/revoke
AdresAuth__IntrospectEndpoint=/oauth/introspect

# JWT Validation
AUTH_AUTHORITY=https://auth.adres.gov.co
AUTH_AUDIENCE=adres-api
AUTH_USE_JWKS=true
AUTH_JWKS_URL=https://auth.adres.gov.co/.well-known/jwks.json
```

### 2. Configuración en `appsettings.json`

```json
{
  "AdresAuth": {
    "ServerUrl": "https://auth.adres.gov.co",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "Jwt": {
    "Authority": "https://auth.adres.gov.co",
    "Audience": "adres-api",
    "UseJwks": true,
    "JwksUrl": "https://auth.adres.gov.co/.well-known/jwks.json"
  }
}
```

---

## 🚀 Endpoints Disponibles

### 1. Login
```http
POST /api/AdresAuth/login
Content-Type: application/json

{
  "username": "usuario@example.com",
  "password": "password123"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "eyJhbGciOiJSUzI1NiIs...",
  "scope": "openid profile email"
}
```

### 2. Refresh Token
```http
POST /api/AdresAuth/refresh
Content-Type: application/json

{
  "refreshToken": "eyJhbGciOiJSUzI1NiIs..."
}
```

### 3. Validar Token
```http
GET /api/AdresAuth/validate
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Respuesta:**
```json
{
  "valid": true,
  "username": "usuario@example.com",
  "email": "usuario@example.com",
  "name": "Juan Pérez",
  "roles": ["user", "admin"],
  "permissions": ["read", "write"],
  "expires_at": "2025-10-26T15:30:00Z"
}
```

### 4. Logout
```http
POST /api/AdresAuth/logout
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

### 5. Información del Usuario
```http
GET /api/AdresAuth/me
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

### 6. Configuración (Frontend)
```http
GET /api/AdresAuth/config
```

**Respuesta:**
```json
{
  "loginUrl": "https://api.example.com/api/AdresAuth/login",
  "logoutUrl": "https://api.example.com/api/AdresAuth/logout",
  "refreshUrl": "https://api.example.com/api/AdresAuth/refresh",
  "validateUrl": "https://api.example.com/api/AdresAuth/validate",
  "meUrl": "https://api.example.com/api/AdresAuth/me",
  "authority": "https://auth.adres.gov.co"
}
```

### 7. JWKS (Claves Públicas)
```http
GET /api/AdresAuth/jwks
```

---

## 🔐 Flujo de Autenticación

### Flujo Completo

```mermaid
sequenceDiagram
    participant Frontend
    participant API
    participant ADRES

    Frontend->>API: POST /api/AdresAuth/login
    API->>ADRES: POST /oauth/token (grant_type=password)
    ADRES-->>API: access_token + refresh_token
    API-->>Frontend: tokens

    Frontend->>API: GET /api/Users (Authorization: Bearer token)
    API->>ADRES: Validate JWT (using JWKS)
    ADRES-->>API: Token válido
    API-->>Frontend: Datos del usuario

    Note over Frontend: Token expira

    Frontend->>API: POST /api/AdresAuth/refresh
    API->>ADRES: POST /oauth/token (grant_type=refresh_token)
    ADRES-->>API: nuevo access_token
    API-->>Frontend: nuevo token

    Frontend->>API: POST /api/AdresAuth/logout
    API->>ADRES: POST /oauth/revoke
    ADRES-->>API: Token revocado
    API-->>Frontend: Logout exitoso
```

---

## 💻 Integración Frontend (React)

### 1. Servicio de API (`services/api.js`)

```javascript
import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL;

// Configurar interceptor para agregar token
axios.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('access_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Interceptor para refresh automático
axios.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      const refreshToken = localStorage.getItem('refresh_token');
      if (refreshToken) {
        try {
          const response = await axios.post(`${API_BASE_URL}/api/AdresAuth/refresh`, {
            refreshToken
          });

          const { access_token, refresh_token } = response.data;
          localStorage.setItem('access_token', access_token);
          if (refresh_token) {
            localStorage.setItem('refresh_token', refresh_token);
          }

          originalRequest.headers.Authorization = `Bearer ${access_token}`;
          return axios(originalRequest);
        } catch (refreshError) {
          // Refresh falló, logout
          localStorage.removeItem('access_token');
          localStorage.removeItem('refresh_token');
          window.location.href = '/login';
          return Promise.reject(refreshError);
        }
      }
    }

    return Promise.reject(error);
  }
);

// Login
export const login = async (username, password) => {
  const response = await axios.post(`${API_BASE_URL}/api/AdresAuth/login`, {
    username,
    password
  });
  
  const { access_token, refresh_token } = response.data;
  localStorage.setItem('access_token', access_token);
  if (refresh_token) {
    localStorage.setItem('refresh_token', refresh_token);
  }
  
  return response.data;
};

// Logout
export const logout = async () => {
  const token = localStorage.getItem('access_token');
  if (token) {
    await axios.post(`${API_BASE_URL}/api/AdresAuth/logout`);
  }
  localStorage.removeItem('access_token');
  localStorage.removeItem('refresh_token');
};

// Obtener usuario actual
export const getCurrentUser = async () => {
  const response = await axios.get(`${API_BASE_URL}/api/AdresAuth/me`);
  return response.data;
};

// Validar token
export const validateToken = async () => {
  try {
    const response = await axios.get(`${API_BASE_URL}/api/AdresAuth/validate`);
    return response.data.valid;
  } catch {
    return false;
  }
};
```

### 2. Componente de Login (`Login.jsx`)

```javascript
import React, { useState } from 'react';
import { login } from '../services/api';
import { useNavigate } from 'react-router-dom';

export default function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      await login(username, password);
      navigate('/dashboard');
    } catch (err) {
      setError(err.response?.data?.error_description || 'Error de autenticación');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="text"
        placeholder="Usuario"
        value={username}
        onChange={(e) => setUsername(e.target.value)}
        required
      />
      <input
        type="password"
        placeholder="Contraseña"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
      />
      {error && <div className="error">{error}</div>}
      <button type="submit" disabled={loading}>
        {loading ? 'Iniciando sesión...' : 'Iniciar Sesión'}
      </button>
    </form>
  );
}
```

---

## 🧪 Pruebas

### Prueba con cURL

```bash
# 1. Login
curl -X POST https://api.example.com/api/AdresAuth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"usuario@example.com","password":"password123"}'

# 2. Usar el token
curl https://api.example.com/api/AdresAuth/me \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."

# 3. Refresh
curl -X POST https://api.example.com/api/AdresAuth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"eyJhbGciOiJSUzI1NiIs..."}'

# 4. Logout
curl -X POST https://api.example.com/api/AdresAuth/logout \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
```

---

## 🔍 Troubleshooting

### Error: "Unable to connect to ADRES authentication server"
- Verificar que `AdresAuth__ServerUrl` esté correctamente configurado
- Verificar conectividad de red al servidor ADRES
- Revisar logs: `docker logs adres-api`

### Error: "invalid_grant" o "invalid_client"
- Verificar `AdresAuth__ClientId` y `AdresAuth__ClientSecret`
- Confirmar que las credenciales del usuario sean correctas

### Error: "Token validation failed"
- Verificar que el JWKS endpoint sea accesible
- Confirmar que `AUTH_JWKS_URL` esté correctamente configurado
- El token puede haber expirado - usar refresh

---

## 📊 Monitoreo

Logs importantes a revisar:

```bash
# Ver logs del contenedor API
docker logs -f adres-api

# Filtrar solo logs de autenticación
docker logs adres-api 2>&1 | grep "ADRES\|Auth"

# Ver intentos de login
docker logs adres-api 2>&1 | grep "Intentando autenticación"
```

---

## 🔒 Seguridad

### Recomendaciones

1. **HTTPS Obligatorio**: Nunca usar HTTP en producción
2. **Client Secret**: Mantener seguro, usar variables de entorno
3. **Token Storage**: En el frontend, usar `httpOnly` cookies cuando sea posible
4. **Refresh Tokens**: Rotar refresh tokens en cada uso
5. **Timeouts**: Configurar timeouts adecuados para las llamadas HTTP

---

## 📝 Checklist de Implementación

- [ ] Configurar `AdresAuth__ServerUrl`
- [ ] Obtener y configurar `ClientId` y `ClientSecret`
- [ ] Verificar conectividad al servidor ADRES
- [ ] Probar endpoint `/api/AdresAuth/login`
- [ ] Probar endpoint `/api/AdresAuth/refresh`
- [ ] Implementar frontend con manejo de tokens
- [ ] Configurar interceptores para refresh automático
- [ ] Probar logout y revocación de tokens
- [ ] Configurar CORS correctamente
- [ ] Implementar manejo de errores
- [ ] Agregar logging
- [ ] Documentar credenciales en gestor de secretos
- [ ] Realizar pruebas end-to-end

---

**Última actualización**: 26 de octubre de 2025
