# 🚨 RESUMEN EJECUTIVO - Mixed Content Error

## El Problema

**Error:** `blocked:mixed-content`

Tu frontend está en **HTTPS** pero intenta conectarse a la API usando **HTTP**. Los navegadores bloquean esto automáticamente por seguridad.

---

## ✅ Solución Rápida para el Servidor

```bash
cd ~/adresPC

# 1. Copiar configuraciones actualizadas (ahora con https)
cp .env.server .env
cp adres.api/.env.server adres.api/.env
cp adres-web/.env.server adres-web/.env

# 2. Verificar que tengan https://
cat adres-web/.env | grep REACT_APP_API_BASE_URL
# Debe mostrar: REACT_APP_API_BASE_URL=https://...

# 3. Rebuild y restart
docker compose down
docker compose build
docker compose up -d

# 4. Ver logs
docker compose logs -f web
```

---

## 📋 Checklist

- ✅ `.env.server` actualizado con `https://`
- ✅ `adres-web/.env.server` actualizado con `https://`
- ✅ `adres.api/.env.server` actualizado con `https://` en CORS
- 🔲 Copiar archivos `.env.server` → `.env` en el servidor
- 🔲 Rebuild de Docker compose
- 🔲 Verificar en el navegador que las peticiones usen HTTPS

---

## ⚠️ Requisito: Certificado SSL

Para que funcione, el servidor DEBE tener certificados SSL configurados en:
- `https://adres-autenticacion.centralspike.com`
- `https://adres-autenticacion-back.centralspike.com`

### ¿No tienes SSL?

**Opción A:** Instalar certificado gratis con Let's Encrypt
```bash
sudo apt install certbot
# Luego configurar nginx/apache con certbot
```

**Opción B:** Temporalmente volver a HTTP (solo desarrollo)
```bash
# En el servidor, editar manualmente los .env.server
# Cambiar todas las URLs de https:// a http://
```

---

## 🎯 Archivos Modificados

1. `.env.server` → `https://` en dominios
2. `adres-web/.env.server` → `https://` en API_BASE_URL
3. `adres.api/.env.server` → `https://` en ALLOWED_CORS
4. `DOMAIN_CONFIGURATION.md` → Documentación actualizada
5. `FIX_MIXED_CONTENT.md` → Guía detallada del fix

---

## 📞 Soporte

Ver archivo completo: `FIX_MIXED_CONTENT.md`
