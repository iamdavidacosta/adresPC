# 🔍 Análisis del Error: invalid_grant

## ✅ Datos Confirmados

```
Response: {"error":"invalid_grant"}

Request enviado:
- grant_type: authorization_code
- code: 168BA0EF8BEB1118F6A6...
- redirect_uri: https://adres-autenticacion.centralspike.com/auth/callback
- client_id: 410c8553-f9e4-44b8-90e1-234dd7a8bcd4
- code_verifier: 4aOPA8ctnx...
- client_secret: NO (correcto para SPA)
```

**Todo se ve correcto técnicamente.** ✅

---

## 🎯 Causa del Problema

`invalid_grant` sin descripción adicional generalmente significa:

### 1️⃣ **Código Expirado** (MÁS PROBABLE)
- Los códigos OAuth expiran en 30 segundos a 5 minutos
- Si hay demora entre el callback y el intercambio, puede expirar

### 2️⃣ **Código Ya Usado**
- Los códigos son de un solo uso
- Si se reintenta la petición, fallará

### 3️⃣ **Code Verifier No Coincide** (PKCE)
- El hash del code_verifier no coincide con el code_challenge original
- **ESTE ES PROBABLEMENTE EL PROBLEMA**

---

## 🔍 Análisis PKCE

El flujo PKCE correcto es:

```
1. Frontend genera code_verifier aleatorio
   ↓
2. Frontend calcula code_challenge = SHA256(code_verifier)
   ↓
3. Frontend redirige a Autentic Sign con code_challenge
   ↓
4. Usuario se autentica
   ↓
5. Autentic Sign redirige con code
   ↓
6. Frontend envía code + code_verifier ORIGINAL
   ↓
7. Autentic Sign verifica: SHA256(code_verifier) == code_challenge
```

**El problema:** El `code_verifier` que se está enviando al backend **NO es el mismo** que generó el `code_challenge`.

---

## 🔴 El Problema Identificado

Mirando el código actual, veo que:

1. **El backend genera el code_verifier** en `/api/AdresAuth/authorize`
2. Lo incluye en el `state` para que el frontend lo reciba
3. El frontend debería extraerlo del `state` y enviarlo de vuelta

**PERO** el frontend probablemente está:
- ❌ Generando su propio code_verifier
- ❌ No extrayendo el code_verifier del state
- ❌ Usando un code_verifier diferente al que generó el code_challenge

---

## ✅ Solución

Hay dos enfoques:

### Opción A: Frontend maneja PKCE (Recomendado para SPA)

El **frontend** debe:
1. Generar el code_verifier
2. Calcular el code_challenge
3. Guardar el code_verifier en localStorage
4. Construir la URL de autorización con el code_challenge
5. Al recibir el callback, recuperar el code_verifier de localStorage
6. Enviarlo al backend

### Opción B: Backend maneja PKCE (Actual - Necesita Fix)

El **backend** genera todo, pero necesita:
1. El code_verifier debe llegar al frontend en el `state`
2. El frontend debe extraerlo y enviarlo de vuelta
3. O usar sesiones del lado del servidor (no ideal para SPA)

---

## 🚀 Recomendación: Opción A

**El frontend debe manejar PKCE completamente.** Esto es más seguro y estándar para SPAs.

### Implementación en el Frontend

```javascript
// 1. Generar PKCE
function generateCodeVerifier() {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return base64UrlEncode(array);
}

function base64UrlEncode(buffer) {
    return btoa(String.fromCharCode(...new Uint8Array(buffer)))
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=/g, '');
}

async function generateCodeChallenge(verifier) {
    const encoder = new TextEncoder();
    const data = encoder.encode(verifier);
    const hash = await crypto.subtle.digest('SHA-256', data);
    return base64UrlEncode(hash);
}

// 2. Al iniciar login
async function startLogin() {
    const codeVerifier = generateCodeVerifier();
    const codeChallenge = await generateCodeChallenge(codeVerifier);
    
    // Guardar para después
    localStorage.setItem('pkce_code_verifier', codeVerifier);
    
    // Construir URL
    const authUrl = new URL('https://idp.autenticsign.com/connect/authorize');
    authUrl.searchParams.set('client_id', '410c8553-f9e4-44b8-90e1-234dd7a8bcd4');
    authUrl.searchParams.set('redirect_uri', 'https://adres-autenticacion.centralspike.com/auth/callback');
    authUrl.searchParams.set('response_type', 'code');
    authUrl.searchParams.set('scope', 'openid extended_profile');
    authUrl.searchParams.set('code_challenge', codeChallenge);
    authUrl.searchParams.set('code_challenge_method', 'S256');
    
    // Redirigir
    window.location.href = authUrl.toString();
}

// 3. En el callback
async function handleCallback() {
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    
    // Recuperar code_verifier
    const codeVerifier = localStorage.getItem('pkce_code_verifier');
    
    if (!code || !codeVerifier) {
        console.error('Missing code or code_verifier');
        return;
    }
    
    // Intercambiar código por token
    const response = await fetch('https://adres-autenticacion-back.centralspike.com/api/AdresAuth/token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            code: code,
            codeVerifier: codeVerifier,
            redirectUri: 'https://adres-autenticacion.centralspike.com/auth/callback'
        })
    });
    
    // Limpiar
    localStorage.removeItem('pkce_code_verifier');
    
    const tokens = await response.json();
    // Usar tokens...
}
```

---

## 🔧 Fix Rápido: Verificar el Frontend

**Comando para verificar:**

En el navegador (DevTools > Console), cuando estés en el callback:

```javascript
// Ver si el code_verifier está en el state
const urlParams = new URLSearchParams(window.location.search);
const state = urlParams.get('state');
if (state) {
    const decoded = JSON.parse(atob(state));
    console.log('State:', decoded);
    console.log('Code Verifier from state:', decoded.cv);
}
```

Si `decoded.cv` existe, el frontend debe usarlo. Si no, debe generarlo antes de la autorización.

---

## 📞 Consulta a Autentic Sign

Mientras tanto, confirmar con ellos:

```
Hola,

Estamos recibiendo error "invalid_grant" sin descripción al intercambiar el código.

Pregunta específica sobre PKCE:
1. ¿PKCE es obligatorio para este cliente?
2. ¿Qué code_challenge_method soportan? (S256, plain)
3. ¿Pueden ver en sus logs si el code_verifier que enviamos coincide con el code_challenge?
4. ¿Cuál es el timeout del código de autorización?

Client ID: 410c8553-f9e4-44b8-90e1-234dd7a8bcd4

Datos que estamos enviando:
- grant_type: authorization_code
- code: (recibido del callback)
- redirect_uri: https://adres-autenticacion.centralspike.com/auth/callback
- client_id: 410c8553-f9e4-44b8-90e1-234dd7a8bcd4
- code_verifier: (generado con SHA256)
- NO client_secret (cliente público)

Gracias!
```

---

## 🎯 Próximos Pasos

1. **Verificar** quién está generando el code_verifier (frontend o backend)
2. **Confirmar** que el mismo code_verifier usado para el challenge se usa en el intercambio
3. **Implementar** PKCE completamente en el frontend (recomendado)
4. **Contactar** a Autentic Sign para confirmar requisitos PKCE

---

## 📄 Resumen

El problema **NO es** la redirect_uri ni el client_id. Todo eso está correcto.

El problema **ES** que el `code_verifier` enviado al intercambiar el código **no coincide** con el `code_challenge` que se usó al generar el código.

**Solución:** Implementar PKCE correctamente en el frontend desde el inicio del flujo.
