# 🔒 Fix: Mixed Content Error (HTTPS/HTTP)

## ❌ Error Encontrado

```
Users (blocked:mixed-content) fetch api.js:36 0.0 kB 0
```

**Causa:** El frontend está siendo servido por **HTTPS**, pero intenta hacer peticiones a la API usando **HTTP**. Los navegadores bloquean esto por seguridad (mixed content).

---

## ✅ Solución: Usar HTTPS en todas las URLs

Se actualizaron los archivos de configuración para usar **`https://`** en lugar de **`http://`**:

### Archivos Actualizados:

1. `.env.server` (raíz)
2. `adres-web/.env.server`
3. `adres.api/.env.server`

---

## 🚀 Pasos para Aplicar en el Servidor

### 1️⃣ Copiar configuraciones actualizadas

```bash
cd ~/adresPC

# Hacer backup de las configuraciones actuales (opcional)
cp .env .env.backup 2>/dev/null || true
cp adres.api/.env adres.api/.env.backup 2>/dev/null || true
cp adres-web/.env adres-web/.env.backup 2>/dev/null || true

# Copiar las nuevas configuraciones (ahora con HTTPS)
cp .env.server .env
cp adres.api/.env.server adres.api/.env
cp adres-web/.env.server adres-web/.env
```

### 2️⃣ Verificar que las URLs usen HTTPS

```bash
# Verificar .env raíz
echo "=== .env (raíz) ==="
cat .env | grep -E "DOMAIN|REACT_APP"

# Verificar backend
echo "=== Backend CORS ==="
cat adres.api/.env | grep ALLOWED_CORS

# Verificar frontend
echo "=== Frontend API URL ==="
cat adres-web/.env | grep REACT_APP_API_BASE_URL
```

**Debes ver URLs con `https://`:**
```
FRONTEND_DOMAIN=https://adres-autenticacion.centralspike.com
BACKEND_DOMAIN=https://adres-autenticacion-back.centralspike.com
REACT_APP_API_BASE_URL=https://adres-autenticacion-back.centralspike.com/api
ALLOWED_CORS=https://adres-autenticacion.centralspike.com,https://adres-autenticacion-back.centralspike.com
```

### 3️⃣ Reconstruir y reiniciar

```bash
# Detener servicios
docker compose down

# Reconstruir (importante para que el frontend tome las nuevas variables)
docker compose build

# Iniciar servicios
docker compose up -d

# Ver logs para verificar
docker compose logs -f
```

---

## 🔍 Verificación

### Desde el navegador:
1. Abre las DevTools (F12)
2. Ve a la pestaña **Network**
3. Recarga la página
4. Verifica que las peticiones a `/api/Users` usen **`https://`**

### Desde el servidor:
```bash
# Ver variables del contenedor web
docker exec adres-web env | grep REACT_APP_API_BASE_URL

# Ver variables del contenedor api
docker exec adres-api env | grep ALLOWED_CORS

# Probar endpoint
curl -k https://adres-autenticacion-back.centralspike.com/api/Users
```

---

## ⚠️ Importante: Certificados SSL

Para que HTTPS funcione correctamente, necesitas:

### Opción 1: Certificado SSL Real (Recomendado para Producción)

Si tu servidor tiene certificados SSL configurados:
```bash
# Las URLs con https:// deberían funcionar directamente
curl https://adres-autenticacion.centralspike.com
curl https://adres-autenticacion-back.centralspike.com/api/Users
```

### Opción 2: Si NO tienes certificados SSL

Si estás en desarrollo/staging SIN certificados SSL reales, tienes dos opciones:

#### A) Configurar un reverse proxy con SSL (Recomendado)
```bash
# Instalar certbot para Let's Encrypt (gratis)
sudo apt update
sudo apt install certbot

# Configurar nginx como reverse proxy con SSL
# (Requiere configuración adicional)
```

#### B) Volver a HTTP temporalmente (Solo desarrollo)
Si aún no tienes SSL configurado, cambia temporalmente a HTTP:
```bash
# Editar los .env.server manualmente
sed -i 's/https:/http:/g' .env.server
sed -i 's/https:/http:/g' adres-web/.env.server
sed -i 's/https:/http:/g' adres.api/.env.server

# Luego copiar y rebuild
cp .env.server .env
cp adres.api/.env.server adres.api/.env
cp adres-web/.env.server adres-web/.env
docker compose build && docker compose up -d
```

---

## 📊 Resumen de Cambios

| Archivo | Variable | Antes | Ahora |
|---------|----------|-------|-------|
| `.env.server` | FRONTEND_DOMAIN | `http://...` | `https://...` |
| `.env.server` | BACKEND_DOMAIN | `http://...` | `https://...` |
| `adres-web/.env.server` | REACT_APP_API_BASE_URL | `http://...` | `https://...` |
| `adres.api/.env.server` | ALLOWED_CORS | `http://...` | `https://...` |

---

## 🎯 Causa del Error

**Mixed Content Policy:**
- ✅ HTTPS → HTTPS: Permitido
- ❌ HTTPS → HTTP: **BLOQUEADO** (tu caso)
- ⚠️  HTTP → HTTP: Permitido pero inseguro
- ✅ HTTP → HTTPS: Permitido

Los navegadores modernos bloquean peticiones HTTP desde páginas HTTPS para proteger la seguridad del usuario.
