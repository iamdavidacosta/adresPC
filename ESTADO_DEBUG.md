# 🔍 Estado Actual - Debug OAuth

## ✅ Progreso

1. ✅ **Logs funcionando** - El backend está logueando correctamente
2. ✅ **Redirect URI correcta** - `https://adres-autenticacion.centralspike.com/auth/callback`
3. ✅ **Flujo OAuth inicia** - El usuario es redirigido a Autentic Sign
4. ✅ **Código recibido** - El callback recibe el código de autorización
5. ❌ **Error en intercambio** - Status: `BadRequest` (400)

---

## 🔴 Error Actual

```
❌ Error intercambiando código: BadRequest
   - redirect_uri: https://adres-autenticacion.centralspike.com/auth/callback
```

**Status Code:** `400 Bad Request` (antes era 401 Unauthorized)

---

## 🎯 Qué Significa BadRequest (400)

**400 Bad Request** indica que Autentic Sign rechaza la petición porque:

1. **Código inválido/expirado** (más común)
   - Los códigos de autorización expiran en 1-5 minutos
   - Solo se pueden usar UNA vez

2. **Code Verifier incorrecto** (PKCE)
   - El hash del code_verifier no coincide con el code_challenge

3. **Parámetro faltante o mal formado**
   - Algún campo requerido vacío
   - Formato incorrecto

4. **Client ID no válido**
   - El cliente no existe o está deshabilitado

---

## 📋 Información que Necesitamos

**Falta ver en los logs:**

```
📄 Response body: {...}
```

Este response body tiene el error detallado de Autentic Sign.

---

## 🚀 Siguiente Paso Inmediato

**En el servidor, ejecutar:**

```bash
# Ver el response body completo del error
docker compose logs --tail=100 api | grep -A 15 "Error intercambiando"
```

Buscar específicamente la línea que dice:
```
📄 Response body: {"error":"...", "error_description":"..."}
```

---

## 🧪 Probar de Nuevo (Paso a Paso)

1. **En el servidor:**
   ```bash
   # Logs en tiempo real
   docker compose logs -f api
   ```

2. **En el navegador:**
   - Borrar cookies y localStorage
   - Ir a: `https://adres-autenticacion.centralspike.com`
   - Iniciar sesión

3. **Observar los logs** - Deberías ver:
   ```
   🔄 Intercambiando código...
     📍 Redirect URI: ...
     🔑 Client ID: ...
     📝 Code: ...
     ✅ Code Verifier: ...
   ❌ Error intercambiando código: BadRequest
   📄 Response body: {...}  ← ESTO ES LO IMPORTANTE
   ```

---

## 📞 Preguntas para Autentic Sign

Mientras tanto, contactar a Autentic Sign:

```
Hola,

Estamos recibiendo error 400 Bad Request al intercambiar el código de autorización.

Detalles:
- Client ID: 410c8553-f9e4-44b8-90e1-234dd7a8bcd4
- Redirect URI: https://adres-autenticacion.centralspike.com/auth/callback
- Grant Type: authorization_code
- PKCE: code_challenge_method=S256

Preguntas:
1. ¿El Client ID está activo y configurado correctamente?
2. ¿La Redirect URI está registrada exactamente así?
3. ¿PKCE es obligatorio y está habilitado?
4. ¿Hay logs de su lado que muestren por qué se rechaza?
5. ¿Cuál es el timeout del código de autorización?

Estamos recibiendo el código correctamente pero falla al intercambiarlo.

Gracias!
```

---

## 💡 Hipótesis Más Probable

Dada la información actual, la causa más probable es:

**El código está expirando antes de ser intercambiado**

Esto puede pasar si:
- El código expira muy rápido (< 30 segundos)
- Hay latencia entre el callback y el intercambio
- El frontend está tardando en hacer la petición POST

**Solución:** Reducir el tiempo entre recibir el código y enviarlo al backend.

---

## 🔧 Debug Adicional en el Frontend

Si tienes acceso al código del frontend, agregar:

```javascript
// En el callback
console.log('⏱️ Código recibido:', new Date().toISOString());

// Antes de POST a /api/AdresAuth/token
console.log('📤 Enviando a backend:', new Date().toISOString());

// Calcular diferencia
```

---

## 📄 Archivos de Referencia

- `DEBUG_COMMANDS.md` - Comandos para ver logs
- `FIX_INVALID_GRANT.md` - Guía de troubleshooting
- `RESUMEN_INVALID_GRANT.md` - Resumen del error anterior

---

## ⏭️ Próximos Pasos

1. ✅ Ejecutar comando para ver response body completo
2. 🔲 Compartir el response body para analizar
3. 🔲 Contactar a Autentic Sign con las preguntas
4. 🔲 Verificar timing entre callback y intercambio
