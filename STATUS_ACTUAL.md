# 🔍 Status: Token se Guarda Pero Aún 401

## ✅ Progreso

El frontend ahora está funcionando correctamente:
```
✅ Tokens recibidos: {access_token: 'eyJhbGciOiJSUzI1NiIs...', token_type: 'Bearer', expires_in: 3600}
```

**Confirmado:**
- ✅ PKCE funciona
- ✅ Intercambio de código por token funciona
- ✅ Token se guarda correctamente (snake_case fix funcionó)
- ✅ Token se envía en el header Authorization

## 🔴 Problema Actual

```
GET /api/AdresAuth/me → 401 Unauthorized
```

El backend **aún no valida el token** correctamente.

## 🎯 Causa

Los cambios del backend (JWT audience fix) **aún no se han desplegado** en el servidor.

En los logs del servidor viste:
```
❌ Error al autenticar token JWT
Microsoft.IdentityModel.Tokens.SecurityTokenMalformedException: JWT is not well formed
```

Esto era cuando el token era `undefined`. Ahora el token es correcto, pero **el backend necesita rebuild** para aplicar el fix del audience.

---

## 🚀 ACCIÓN REQUERIDA: Rebuild del Backend

### En el Servidor:

```bash
cd ~/adresPC

# 1. Pull de TODOS los cambios
git pull

# 2. Ver qué cambió
git log --oneline -5

# 3. Rebuild SOLO del backend (más rápido)
docker compose down
docker compose build api
docker compose up -d

# 4. Ver logs en tiempo real
docker compose logs -f api | grep -E "Token|autenticar|validado|CORS"
```

---

## 🔍 Qué Buscar en los Logs

### ✅ Si Funciona:

Deberías ver:
```
🔒 CORS configurado para: https://adres-autenticacion.centralspike.com, ...
✅ Token JWT validado correctamente
```

### ❌ Si Aún Falla:

Verás uno de estos errores:
```
❌ Error al autenticar token JWT
IDX10214: Audience validation failed. Audiences: 'https://idp.autenticsign.com/resources'. 
Did not match: validationParameters.ValidAudiences: 'adres-api'
```

O:
```
IDX10501: Signature validation failed. Unable to match key
```

---

## 📋 Checklist de Verificación

Antes de rebuild, verificar que estos archivos tengan los cambios:

### 1. Program.cs
```bash
# En el servidor
cat ~/adresPC/adres.api/Program.cs | grep -A 5 "ValidAudiences"
```

Debe mostrar:
```csharp
ValidAudiences = new[] { 
    jwtAudience,                      
    $"{jwtAuthority}/resources"       
},
```

### 2. AuthCallback.js
```bash
cat ~/adresPC/adres-web/src/pages/AuthCallback.js | grep "access_token"
```

Debe mostrar:
```javascript
localStorage.setItem('access_token', tokenData.access_token);
```

Si ambos están correctos → Hacer rebuild

---

## 🧪 Después del Rebuild

1. **Borrar localStorage** en el navegador:
   ```javascript
   localStorage.clear();
   ```

2. **Probar el flujo completo** de nuevo

3. **Verificar logs del backend** en tiempo real:
   ```bash
   docker compose logs -f api
   ```

4. Deberías ver:
   ```
   ✅ Token JWT validado correctamente
   ```

---

## 💡 Debug Adicional

Si después del rebuild aún da 401, necesitamos ver:

### En el navegador (DevTools > Network):

**Request a /api/AdresAuth/me:**
```
Headers:
  Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

Copiar el token completo y decodificarlo en https://jwt.io

**Verificar:**
- `iss`: debe ser `https://idp.autenticsign.com`
- `aud`: debe ser `https://idp.autenticsign.com/resources`
- `exp`: no debe estar expirado

### En el servidor:

```bash
# Ver el error específico del JWT
docker compose logs --tail=50 api | grep -A 10 "Error al autenticar"
```

---

## 📞 Comandos Rápidos para Troubleshooting

```bash
# Ver si el código está actualizado
cd ~/adresPC
git log --oneline -3

# Ver último commit
git show --stat

# Rebuild forzado (sin caché)
docker compose build --no-cache api

# Ver variables de entorno del contenedor
docker exec adres-api env | grep -E "AUTH_|ALLOWED_CORS"

# Ver logs solo de validación JWT
docker compose logs api | grep -E "Token|JWT|validado|autenticar"
```

---

## 🎯 Estado Esperado Después del Fix

```
Usuario → Inicia sesión
         ↓
Autentic Sign → Autentica
         ↓
Frontend → Intercambia código ✅
         ↓
Frontend → Guarda token ✅
         ↓
Frontend → Llama /api/AdresAuth/me con token
         ↓
Backend → Valida token con JWKS ⏳ (pending rebuild)
         ↓
Backend → Acepta audience "...resources" ⏳ (pending rebuild)
         ↓
Backend → Devuelve info de usuario ⏳
         ↓
Dashboard → Carga ⏳
```

**El último paso requiere que el backend esté actualizado.**

---

## 📄 Resumen

**Frontend:** ✅ Funcionando correctamente (token se guarda y envía bien)  
**Backend:** ⏳ Necesita rebuild para aplicar fix de JWT audience  

**Acción:** Ejecutar en el servidor:
```bash
cd ~/adresPC
git pull
docker compose build api
docker compose up -d
docker compose logs -f api
```

Luego probar de nuevo el login.
