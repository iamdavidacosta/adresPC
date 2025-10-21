# 📋 Estructura de Archivos de Configuración

Este documento explica la organización de archivos de configuración en el proyecto.

---

## 📂 Estructura Actual

```
adres.api/                          # Raíz del repositorio
│
├── .env                            # ✅ Variables para Docker Compose
│   └── Contiene: SA_PASSWORD, API_PORT, WEB_PORT, etc.
│
├── docker-compose.yml              # ✅ Orquestación única de contenedores
│   └── Usa variables de: .env (raíz)
│
├── adres.api/                      # Backend
│   ├── .env                        # ✅ Variables del backend (desarrollo)
│   ├── .env.staging                # ✅ Variables para staging
│   ├── .env.production             # ✅ Variables para producción
│   └── Dockerfile                  # Build del backend
│
└── adres-web/                      # Frontend
    ├── .env                        # ✅ Variables del frontend (desarrollo)
    ├── Dockerfile                  # Build de producción (multi-stage)
    ├── Dockerfile.dev              # Build de desarrollo
    └── nginx.conf                  # Configuración de Nginx
```

---

## 🔧 Archivos de Variables de Entorno

### **1️⃣ Raíz: `.env`**
**Propósito**: Variables para Docker Compose  
**Usado por**: `docker-compose.yml`

```env
# SQL Server
SA_PASSWORD=YourStrong@Passw0rd
SQL_SERVER_PORT=1433
MSSQL_PID=Developer

# Backend API
API_PORT=8080
DB_NAME=AdresDb
DB_USER=sa

# Frontend Web
WEB_PORT=3000
REACT_APP_API_BASE_URL=http://localhost:8080/api
```

**Cuándo usar**: Al ejecutar `docker-compose up`

---

### **2️⃣ Backend: `adres.api/.env`**
**Propósito**: Variables del backend en desarrollo  
**Usado por**: API cuando se ejecuta localmente con `dotnet run`

```env
# Entorno
ASPNETCORE_ENVIRONMENT=Development

# SQL Server
ConnectionStrings__DefaultConnection=Server=localhost,1433;...

# JWT Configuration
AUTH_AUTHORITY=https://auth.staging.adres.gov.co
AUTH_AUDIENCE=adres-api
AUTH_USE_JWKS=false

# CORS
ALLOWED_CORS=http://localhost:3000,http://localhost:5173

# Swagger
ENABLE_SWAGGER=true
```

**Cuándo usar**: Desarrollo local sin Docker

---

### **3️⃣ Backend: `adres.api/.env.staging`**
**Propósito**: Variables para ambiente de staging  
**Usado por**: Despliegue en servidor de staging

```env
ASPNETCORE_ENVIRONMENT=Staging
AUTH_AUTHORITY=https://auth.staging.adres.gov.co
ALLOWED_CORS=https://app.staging.adres.gov.co
ENABLE_SWAGGER=true
```

**Cuándo usar**: Despliegue en staging

---

### **4️⃣ Backend: `adres.api/.env.production`**
**Propósito**: Variables para ambiente de producción  
**Usado por**: Despliegue en servidor de producción

```env
ASPNETCORE_ENVIRONMENT=Production
AUTH_AUTHORITY=https://auth.adres.gov.co
ALLOWED_CORS=https://app.adres.gov.co
ENABLE_SWAGGER=false
```

**Cuándo usar**: Despliegue en producción

---

### **5️⃣ Frontend: `adres-web/.env`**
**Propósito**: Variables del frontend en desarrollo  
**Usado por**: React cuando se ejecuta con `npm start`

```env
# API Backend
REACT_APP_API_BASE_URL=http://localhost:8080/api

# Autenticación
REACT_APP_AUTH_LOGIN_URL=http://localhost:8080/api/Auth/login
REACT_APP_AUTH_LOGOUT_URL=http://localhost:8080/api/Auth/logout

# Entorno
NODE_ENV=development
```

**Cuándo usar**: Desarrollo local sin Docker

---

## 🚀 Casos de Uso

### **Desarrollo Local con Docker Compose**
```bash
# 1. Editar .env (raíz) si necesitas cambiar puertos
# 2. Ejecutar:
docker-compose up -d

# Usa:
# - .env (raíz) → para configurar Docker Compose
# - adres.api/.env → es cargado por el contenedor API
# - adres-web/.env → es usado en build del frontend
```

---

### **Desarrollo Local sin Docker**

**Backend:**
```bash
cd adres.api
# Editar adres.api/.env según necesites
dotnet run
```

**Frontend:**
```bash
cd adres-web
# Editar adres-web/.env según necesites
npm start
```

---

### **Despliegue a Staging**
```bash
# 1. Copiar adres.api/.env.staging al servidor
# 2. Renombrar a .env en el servidor
# 3. Ejecutar docker-compose con variables de staging
docker-compose up -d
```

---

### **Despliegue a Producción**
```bash
# 1. Copiar adres.api/.env.production al servidor
# 2. Renombrar a .env en el servidor
# 3. Ejecutar docker-compose con variables de producción
docker-compose up -d
```

---

## ⚠️ Importante

1. **`.env` NO se versiona** (está en `.gitignore`)
2. **`.env.staging` y `.env.production` SÍ se versionan** (como plantillas)
3. **Cada proyecto tiene su propio `.env`** para desarrollo local
4. **La raíz tiene un `.env`** solo para Docker Compose

---

## 🔐 Seguridad

- ❌ **NUNCA** commitear archivos `.env` con datos sensibles
- ✅ Los archivos `.env.staging` y `.env.production` son **plantillas**
- ✅ En producción, usar variables de entorno del sistema o secretos

---

**Última Actualización**: Octubre 2025
