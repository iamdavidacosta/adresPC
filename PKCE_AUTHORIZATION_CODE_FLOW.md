# 🔐 Flujo de Authorization Code con PKCE - Autentic Sign

## 📋 Resumen

Autentic Sign requiere **PKCE (Proof Key for Code Exchange)** para el flujo de Authorization Code. Este es un estándar de seguridad (RFC 7636) que protege contra ataques de interceptación de código.

---

## 🔄 Cómo Funciona PKCE

### 1. Frontend Inicia el Flujo

El frontend genera:
- **code_verifier**: String aleatorio de 43-128 caracteres
- **code_challenge**: SHA256(code_verifier) en Base64 URL-safe

```javascript
// Ejemplo en JavaScript
function generateCodeVerifier() {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return base64UrlEncode(array);
}

async function generateCodeChallenge(verifier) {
    const encoder = new TextEncoder();
    const data = encoder.encode(verifier);
    const hash = await crypto.subtle.digest('SHA-256', data);
    return base64UrlEncode(new Uint8Array(hash));
}

function base64UrlEncode(buffer) {
    let str = '';
    const bytes = new Uint8Array(buffer);
    for (let i = 0; i < bytes.length; i++) {
        str += String.fromCharCode(bytes[i]);
    }
    return btoa(str)
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=/g, '');
}

// Generar y guardar en localStorage
const codeVerifier = generateCodeVerifier();
const codeChallenge = await generateCodeChallenge(codeVerifier);
localStorage.setItem('pkce_code_verifier', codeVerifier);
```

### 2. Redirigir a Autentic Sign

```javascript
const params = new URLSearchParams({
    client_id: '410c8553-f9e4-44b8-90e1-234dd7a8bcd4',
    redirect_uri: 'https://adres-autenticacion.centralspike.com/auth/callback',
    response_type: 'code',
    scope: 'openid extended_profile',
    code_challenge: codeChallenge,
    code_challenge_method: 'S256',
    state: 'random-state-value' // Opcional pero recomendado
});

window.location.href = `https://idp.autenticsign.com/connect/authorize?${params}`;
```

### 3. Callback - Recibir el Código

Después del login, Autentic Sign redirige a:
```
https://adres-autenticacion.centralspike.com/auth/callback?code=AUTHORIZATION_CODE&state=random-state-value
```

### 4. Intercambiar Código por Token

El frontend envía el **código** y el **code_verifier** al backend:

```javascript
const urlParams = new URLSearchParams(window.location.search);
const code = urlParams.get('code');
const codeVerifier = localStorage.getItem('pkce_code_verifier');

const response = await fetch('https://adres-autenticacion-back.centralspike.com/api/AdresAuth/token', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        code: code,
        codeVerifier: codeVerifier,
        redirectUri: 'https://adres-autenticacion.centralspike.com/auth/callback'
    })
});

const tokens = await response.json();
// { access_token, refresh_token, expires_in, token_type }

// Limpiar code_verifier
localStorage.removeItem('pkce_code_verifier');

// Guardar tokens
localStorage.setItem('access_token', tokens.access_token);
localStorage.setItem('refresh_token', tokens.refresh_token);
```

---

## 🎯 Flujo Completo (Opción 1: Frontend Maneja PKCE)

```
Frontend
│
├─ 1. Genera code_verifier aleatorio (43-128 chars)
├─ 2. Calcula code_challenge = SHA256(code_verifier)
├─ 3. Guarda code_verifier en localStorage
├─ 4. Redirige a: https://idp.autenticsign.com/connect/authorize
│      ?client_id=410c8553-f9e4-44b8-90e1-234dd7a8bcd4
│      &redirect_uri=https://adres-autenticacion.centralspike.com/auth/callback
│      &response_type=code
│      &scope=openid extended_profile
│      &code_challenge=BASE64_URL_ENCODED_HASH
│      &code_challenge_method=S256
│
User Login en Autentic Sign
│
├─ 5. Autentic Sign valida credenciales
├─ 6. Redirige a: https://adres-autenticacion.centralspike.com/auth/callback
│      ?code=AUTHORIZATION_CODE
│
Frontend (en /auth/callback)
│
├─ 7. Extrae 'code' de URL
├─ 8. Recupera code_verifier de localStorage
├─ 9. POST a: https://adres-autenticacion-back.centralspike.com/api/AdresAuth/token
│      Body: { code, codeVerifier, redirectUri }
│
Backend API
│
├─ 10. POST a: https://idp.autenticsign.com/connect/token
│       Body: grant_type=authorization_code
│             code=AUTHORIZATION_CODE
│             redirect_uri=https://adres-autenticacion.centralspike.com/auth/callback
│             client_id=410c8553-f9e4-44b8-90e1-234dd7a8bcd4
│             code_verifier=ORIGINAL_CODE_VERIFIER
│
Autentic Sign
│
├─ 11. Valida code_verifier:
│       - Calcula SHA256(code_verifier)
│       - Compara con code_challenge guardado
│       - Si coinciden, emite tokens
│
Backend API
│
├─ 12. Retorna: { access_token, refresh_token, expires_in, token_type }
│
Frontend
│
└─ 13. Guarda tokens en localStorage
   14. Redirige al dashboard
```

---

## 🎯 Flujo Simplificado (Opción 2: Backend Maneja PKCE con Sesión)

**Más fácil para el frontend - Recomendado**

```
Frontend
│
├─ 1. Redirige a: https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize
│
Backend API
│
├─ 2. Genera code_verifier aleatorio
├─ 3. Calcula code_challenge = SHA256(code_verifier)
├─ 4. Guarda code_verifier en sesión del servidor
├─ 5. Redirige a: https://idp.autenticsign.com/connect/authorize
│      ?client_id=410c8553-f9e4-44b8-90e1-234dd7a8bcd4
│      &redirect_uri=https://adres-autenticacion.centralspike.com/auth/callback
│      &response_type=code
│      &scope=openid extended_profile
│      &code_challenge=BASE64_URL_ENCODED_HASH
│      &code_challenge_method=S256
│
User Login en Autentic Sign
│
├─ 6. Autentic Sign valida credenciales
├─ 7. Redirige a: https://adres-autenticacion-back.centralspike.com/api/AdresAuth/callback
│      ?code=AUTHORIZATION_CODE
│
Backend API (en /callback)
│
├─ 8. Recupera code_verifier de sesión
├─ 9. POST a: https://idp.autenticsign.com/connect/token
│      Body: grant_type=authorization_code
│            code=AUTHORIZATION_CODE
│            redirect_uri=https://adres-autenticacion.centralspike.com/auth/callback
│            client_id=410c8553-f9e4-44b8-90e1-234dd7a8bcd4
│            code_verifier=CODE_VERIFIER_FROM_SESSION
│
Autentic Sign
│
├─ 10. Valida code_verifier y retorna tokens
│
Backend API
│
├─ 11. Limpia code_verifier de sesión
├─ 12. Redirige a: https://adres-autenticacion.centralspike.com
│       ?access_token=XXX&refresh_token=YYY&expires_in=3600
│
Frontend
│
└─ 13. Extrae tokens de URL
   14. Guarda en localStorage
   15. Muestra dashboard
```

---

## 🚀 Implementación en React

### Componente Login (Opción 2 - Recomendada)

```jsx
// src/pages/Login.jsx
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

export default function Login() {
    const navigate = useNavigate();

    const handleLogin = () => {
        // Simplemente redirige al backend - él maneja todo el PKCE
        window.location.href = 'https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize';
    };

    return (
        <div className="login-container">
            <h1>Iniciar Sesión</h1>
            <button onClick={handleLogin}>
                Ingresar con Autentic Sign
            </button>
        </div>
    );
}
```

### Componente Callback

```jsx
// src/pages/AuthCallback.jsx
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

export default function AuthCallback() {
    const navigate = useNavigate();

    useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        const accessToken = params.get('access_token');
        const refreshToken = params.get('refresh_token');
        const expiresIn = params.get('expires_in');

        if (accessToken) {
            // Guardar tokens
            localStorage.setItem('access_token', accessToken);
            localStorage.setItem('refresh_token', refreshToken);
            localStorage.setItem('token_expires_at', 
                Date.now() + (parseInt(expiresIn) * 1000));

            // Limpiar URL (remover tokens)
            window.history.replaceState({}, document.title, '/');

            // Redirigir al dashboard
            navigate('/dashboard');
        } else {
            // Error en autenticación
            const error = params.get('error');
            console.error('Error de autenticación:', error);
            navigate('/login');
        }
    }, [navigate]);

    return (
        <div className="loading">
            <p>Completando inicio de sesión...</p>
        </div>
    );
}
```

### Rutas

```jsx
// src/App.jsx
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Login from './pages/Login';
import AuthCallback from './pages/AuthCallback';
import Dashboard from './pages/Dashboard';

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/login" element={<Login />} />
                <Route path="/auth/callback" element={<AuthCallback />} />
                <Route path="/dashboard" element={<Dashboard />} />
                <Route path="/" element={<Login />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;
```

---

## 🧪 Pruebas

### 1. Probar URL de Autorización

Navega en tu navegador a:
```
https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize
```

Deberías ser redirigido a Autentic Sign con un URL similar a:
```
https://idp.autenticsign.com/connect/authorize?client_id=410c8553-f9e4-44b8-90e1-234dd7a8bcd4&redirect_uri=https%3A%2F%2Fadres-autenticacion.centralspike.com%2Fauth%2Fcallback&response_type=code&scope=openid%20extended_profile&code_challenge=XXXXXXX&code_challenge_method=S256
```

### 2. Verificar Sesión (Opcional)

Agregar endpoint de debug en el backend:

```csharp
[HttpGet("debug/session")]
public IActionResult CheckSession()
{
    var verifier = HttpContext.Session.GetString("pkce_code_verifier");
    return Ok(new { has_verifier = !string.IsNullOrEmpty(verifier) });
}
```

---

## ⚠️ Errores Comunes

### Error: "code challenge required"
**Causa**: Autentic Sign requiere PKCE pero no se envió `code_challenge`
**Solución**: ✅ Ya implementado - el backend genera automáticamente

### Error: "invalid_grant" en token exchange
**Causa**: El `code_verifier` no coincide con el `code_challenge` original
**Solución**: Verificar que el code_verifier se guarda y recupera correctamente

### Error: "PKCE code verifier not found in session"
**Causa**: La sesión expiró o no se compartió entre requests
**Solución**: 
- Verificar que `app.UseSession()` está antes de `app.MapControllers()`
- Asegurar que las cookies de sesión funcionan (dominio correcto)
- Aumentar `IdleTimeout` si es necesario

---

## 🔒 Seguridad

### ¿Por Qué PKCE?

PKCE protege contra:
1. **Authorization Code Interception**: Si un atacante intercepta el código, no puede usarlo sin el `code_verifier`
2. **Cross-Site Request Forgery (CSRF)**: El `state` parameter previene CSRF
3. **Malicious Apps**: Previene que apps maliciosas usen códigos robados

### Flujo de Validación

```
1. Frontend/Backend genera code_verifier aleatorio (128-256 bits)
2. Se calcula code_challenge = BASE64URL(SHA256(code_verifier))
3. Se envía code_challenge a Autentic Sign
4. Autentic Sign guarda code_challenge con el código de autorización
5. Al intercambiar el código, se envía code_verifier
6. Autentic Sign calcula SHA256(code_verifier) y compara
7. Si coincide ✅ → Emite tokens
8. Si no coincide ❌ → Rechaza solicitud
```

---

## 📊 Estado de Implementación

✅ **Backend - AdresAuthService**:
- `GenerateCodeVerifier()` - Genera verifier aleatorio
- `GenerateCodeChallenge()` - Calcula SHA256 hash
- `Base64UrlEncode()` - Codifica en formato URL-safe
- `GetAuthorizationUrl()` - Retorna (authUrl, codeVerifier)
- `ExchangeCodeForTokenAsync()` - Incluye code_verifier

✅ **Backend - AdresAuthController**:
- `GET /authorize` - Guarda verifier en sesión, redirige
- `GET /callback` - Recupera verifier, intercambia código
- `POST /token` - Acepta code_verifier en request body

✅ **Backend - Program.cs**:
- Sesiones distribuidas configuradas
- Cookies con SameSite=Lax
- Timeout de 10 minutos

⏳ **Frontend** (Pendiente):
- Componente Login
- Componente AuthCallback
- Manejo de tokens en localStorage

---

## 🎯 Próximos Pasos

1. **Desplegar backend actualizado**:
   ```bash
   git add -A
   git commit -m "feat: Add PKCE support for Authorization Code flow"
   git push origin stg
   ```

2. **Actualizar frontend** (crear componentes Login y AuthCallback)

3. **Probar flujo completo**:
   - Iniciar login desde frontend
   - Verificar redirección a Autentic Sign
   - Completar login
   - Verificar recepción de tokens

4. **Verificar en logs**:
   ```bash
   docker logs adres-api --tail 100 -f
   ```

---

**Estado**: ✅ Backend listo con PKCE
**Siguiente**: Desplegar y actualizar frontend
