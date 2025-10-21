# 🚀 Guía de Despliegue - ADRES Sistema de Gestión

## 📋 Tabla de Contenido

1. [Requisitos Previos](#requisitos-previos)
2. [Despliegue en Staging](#despliegue-en-staging)
3. [Despliegue en Producción](#despliegue-en-producción)
4. [Configuración del Autenticador](#configuración-del-autenticador)
5. [Migraciones de Base de Datos](#migraciones-de-base-de-datos)
6. [Verificación Post-Despliegue](#verificación-post-despliegue)
7. [Rollback](#rollback)

---

## 📦 Requisitos Previos

### **Servidor**
- Windows Server 2019+ o Linux (Ubuntu 20.04+)
- Docker y Docker Compose instalados
- Certificados SSL configurados
- Puertos abiertos: 80 (HTTP), 443 (HTTPS), 1433 (SQL Server interno)

### **Dominios Configurados**
- **Staging**:
  - `app.staging.adres.gov.co` → Frontend
  - `api.staging.adres.gov.co` → Backend API
  
- **Producción**:
  - `app.adres.gov.co` → Frontend
  - `api.adres.gov.co` → Backend API

### **Accesos Necesarios**
- Credenciales del repositorio Git
- Credenciales del servidor de base de datos
- Configuración del autenticador externo (ver `AUTHENTICATION_URLS.md`)

---

## 🟡 Despliegue en Staging

### **Paso 1: Clonar el Repositorio**

```bash
# SSH al servidor de staging
ssh usuario@servidor-staging.adres.gov.co

# Clonar repositorio
cd /opt
git clone https://github.com/iamdavidacosta/adresPC.git adres-staging
cd adres-staging
git checkout main
```

### **Paso 2: Configurar Variables de Entorno**

```bash
# Copiar archivo de configuración de staging
cp .env.staging .env

# Editar variables sensibles
nano .env
```

**Actualizar estas variables**:
```bash
# Cambiar la contraseña de SQL Server
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=AdresAuthDb;User ID=sa;Password=CONTRASEÑA_SEGURA_AQUÍ;TrustServerCertificate=True;Encrypt=False

# Configurar URLs del autenticador (provistas por el equipo de autenticación)
AUTH_AUTHORITY=https://auth.staging.adres.gov.co
AUTH_JWKS_URL=https://auth.staging.adres.gov.co/.well-known/jwks.json

# Configurar URLs del frontend
FRONTEND_URL_STAGING=https://app.staging.adres.gov.co
AUTH_CALLBACK_URL=https://app.staging.adres.gov.co/auth/callback
```

### **Paso 3: Construir y Levantar Contenedores**

```bash
# Construir imágenes
docker-compose build

# Levantar servicios
docker-compose up -d

# Verificar que estén corriendo
docker-compose ps
```

### **Paso 4: Ejecutar Migraciones de Base de Datos**

```bash
# Las migraciones se ejecutan automáticamente al iniciar la API
# Verificar logs
docker-compose logs -f api

# Deberías ver:
# "Aplicando migraciones pendientes..."
# "Ejecutando seed de datos..."
# "Base de datos lista ✅"
```

### **Paso 5: Desplegar Frontend**

```bash
cd adres-web

# Instalar dependencias
npm install

# Usar variables de entorno de staging
cp .env.staging .env.production.local

# Construir para producción
npm run build

# El contenido estará en: adres-web/build/
# Copiar a servidor web (Nginx/IIS)
```

### **Paso 6: Configurar Nginx (o IIS)**

**Archivo: `/etc/nginx/sites-available/adres-staging`**

```nginx
# Frontend
server {
    listen 443 ssl http2;
    server_name app.staging.adres.gov.co;

    ssl_certificate /etc/ssl/certs/adres-staging.crt;
    ssl_certificate_key /etc/ssl/private/adres-staging.key;

    root /opt/adres-staging/adres-web/build;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache estáticos
    location /static/ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}

# Backend API
server {
    listen 443 ssl http2;
    server_name api.staging.adres.gov.co;

    ssl_certificate /etc/ssl/certs/adres-staging.crt;
    ssl_certificate_key /etc/ssl/private/adres-staging.key;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}

# Redirect HTTP a HTTPS
server {
    listen 80;
    server_name app.staging.adres.gov.co api.staging.adres.gov.co;
    return 301 https://$host$request_uri;
}
```

```bash
# Habilitar sitio
ln -s /etc/nginx/sites-available/adres-staging /etc/nginx/sites-enabled/

# Verificar configuración
nginx -t

# Recargar Nginx
systemctl reload nginx
```

---

## 🔴 Despliegue en Producción

### **Paso 1: Preparación**

```bash
# SSH al servidor de producción
ssh usuario@servidor-produccion.adres.gov.co

# Clonar repositorio
cd /opt
git clone https://github.com/iamdavidacosta/adresPC.git adres-production
cd adres-production
git checkout main  # O tag específico: git checkout v1.0.0
```

### **Paso 2: Configurar Variables de Entorno**

```bash
# Copiar archivo de configuración de producción
cp .env.production .env

# Editar variables sensibles
nano .env
```

**IMPORTANTE: Cambiar TODAS las contraseñas**:
```bash
ConnectionStrings__DefaultConnection=Server=sqlserver-prod,1433;Database=AdresAuthDb;User ID=sa;Password=CONTRASEÑA_MUY_SEGURA;TrustServerCertificate=True;Encrypt=True

AUTH_AUTHORITY=https://auth.adres.gov.co
AUTH_JWKS_URL=https://auth.adres.gov.co/.well-known/jwks.json

FRONTEND_URL_PRODUCTION=https://app.adres.gov.co
AUTH_CALLBACK_URL=https://app.adres.gov.co/auth/callback

# Deshabilitar Swagger en producción
ENABLE_SWAGGER=false
```

### **Paso 3: Construir y Levantar**

```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

### **Paso 4: Migraciones (Producción)**

```bash
# Verificar estado de migraciones
docker exec -it adres-api dotnet ef database update --context AdresAuthDbContext

# Verificar logs
docker-compose logs -f api
```

### **Paso 5: Deploy Frontend**

Igual que staging, pero usando `.env.production`

---

## 🔐 Configuración del Autenticador

Ver documento completo: [`AUTHENTICATION_URLS.md`](./AUTHENTICATION_URLS.md)

### **URLs a Proporcionar al Proveedor**

**Staging:**
```
Callback: https://app.staging.adres.gov.co/auth/callback
Logout:   https://app.staging.adres.gov.co
Error:    https://app.staging.adres.gov.co/auth/error
```

**Producción:**
```
Callback: https://app.adres.gov.co/auth/callback
Logout:   https://app.adres.gov.co
Error:    https://app.adres.gov.co/auth/error
```

---

## 🗄️ Migraciones de Base de Datos

### **Crear Nueva Migración (Desarrollo)**

```bash
cd adres.api
dotnet ef migrations add NombreDeLaMigracion --context AdresAuthDbContext
```

### **Aplicar Migraciones Manualmente**

```bash
# En contenedor
docker exec -it adres-api dotnet ef database update

# Desde host
dotnet ef database update --project adres.api
```

### **Rollback de Migración**

```bash
# Ver migraciones aplicadas
dotnet ef migrations list

# Revertir a migración específica
dotnet ef database update MigracionAnterior
```

### **Script SQL para DBA**

```bash
# Generar script SQL sin ejecutar
dotnet ef migrations script --idempotent --output migration.sql
```

---

## ✅ Verificación Post-Despliegue

### **1. Health Checks**

```bash
# Backend API
curl https://api.staging.adres.gov.co/
# Esperado: "ADRES.API lista 🚀"

# Swagger (solo staging)
curl https://api.staging.adres.gov.co/swagger/index.html

# Base de datos
curl -X GET "https://api.staging.adres.gov.co/api/Users" -H "accept: application/json"
```

### **2. Autenticación**

```bash
# Obtener configuración de auth
curl https://api.staging.adres.gov.co/api/Auth/config

# Debería devolver:
# {
#   "loginUrl": "https://api.staging.adres.gov.co/api/Auth/login",
#   "logoutUrl": "https://api.staging.adres.gov.co/api/Auth/logout",
#   ...
# }
```

### **3. Frontend**

- Abrir https://app.staging.adres.gov.co
- Click en "Iniciar Sesión"
- Debería redirigir al autenticador externo
- Después de login, debería volver a `/auth/callback`
- Dashboard debería cargar correctamente

### **4. Logs**

```bash
# Ver logs de la API
docker-compose logs -f api

# Ver logs de SQL Server
docker-compose logs -f sqlserver

# Ver errores recientes
docker-compose logs --tail=100 api | grep ERROR
```

---

## 🔄 Rollback

### **Opción 1: Rollback de Código**

```bash
# Volver a commit anterior
git log --oneline -10
git checkout <commit-hash>
docker-compose up -d --build
```

### **Opción 2: Rollback de Base de Datos**

```bash
# Revertir migración
docker exec -it adres-api dotnet ef database update MigracionAnterior
```

### **Opción 3: Restaurar Backup**

```bash
# Restaurar backup de SQL Server
docker exec -it adres-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Password' -Q "RESTORE DATABASE AdresAuthDb FROM DISK='/var/opt/mssql/backup/AdresAuthDb.bak' WITH REPLACE"
```

---

## 📞 Soporte

- **Errores de Despliegue**: dev@adres.gov.co
- **Problemas de Infraestructura**: infra@adres.gov.co
- **Autenticación**: auth-support@adres.gov.co

---

## 📝 Checklist de Despliegue

### **Pre-Despliegue**
- [ ] Backup de base de datos actual
- [ ] Variables de entorno configuradas
- [ ] URLs del autenticador configuradas
- [ ] Certificados SSL válidos
- [ ] DNS apuntando correctamente

### **Durante Despliegue**
- [ ] Git pull del código actualizado
- [ ] Docker compose build exitoso
- [ ] Migraciones aplicadas sin errores
- [ ] Servicios levantados correctamente

### **Post-Despliegue**
- [ ] Health checks pasando
- [ ] Login funciona correctamente
- [ ] API responde correctamente
- [ ] Logs sin errores críticos
- [ ] Notificar al equipo

---

**Última Actualización**: Octubre 2025  
**Versión**: 1.0
