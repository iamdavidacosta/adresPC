# 🌐 Configuración de Dominios y CORS

## 📋 Configuración para el Servidor

Sigue estos pasos para configurar los dominios correctos en el servidor.

---

## 🔧 Paso 1: Copiar Archivos de Configuración

En el servidor, ejecuta:

```bash
cd ~/adresPC

# Copiar configuración de la raíz
cp .env.server .env

# Copiar configuración del backend
cp adres.api/.env.server adres.api/.env

# Copiar configuración del frontend
cp adres-web/.env.server adres-web/.env
```

---

## 📝 Paso 2: Verificar Configuración

### **Archivo: `.env` (raíz)**

```bash
cat .env
```

Deberías ver:
```env
FRONTEND_DOMAIN=http://adres-autenticacion.centralspike.com
BACKEND_DOMAIN=http://adres-autenticacion-back.centralspike.com
```

### **Archivo: `adres.api/.env` (backend)**

```bash
cat adres.api/.env
```

Deberías ver:
```env
ALLOWED_CORS=http://adres-autenticacion.centralspike.com,http://adres-autenticacion-back.centralspike.com
```

### **Archivo: `adres-web/.env` (frontend)**

```bash
cat adres-web/.env
```

Deberías ver:
```env
REACT_APP_API_BASE_URL=http://adres-autenticacion-back.centralspike.com/api
```

---

## 🚀 Paso 3: Reconstruir y Reiniciar

```bash
# Detener servicios
docker compose down

# Reconstruir imágenes (importante para que tome las nuevas variables)
docker compose build

# Iniciar servicios
docker compose up -d

# Verificar logs
docker compose logs -f
```

---

## 🎯 Dominios Configurados

| Servicio | URL |
|----------|-----|
| **Frontend** | http://adres-autenticacion.centralspike.com |
| **Backend API** | http://adres-autenticacion-back.centralspike.com |
| **Backend Swagger** | http://adres-autenticacion-back.centralspike.com/swagger |

---

## 🔍 Verificar CORS

Una vez iniciado, verifica que el backend tenga CORS configurado:

```bash
# Ver configuración del backend
docker exec adres-api env | grep ALLOWED_CORS
```

Deberías ver:
```
ALLOWED_CORS=http://adres-autenticacion.centralspike.com,http://adres-autenticacion-back.centralspike.com
```

---

## ⚙️ Variables de Entorno Disponibles

### **Raíz (`.env`)**
- `FRONTEND_DOMAIN` - URL del frontend
- `BACKEND_DOMAIN` - URL del backend
- `SA_PASSWORD` - Password de SQL Server
- `API_PORT` - Puerto del backend (default: 8080)
- `WEB_PORT` - Puerto del frontend (default: 3000)

### **Backend (`adres.api/.env`)**
- `ASPNETCORE_ENVIRONMENT` - Entorno (Development/Production)
- `ALLOWED_CORS` - Orígenes permitidos (separados por coma)
- `AUTH_AUTHORITY` - URL del servidor de autenticación
- `ENABLE_SWAGGER` - Habilitar/deshabilitar Swagger
- `ConnectionStrings__DefaultConnection` - Cadena de conexión a SQL Server

### **Frontend (`adres-web/.env`)**
- `REACT_APP_API_BASE_URL` - URL base del backend
- `REACT_APP_AUTH_LOGIN_URL` - URL de login
- `REACT_APP_AUTH_LOGOUT_URL` - URL de logout
- `NODE_ENV` - Entorno de Node (development/production)

---

## 🔄 Actualizar Dominios

Si necesitas cambiar los dominios en el futuro:

1. **Edita `.env` en la raíz**:
   ```bash
   nano .env
   ```
   Cambia `FRONTEND_DOMAIN` y `BACKEND_DOMAIN`

2. **Edita `adres.api/.env`**:
   ```bash
   nano adres.api/.env
   ```
   Actualiza `ALLOWED_CORS` con los nuevos dominios

3. **Edita `adres-web/.env`**:
   ```bash
   nano adres-web/.env
   ```
   Actualiza `REACT_APP_API_BASE_URL`

4. **Reconstruye**:
   ```bash
   docker compose down
   docker compose build
   docker compose up -d
   ```

---

## 🆘 Troubleshooting

### **Error de CORS**
Si ves errores de CORS en el navegador:

1. Verifica que `ALLOWED_CORS` incluya el dominio del frontend
2. Asegúrate de que no haya espacios en la lista de dominios
3. Reconstruye el contenedor del backend

```bash
docker compose restart api
docker compose logs api | grep CORS
```

### **Frontend no se conecta al backend**
1. Verifica `REACT_APP_API_BASE_URL` en `adres-web/.env`
2. Asegúrate de que el backend esté accesible en ese dominio
3. Reconstruye el contenedor del frontend

```bash
docker compose build web
docker compose up -d web
```

### **Variables no se aplican**
Las variables de entorno se leen durante el **build**, no solo al iniciar. Siempre ejecuta `docker compose build` después de cambiar `.env`:

```bash
docker compose down
docker compose build
docker compose up -d
```

---

## 📊 Verificar Configuración Actual

```bash
# Ver todas las variables de entorno del backend
docker exec adres-api env

# Ver todas las variables del frontend
docker exec adres-web env

# Ver configuración específica de CORS
docker exec adres-api env | grep ALLOWED_CORS

# Ver configuración de la API en el frontend
docker exec adres-web cat /usr/share/nginx/html/static/js/*.js | grep -o 'http://[^"]*api'
```

---

**Última Actualización**: 21 de octubre de 2025
