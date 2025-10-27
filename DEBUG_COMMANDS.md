# 🔍 Comandos de Debugging - Error OAuth

## 📋 Ver Logs Completos del Error

### Opción 1: Ver TODO el contexto del error (Recomendado)
```bash
docker compose logs --tail=100 api | grep -A 10 -B 5 "Intercambiando"
```

Esto muestra:
- 5 líneas ANTES de "Intercambiando"
- La línea que contiene "Intercambiando"  
- 10 líneas DESPUÉS (donde están los errores)

### Opción 2: Ver solo los logs de OAuth
```bash
docker compose logs --tail=200 api | grep -E "🔄|📍|🔑|📝|✅|❌|📄|🔍" 
```

### Opción 3: Ver TODO el log reciente
```bash
docker compose logs --tail=50 api
```

### Opción 4: Ver logs en tiempo real
```bash
docker compose logs -f api
```
(Luego hacer el login en el navegador y observar)

---

## 🎯 Lo que Necesitamos Ver

Busca en los logs las siguientes líneas:

```
🔄 Intercambiando código de autorización por token en https://idp.autenticsign.com/connect/token con PKCE
  📍 Redirect URI: https://adres-autenticacion.centralspike.com/auth/callback
  🔑 Client ID: 410c8553-f9e4-44b8-90e1-234dd7a8bcd4
  📝 Code (primeros 20 chars): XXXXXXXXXXXXXXXXX...
  ✅ Code Verifier (primeros 10 chars): xxxxxxxxxx...
❌ Error intercambiando código: BadRequest
📄 Response body: {...}
🔍 Request data enviado:
   - grant_type: authorization_code
   - code: XXXXX...
   - redirect_uri: https://adres-autenticacion.centralspike.com/auth/callback
   - client_id: 410c8553-f9e4-44b8-90e1-234dd7a8bcd4
   - code_verifier: xxxxxx...
   - client_secret: NO
```

---

## 🔍 Análisis del Error

Según el log parcial:
```
❌ Error intercambiando código: BadRequest
   - redirect_uri: https://adres-autenticacion.centralspike.com/auth/callback
```

**BadRequest (400)** en lugar de Unauthorized (401) significa:

### Posibles Causas:

1. **Código inválido o expirado**
   - Los códigos expiran en ~1-5 minutos
   - No se pueden reusar

2. **Code verifier incorrecto (PKCE)**
   - El code_verifier no coincide con el code_challenge usado

3. **Parámetro faltante o mal formado**
   - Algún campo requerido está vacío
   - Formato incorrecto

4. **Client ID no encontrado**
   - El cliente no existe en Autentic Sign

---

## ✅ Siguiente Comando

**Ejecutar esto para ver el response body completo:**

```bash
docker compose logs --tail=100 api | grep -A 15 "Error intercambiando"
```

Esto mostrará el `📄 Response body:` que tiene la descripción del error real.

---

## 🧪 Probar de Nuevo

1. **Borrar caché y cookies** del navegador
2. Ir a `https://adres-autenticacion.centralspike.com`
3. Iniciar sesión
4. **INMEDIATAMENTE después** del error, ejecutar:
   ```bash
   docker compose logs --tail=50 api
   ```

---

## 📞 Si el Response Body dice...

### `"invalid_grant"` o `"code_expired"`
→ El código expiró o ya fue usado. Probar de nuevo.

### `"invalid_request"` + descripción
→ Falta un parámetro o está mal formado. Ver la descripción.

### `"invalid_client"`
→ El Client ID no existe o no está autorizado.

### `"unsupported_grant_type"`
→ Autentic Sign no soporta authorization_code (poco probable).

---

## 🎯 Información Crítica a Obtener

Del response body necesitamos ver:
```json
{
  "error": "...",
  "error_description": "..."
}
```

Ejecuta:
```bash
docker compose logs --tail=100 api | grep -E "Response body|error"
```
