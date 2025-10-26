# 🚀 Guía Rápida - Frontend

## 1. Botón "Iniciar Sesión"

```jsx
const handleLogin = () => {
  window.location.href = 'https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize';
};
```

## 2. Ruta `/auth/callback`

```jsx
// src/pages/AuthCallback.jsx
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

export default function AuthCallback() {
    const navigate = useNavigate();

    useEffect(() => {
        handleCallback();
    }, []);

    const handleCallback = async () => {
        try {
            // 1. Extraer parámetros
            const params = new URLSearchParams(window.location.search);
            const code = params.get('code');
            const stateParam = params.get('state');

            // 2. Decodificar state para obtener code_verifier
            const stateJson = atob(stateParam);
            const stateData = JSON.parse(stateJson);
            const codeVerifier = stateData.cv;

            // 3. Intercambiar código por token
            const response = await fetch('https://adres-autenticacion-back.centralspike.com/api/AdresAuth/token', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    code: code,
                    codeVerifier: codeVerifier,
                    redirectUri: 'https://adres-autenticacion.centralspike.com/auth/callback'
                })
            });

            const tokens = await response.json();

            // 4. Guardar tokens
            localStorage.setItem('access_token', tokens.access_token);
            localStorage.setItem('refresh_token', tokens.refresh_token);

            // 5. Obtener datos del usuario
            const userResponse = await fetch('https://adres-autenticacion-back.centralspike.com/api/AdresAuth/me', {
                headers: { 'Authorization': `Bearer ${tokens.access_token}` }
            });

            const userData = await userResponse.json();
            localStorage.setItem('user', JSON.stringify(userData));

            // 6. Redirigir según rol
            if (userData.roles?.includes('Admin')) {
                navigate('/admin/dashboard');
            } else {
                navigate('/dashboard');
            }

        } catch (error) {
            console.error(error);
            navigate('/login');
        }
    };

    return <div>Procesando...</div>;
}
```

## 3. Endpoints Backend

| URL | Descripción |
|-----|-------------|
| `GET /api/AdresAuth/authorize` | Iniciar login → Redirige a Autentic Sign |
| `POST /api/AdresAuth/token` | Intercambiar código por tokens |
| `GET /api/AdresAuth/me` | Obtener datos del usuario (requiere Bearer token) |

## 4. Estructura de Datos

### POST /api/AdresAuth/token - Request
```json
{
  "code": "CODIGO_DE_AUTENTICACION",
  "codeVerifier": "CODE_VERIFIER_DEL_STATE",
  "redirectUri": "https://adres-autenticacion.centralspike.com/auth/callback"
}
```

### POST /api/AdresAuth/token - Response
```json
{
  "access_token": "eyJhbGci...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "def5020...",
  "scope": "openid extended_profile"
}
```

### GET /api/AdresAuth/me - Response
```json
{
  "email": "usuario@example.com",
  "name": "Nombre Usuario",
  "roles": ["Admin"],
  "permissions": ["read", "write"],
  "authenticated": true
}
```

---

**¡Listo!** Con esto tu frontend ya puede autenticarse con Autentic Sign.
