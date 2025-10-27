# ✅ FIX APLICADO: invalid_grant - PKCE Code Verifier Mismatch

## 🔴 El Problema Encontrado

En `AdresAuthController.cs`, el método `Authorize()` estaba llamando `GetAuthorizationUrl()` **DOS VECES**:

```csharp
// Primera llamada - Genera code_verifier #1 y code_challenge #1
var (authUrl, codeVerifier) = _adresAuthService.GetAuthorizationUrl(..., "");

// Guarda code_verifier #1 en el state
var stateData = new { cv = codeVerifier };

// Segunda llamada - Genera code_verifier #2 y code_challenge #2 (DIFERENTE!)
(authUrl, _) = _adresAuthService.GetAuthorizationUrl(..., state);
```

**Resultado:**
- `code_challenge` enviado a Autentic Sign = Hash de code_verifier **#2**
- `code_verifier` en el state = code_verifier **#1**
- **NO COINCIDEN** ❌

Cuando el frontend intentaba intercambiar el código:
- Enviaba code_verifier #1 (del state)
- Autentic Sign esperaba code_verifier #2 (el que generó el challenge)
- Error: `invalid_grant` 🔴

---

## ✅ La Solución Aplicada

**Modificado:** `AdresAuthController.cs`, método `Authorize()`

Ahora solo llama `GetAuthorizationUrl()` **UNA VEZ**:

```csharp
// Generar PKCE UNA SOLA VEZ
var (authUrlWithoutState, codeVerifier) = _adresAuthService.GetAuthorizationUrl(redirectUri, "");

// Guardar el MISMO code_verifier en el state
var stateData = new { cv = codeVerifier };
var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(stateJson));

// Agregar state a la URL SIN regenerar code_verifier
var authUrl = authUrlWithoutState + "&state=" + Uri.EscapeDataString(state);
```

**Ahora:**
- `code_challenge` enviado a Autentic Sign = Hash de code_verifier único
- `code_verifier` en el state = El mismo code_verifier único
- **COINCIDEN** ✅

---

## 🚀 Desplegar el Fix

### En el Servidor

```bash
cd ~/adresPC

# Pull del código actualizado
git pull

# Rebuild del backend
docker compose down
docker compose build api
docker compose up -d

# Ver logs
docker compose logs -f api
```

---

## 🧪 Probar

1. **Borrar cookies y localStorage** del navegador
2. Ir a: `https://adres-autenticacion.centralspike.com`
3. Click en "Iniciar Sesión"
4. Autenticarse en Autentic Sign
5. **Ahora debería funcionar** ✅

**En los logs deberías ver:**
```
🔄 Redirigiendo a Autentic Sign con PKCE
  📍 Code Verifier (primeros 10): 4aOPA8ctnx...
...
🔄 Intercambiando código de autorización...
  ✅ Code Verifier (primeros 10): 4aOPA8ctnx...
✅ Código intercambiado exitosamente por token
```

**Los primeros 10 caracteres del code_verifier deben ser los MISMOS** ✅

---

## 📋 Cambios Realizados

### Archivos Modificados

1. ✅ `adres.api/Controllers/AdresAuthController.cs`
   - Método `Authorize()` - Fix del doble llamado a GetAuthorizationUrl
   - Agregado log del code_verifier para debug

### Archivos de Documentación

1. 📄 `ANALISIS_INVALID_GRANT.md` - Análisis del problema
2. 📄 `FIX_PKCE_MISMATCH.md` - Este archivo
3. 📄 `DEBUG_COMMANDS.md` - Comandos de debugging
4. 📄 `ESTADO_DEBUG.md` - Estado del debugging

---

## 🎯 Resumen

**Antes:**
```
code_challenge enviado a Autentic Sign: ABC123 (de code_verifier #2)
code_verifier en callback: XYZ789 (code_verifier #1)
Resultado: invalid_grant ❌
```

**Después:**
```
code_challenge enviado a Autentic Sign: ABC123 (de code_verifier único)
code_verifier en callback: ABC123 (el mismo code_verifier único)
Resultado: ✅ Token obtenido correctamente
```

---

## ⚠️ Nota Importante

Este fix asume que el flujo es:
1. Usuario → `/api/AdresAuth/authorize`
2. Backend → Redirige a Autentic Sign con code_challenge
3. Autentic Sign → Redirige a `/auth/callback` con code
4. Frontend → Extrae code_verifier del state
5. Frontend → POST `/api/AdresAuth/token` con code y code_verifier
6. Backend → Intercambia con Autentic Sign
7. ✅ Token obtenido

El fix garantiza que el code_verifier usado en el paso 5 coincida con el que generó el code_challenge en el paso 2.

---

## 🎉 Próximo Paso

Una vez desplegado y probado, el flujo OAuth completo debería funcionar:
1. ✅ Login con Autentic Sign
2. ✅ Callback con código
3. ✅ Intercambio de código por token (ANTES FALLABA, AHORA FUNCIONA)
4. ✅ Obtener perfil de usuario
5. ✅ Redirigir al dashboard

**¡El error `invalid_grant` debería estar resuelto!** 🎉
