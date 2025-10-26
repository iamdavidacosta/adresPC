# 🎨 Integración Frontend - Authorization Code con PKCE

## 📋 Flujo Completo (SIMPLIFICADO)

```
1. [Frontend] Botón "Iniciar Sesión" 
   → window.location.href = 'https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize'

2. [Backend /authorize]
   → Genera code_verifier y code_challenge (PKCE)
   → Incluye code_verifier en state (Base64 JSON: {returnUrl, cv})
   → Redirige a Autentic Sign

3. [Autentic Sign]
   → Usuario ingresa credenciales
   → Redirige a: https://adres-autenticacion.centralspike.com/auth/callback?code=XXX&state=YYY

4. [Frontend /auth/callback]
   → Decodifica state → obtiene code_verifier
   → POST /api/AdresAuth/token {code, codeVerifier}
   → Recibe {access_token, refresh_token}
   → GET /api/AdresAuth/me con Bearer token
   → Obtiene datos del usuario
   → Redirige según rol
```

---

## 🔧 Implementación en React

### 1. Componente Login

```jsx
// src/pages/Login.jsx
import React from 'react';
import { useNavigate } from 'react-router-dom';

export default function Login() {
    const navigate = useNavigate();

    const handleLogin = () => {
        // Redirigir al backend para iniciar el flujo OAuth
        window.location.href = 'https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize';
    };

    return (
        <div className="login-container">
            <h1>Bienvenido a ADRES</h1>
            <button onClick={handleLogin} className="btn-login">
                🔐 Iniciar Sesión con Autentic Sign
            </button>
        </div>
    );
}
```

### 2. Componente Callback (El más importante)

```jsx
// src/pages/AuthCallback.jsx
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

export default function AuthCallback() {
    const navigate = useNavigate();
    const [status, setStatus] = useState('Procesando autenticación...');
    const [error, setError] = useState(null);

    useEffect(() => {
        handleCallback();
    }, []);

    const handleCallback = async () => {
        try {
            // 1. Extraer parámetros de la URL
            const params = new URLSearchParams(window.location.search);
            const code = params.get('code');
            const stateParam = params.get('state');
            const errorParam = params.get('error');

            if (errorParam) {
                throw new Error(`Error de Autentic Sign: ${errorParam}`);
            }

            if (!code) {
                throw new Error('No se recibió código de autorización');
            }

            setStatus('Decodificando parámetros...');

            // 2. Decodificar el state para obtener el code_verifier
            let codeVerifier = null;
            let returnUrl = '/';

            if (stateParam) {
                try {
                    const stateJson = atob(stateParam); // Decodificar Base64
                    const stateData = JSON.parse(stateJson);
                    codeVerifier = stateData.cv; // code_verifier
                    returnUrl = stateData.returnUrl || '/';
                } catch (e) {
                    console.error('Error decodificando state:', e);
                }
            }

            if (!codeVerifier) {
                throw new Error('No se encontró code_verifier en el state');
            }

            setStatus('Intercambiando código por token...');

            // 3. Intercambiar el código por tokens
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

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error_description || errorData.error || 'Error desconocido');
            }

            const tokens = await response.json();

            setStatus('Guardando tokens...');

            // 4. Guardar tokens en localStorage
            localStorage.setItem('access_token', tokens.access_token);
            localStorage.setItem('refresh_token', tokens.refresh_token);
            localStorage.setItem('token_expires_at', Date.now() + (tokens.expires_in * 1000));

            setStatus('Obteniendo información del usuario...');

            // 5. Obtener información del usuario
            const userResponse = await fetch('https://adres-autenticacion-back.centralspike.com/api/AdresAuth/me', {
                headers: {
                    'Authorization': `Bearer ${tokens.access_token}`
                }
            });

            if (!userResponse.ok) {
                throw new Error('Error obteniendo información del usuario');
            }

            const userData = await userResponse.json();

            // 6. Guardar datos del usuario
            localStorage.setItem('user', JSON.stringify(userData));

            setStatus('Redirigiendo...');

            // 7. Limpiar URL (remover código y state)
            window.history.replaceState({}, document.title, '/');

            // 8. Redirigir según el rol del usuario
            if (userData.roles && userData.roles.length > 0) {
                const role = userData.roles[0];
                switch (role) {
                    case 'Admin':
                        navigate('/admin/dashboard');
                        break;
                    case 'Manager':
                        navigate('/manager/dashboard');
                        break;
                    case 'User':
                        navigate('/user/dashboard');
                        break;
                    default:
                        navigate('/dashboard');
                }
            } else {
                navigate('/dashboard');
            }

        } catch (error) {
            console.error('Error en callback:', error);
            setError(error.message);
            setStatus('Error');
            
            // Redirigir a login después de 3 segundos
            setTimeout(() => {
                navigate('/login');
            }, 3000);
        }
    };

    if (error) {
        return (
            <div className="callback-container error">
                <h1>❌ Error de Autenticación</h1>
                <p>{error}</p>
                <p>Redirigiendo al login...</p>
            </div>
        );
    }

    return (
        <div className="callback-container">
            <div className="spinner"></div>
            <h1>Completando inicio de sesión...</h1>
            <p>{status}</p>
        </div>
    );
}
```

### 3. Componente Dashboard (Ejemplo)

```jsx
// src/pages/Dashboard.jsx
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

export default function Dashboard() {
    const navigate = useNavigate();
    const [user, setUser] = useState(null);

    useEffect(() => {
        // Verificar si hay token
        const token = localStorage.getItem('access_token');
        if (!token) {
            navigate('/login');
            return;
        }

        // Cargar datos del usuario
        const userData = localStorage.getItem('user');
        if (userData) {
            setUser(JSON.parse(userData));
        }
    }, [navigate]);

    const handleLogout = () => {
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('token_expires_at');
        localStorage.removeItem('user');
        navigate('/login');
    };

    if (!user) {
        return <div>Cargando...</div>;
    }

    return (
        <div className="dashboard">
            <h1>Bienvenido, {user.name}</h1>
            <p>Email: {user.email}</p>
            <p>Roles: {user.roles?.join(', ')}</p>
            <button onClick={handleLogout}>Cerrar Sesión</button>
        </div>
    );
}
```

### 4. Configurar Rutas

```jsx
// src/App.jsx
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Login from './pages/Login';
import AuthCallback from './pages/AuthCallback';
import Dashboard from './pages/Dashboard';
import PrivateRoute from './components/PrivateRoute';

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/login" element={<Login />} />
                <Route path="/auth/callback" element={<AuthCallback />} />
                <Route 
                    path="/dashboard" 
                    element={
                        <PrivateRoute>
                            <Dashboard />
                        </PrivateRoute>
                    } 
                />
                <Route path="/" element={<Login />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;
```

### 5. Componente PrivateRoute (Proteger rutas)

```jsx
// src/components/PrivateRoute.jsx
import React from 'react';
import { Navigate } from 'react-router-dom';

export default function PrivateRoute({ children }) {
    const token = localStorage.getItem('access_token');
    const expiresAt = localStorage.getItem('token_expires_at');

    // Verificar si hay token y no ha expirado
    if (!token || (expiresAt && Date.now() > parseInt(expiresAt))) {
        return <Navigate to="/login" />;
    }

    return children;
}
```

---

## 🔑 API Helper para Requests Autenticados

```javascript
// src/utils/api.js

const API_URL = 'https://adres-autenticacion-back.centralspike.com';

export async function apiRequest(endpoint, options = {}) {
    const token = localStorage.getItem('access_token');
    
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${API_URL}${endpoint}`, {
        ...options,
        headers
    });

    // Si el token expiró, redirigir a login
    if (response.status === 401) {
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('user');
        window.location.href = '/login';
        throw new Error('Token expirado');
    }

    return response;
}

// Ejemplo de uso:
// const data = await apiRequest('/api/AdresAuth/me').then(r => r.json());
```

---

## 📊 Endpoints del Backend

| Endpoint | Método | Descripción | Requiere Token |
|----------|--------|-------------|----------------|
| `/api/AdresAuth/authorize` | GET | Inicia flujo OAuth | ❌ |
| `/api/AdresAuth/token` | POST | Intercambia código por token | ❌ |
| `/api/AdresAuth/me` | GET | Obtiene datos del usuario | ✅ |
| `/api/AdresAuth/refresh` | POST | Renueva access token | ✅ |
| `/api/AdresAuth/logout` | POST | Cierra sesión | ✅ |

---

## 🧪 Testing

### Probar flujo completo:

1. Ir a `/login`
2. Click en "Iniciar Sesión"
3. Ingresar credenciales en Autentic Sign
4. Ser redirigido a `/auth/callback`
5. Ver proceso de intercambio de tokens
6. Ser redirigido a dashboard según rol

### Ver logs en consola del navegador:

```javascript
// Ver tokens guardados
console.log('Access Token:', localStorage.getItem('access_token'));
console.log('User Data:', JSON.parse(localStorage.getItem('user')));
```

---

## ⚠️ Puntos Importantes

### 1. El state contiene el code_verifier

El backend incluye el `code_verifier` en el parámetro `state` como JSON en Base64:

```javascript
// Decodificar state
const stateJson = atob(stateParam); // "{ \"returnUrl\": \"/\", \"cv\": \"code_verifier_aqui\" }"
const stateData = JSON.parse(stateJson);
const codeVerifier = stateData.cv;
```

### 2. El redirect_uri debe coincidir exactamente

```javascript
redirectUri: 'https://adres-autenticacion.centralspike.com/auth/callback'
```

### 3. Manejo de errores

Siempre verificar:
- Si hay parámetro `error` en la URL (error de Autentic Sign)
- Si hay `code` en la URL
- Si el `state` contiene `code_verifier`
- Si la respuesta del backend es exitosa

---

## 🎨 CSS de Ejemplo para Callback

```css
/* src/pages/AuthCallback.css */
.callback-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
}

.spinner {
    width: 60px;
    height: 60px;
    border: 6px solid rgba(255, 255, 255, 0.3);
    border-top: 6px solid white;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 20px;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.callback-container.error {
    background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
}
```

---

**Estado**: ✅ Listo para integración
**Siguiente**: Implementar componentes en el frontend React
