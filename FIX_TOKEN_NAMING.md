# ✅ FIX: JWT Malformed Error - snake_case vs camelCase

## 🔴 El Error

```
❌ Error al autenticar token JWT
Microsoft.IdentityModel.Tokens.SecurityTokenMalformedException: 
IDX14100: JWT is not well formed, there are no dots (.).
```

**Endpoint:** `GET /api/AdresAuth/me`  
**Status:** 401 Unauthorized

---

## 🔍 Causa Raíz

El backend devuelve la respuesta en **snake_case** (estándar OAuth 2.0):
```json
{
  "access_token": "eyJhbGc...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

Pero el frontend estaba buscando **camelCase**:
```javascript
const token = tokenData.accessToken;  // ❌ undefined
localStorage.setItem('access_token', tokenData.accessToken);  // ❌ undefined
```

**Resultado:**
- `tokenData.accessToken` es `undefined`
- Se envía: `Authorization: Bearer undefined`
- Backend intenta validar `"undefined"` como JWT
- Error: "JWT is not well formed, there are no dots"

---

## ✅ Solución Aplicada

**Modificado:** `adres-web/src/pages/AuthCallback.js`

### Antes:
```javascript
const tokenData = await tokenResponse.json();

localStorage.setItem('access_token', tokenData.accessToken);  // ❌ undefined
if (tokenData.refreshToken) {
  localStorage.setItem('refresh_token', tokenData.refreshToken);
}

const userResponse = await fetch('.../api/AdresAuth/me', {
  headers: {
    'Authorization': `Bearer ${tokenData.accessToken}`  // ❌ undefined
  }
});
```

### Después:
```javascript
const tokenData = await tokenResponse.json();

// Usar snake_case (estándar OAuth)
if (tokenData.access_token) {
  localStorage.setItem('access_token', tokenData.access_token);
} else {
  throw new Error('No se recibió access_token del servidor');
}

if (tokenData.refresh_token) {
  localStorage.setItem('refresh_token', tokenData.refresh_token);
}

const userResponse = await fetch('.../api/AdresAuth/me', {
  headers: {
    'Authorization': `Bearer ${tokenData.access_token}`  // ✅ correcto
  }
});
```

**Cambios:**
1. ✅ Usar `access_token` en lugar de `accessToken`
2. ✅ Usar `refresh_token` en lugar de `refreshToken`
3. ✅ Agregar validación que el token no sea `undefined`
4. ✅ Agregar log para debug

---

## 🚀 Desplegar

### Frontend (Local - para rebuild):
```bash
cd c:\work\adres\adresPC\adres-web

# Si estás desarrollando local
npm start

# O rebuild de Docker
cd c:\work\adres\adresPC
docker compose build web
docker compose up -d
```

### Backend (Ya está correcto):
```bash
# No requiere cambios adicionales
# El backend ya devuelve snake_case (estándar OAuth)
```

---

## 🧪 Probar

1. **Borrar localStorage y cookies** del navegador
   ```javascript
   // En DevTools Console:
   localStorage.clear();
   ```

2. Ir a `https://adres-autenticacion.centralspike.com`

3. Iniciar sesión con Autentic Sign

4. **Verificar en DevTools Console:**
   ```
   ✅ Tokens recibidos: { access_token: 'eyJhbGc...', token_type: 'Bearer', ... }
   ```

5. **Verificar en DevTools > Application > Local Storage:**
   - `access_token`: debe tener un JWT válido (con puntos)

6. **El dashboard debería cargar** ✅

---

## 📋 Verificación en Logs del Backend

**Ahora deberías ver:**
```
✅ Token JWT validado correctamente
```

**En lugar de:**
```
❌ Error al autenticar token JWT
IDX14100: JWT is not well formed
```

---

## 🎯 Resumen de la Cadena de Fixes

1. ✅ **PKCE Mismatch** - Corregido el doble llamado a `GetAuthorizationUrl()`
2. ✅ **JWT Audience** - Agregado múltiples audiences aceptadas
3. ✅ **snake_case vs camelCase** - Frontend ahora usa snake_case (este fix)

**Ahora el flujo completo debería funcionar:**
```
1. Usuario → Click login
2. Backend → Genera PKCE y redirige a Autentic Sign
3. Usuario → Se autentica
4. Autentic Sign → Redirige con código
5. Frontend → Intercambia código por token ✅
6. Frontend → Guarda access_token correcto ✅
7. Frontend → Llama /api/AdresAuth/me con token ✅
8. Backend → Valida token JWT ✅
9. Backend → Devuelve info de usuario ✅
10. Dashboard → Carga con usuario autenticado ✅
```

---

## 💡 Nota sobre Naming Conventions

**OAuth 2.0 estándar usa snake_case:**
- `access_token`
- `refresh_token`
- `expires_in`
- `token_type`

**JavaScript/JSON generalmente usa camelCase:**
- `accessToken`
- `refreshToken`
- `expiresIn`
- `tokenType`

Decidimos mantener **snake_case** porque:
1. Es el estándar OAuth 2.0
2. El backend .NET devuelve snake_case por defecto
3. Muchas librerías OAuth esperan snake_case

---

## 📄 Archivos Modificados

1. ✅ `adres-web/src/pages/AuthCallback.js`
   - Cambiado de camelCase a snake_case
   - Agregada validación de token
   - Agregado log de debug

---

## 📞 Si Aún Falla

Si después del fix aún da error:

```bash
# En el navegador (DevTools Console):
console.log('Token guardado:', localStorage.getItem('access_token'));

# Debe mostrar un JWT con puntos, ej:
# "eyJhbGciOiJSUzI1NiIsImtpZCI6Ij..."
```

Si muestra `null` o `undefined`:
- Verificar que el backend devuelve `access_token` en la respuesta
- Verificar Network tab en DevTools para ver la respuesta de `/api/AdresAuth/token`

---

## 🎉 Estado Final Esperado

✅ OAuth flow completo funciona  
✅ Código se intercambia por token  
✅ Token se guarda correctamente en localStorage  
✅ Token se envía correctamente en Authorization header  
✅ Backend valida el token  
✅ Usuario autenticado carga en dashboard  

**¡Todo el flujo OAuth debería estar funcionando!** 🎉
