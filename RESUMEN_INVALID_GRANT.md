# 🚨 RESUMEN: Error invalid_grant en OAuth

## ❌ El Problema

```
POST /api/AdresAuth/token
Status: 401 Unauthorized
{"error":"invalid_grant","error_description":""}
```

El intercambio del código de autorización por el token está fallando.

---

## 🎯 Causa Más Probable

El **`redirect_uri`** enviado al intercambiar el código **NO coincide exactamente** con la usada al generar el código.

### ¿Qué verificar?

**En Autentic Sign (panel de admin):**
- Cliente: `410c8553-f9e4-44b8-90e1-234dd7a8bcd4`
- Redirect URI registrada: ¿Cuál es EXACTAMENTE?

Debe ser:
```
https://adres-autenticacion.centralspike.com/auth/callback
```

**SIN:**
- Trailing slash `/`
- Puerto `:443`
- HTTP (debe ser HTTPS)
- Parámetros query

---

## ✅ Solución Aplicada

He agregado **logs detallados** al backend para debug. Ahora cuando falle mostrará:

```
❌ Error intercambiando código: 401
📄 Response body: {"error":"invalid_grant"...}
🔍 Request data enviado:
   - redirect_uri: https://adres-autenticacion.centralspike.com/auth/callback
   - client_id: 410c8553-f9e4-44b8-90e1-234dd7a8bcd4
   - code: 99DB5C4B7A98B6BDB867...
   - code_verifier: wkrbWJfsQx...
```

---

## 🚀 Próximos Pasos

### 1️⃣ En el Servidor

```bash
cd ~/adresPC

# Pull de los cambios
git pull

# Rebuild backend
docker compose down
docker compose build api
docker compose up -d

# Ver logs en tiempo real
docker compose logs -f api
```

### 2️⃣ Probar de Nuevo

1. Ir a: `https://adres-autenticacion.centralspike.com`
2. Iniciar sesión con Autentic Sign
3. Cuando falle, ver los logs del backend
4. Los logs mostrarán EXACTAMENTE qué se está enviando

### 3️⃣ Verificar en Autentic Sign

Contactar al equipo de Autentic Sign para confirmar:

1. **Client ID:** `410c8553-f9e4-44b8-90e1-234dd7a8bcd4`
2. **Redirect URIs registradas:** ¿Cuáles exactamente?
3. **¿Requiere client_secret?** (para clientes públicos/SPA debería ser NO)
4. **PKCE habilitado:** Debe ser SÍ

---

## 📋 Preguntas para Autentic Sign

```
Hola,

Estamos teniendo un error "invalid_grant" al intercambiar el código de autorización.

Client ID: 410c8553-f9e4-44b8-90e1-234dd7a8bcd4

Preguntas:
1. ¿Qué Redirect URIs están registradas para este cliente?
2. ¿El cliente requiere client_secret o es público (SPA)?
3. ¿PKCE está habilitado y es obligatorio?
4. ¿Pueden revisar sus logs para este error y dar más detalles?

Estamos enviando:
- redirect_uri: https://adres-autenticacion.centralspike.com/auth/callback
- grant_type: authorization_code
- code_challenge_method: S256

Gracias!
```

---

## 🔍 Otras Causas Posibles

1. **Código expirado** (timeout 1-5 min) - Probar de nuevo
2. **Código ya usado** - Los códigos son de un solo uso
3. **Code verifier incorrecto** - El hash no coincide con el challenge
4. **Client secret requerido pero no enviado**

---

## 📄 Documentación

- `FIX_INVALID_GRANT.md` - Guía completa de debugging
- `AUTH_URLS.md` - URLs de autenticación configuradas

---

## 💡 Tip

Si Autentic Sign dice que la redirect URI registrada es ligeramente diferente (ej: con trailing slash), puedes:

**Opción A:** Actualizar en Autentic Sign (recomendado)
**Opción B:** Cambiar en el código para que coincida exactamente

