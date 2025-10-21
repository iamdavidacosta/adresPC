# ✅ Reorganización Completada

## 📋 Resumen de Cambios

Se ha reorganizado la estructura del proyecto para que cada proyecto (frontend y backend) tenga sus propios archivos de configuración de forma independiente.

---

## 📂 Estructura ANTES

```
adres.api/
├── .env.staging                    # ❌ Mezclado
├── .env.production                 # ❌ Mezclado
├── docker-compose.yml              # ✅ OK
├── docker-compose.dev.yml          # ❌ Archivo extra
├── docker-compose.prod.yml         # ❌ Archivo extra
│
├── adres-web/
│   ├── .env.staging                # ❌ Duplicado
│   └── .env.production             # ❌ Duplicado
│
└── adres.api/
    └── (sin archivos .env)         # ❌ Faltaba
```

---

## 📂 Estructura DESPUÉS (ACTUAL)

```
adres.api/                          # 📁 Raíz del repositorio
│
├── .env                            # ✅ Solo para Docker Compose
├── docker-compose.yml              # ✅ Archivo único de orquestación
│
├── adres-web/                      # ⚛️ Frontend
│   ├── .env                        # ✅ Variables del frontend
│   ├── Dockerfile                  # ✅ Build de producción
│   ├── Dockerfile.dev              # ✅ Build de desarrollo
│   └── nginx.conf                  # ✅ Configuración de Nginx
│
└── adres.api/                      # 🔧 Backend
    ├── .env                        # ✅ Variables del backend
    ├── .env.staging                # ✅ Plantilla para staging
    ├── .env.production             # ✅ Plantilla para producción
    └── Dockerfile                  # ✅ Build del backend
```

---

## 🎯 Beneficios

### **1. Separación Clara**
- ✅ Cada proyecto tiene sus propias variables de entorno
- ✅ Backend en `adres.api/`
- ✅ Frontend en `adres-web/`
- ✅ Orquestación en la raíz

### **2. Simplicidad**
- ✅ Un solo `docker-compose.yml` (sin archivos `.dev.yml` o `.prod.yml`)
- ✅ Variables de entorno organizadas por proyecto
- ✅ Fácil de entender para nuevos desarrolladores

### **3. Flexibilidad**
- ✅ Desarrollo local sin Docker: usar `.env` de cada proyecto
- ✅ Desarrollo con Docker: usar `.env` de la raíz
- ✅ Staging/Production: usar `.env.staging` o `.env.production`

---

## 🔧 Archivos por Proyecto

### **Raíz del Repositorio**
| Archivo | Propósito |
|---------|-----------|
| `.env` | Variables para `docker-compose.yml` |
| `docker-compose.yml` | Orquestación de los 3 servicios (sqlserver, api, web) |
| `ENV_STRUCTURE.md` | Documentación de la estructura de variables |

### **Backend (`adres.api/`)**
| Archivo | Propósito |
|---------|-----------|
| `.env` | Variables para desarrollo local |
| `.env.staging` | Plantilla para ambiente de staging |
| `.env.production` | Plantilla para ambiente de producción |
| `Dockerfile` | Build del contenedor de la API |
| `README.md` | Documentación del backend |

### **Frontend (`adres-web/`)**
| Archivo | Propósito |
|---------|-----------|
| `.env` | Variables para desarrollo local |
| `Dockerfile` | Build de producción (multi-stage con Nginx) |
| `Dockerfile.dev` | Build de desarrollo (con hot reload) |
| `nginx.conf` | Configuración del servidor web |
| `README.md` | Documentación del frontend |

---

## 🚀 Comandos Rápidos

### **Desarrollo Local con Docker**
```bash
# Asegúrate de tener el .env configurado en la raíz
docker-compose up -d

# Frontend: http://localhost:3000
# Backend: http://localhost:8080
# Swagger: http://localhost:8080/swagger
```

### **Desarrollo Local sin Docker**

**Backend:**
```bash
cd adres.api
# Editar adres.api/.env si necesitas
dotnet run
```

**Frontend:**
```bash
cd adres-web
# Editar adres-web/.env si necesitas
npm start
```

### **Despliegue**
```bash
# Staging
cp adres.api/.env.staging adres.api/.env
docker-compose up -d

# Production
cp adres.api/.env.production adres.api/.env
docker-compose up -d
```

---

## 📝 Archivos Eliminados

- ❌ `.env.staging` (raíz) → Movido a `adres.api/.env.staging`
- ❌ `.env.production` (raíz) → Movido a `adres.api/.env.production`
- ❌ `docker-compose.dev.yml` → Consolidado en `docker-compose.yml`
- ❌ `docker-compose.prod.yml` → Consolidado en `docker-compose.yml`
- ❌ `adres-web/.env.staging` → Eliminado (duplicado)
- ❌ `adres-web/.env.production` → Eliminado (duplicado)

---

## 📝 Archivos Creados

- ✅ `.env` (raíz) → Variables para Docker Compose
- ✅ `adres.api/.env` → Variables del backend
- ✅ `adres-web/.env` → Variables del frontend
- ✅ `adres-web/Dockerfile` → Build de producción
- ✅ `adres-web/Dockerfile.dev` → Build de desarrollo
- ✅ `adres-web/nginx.conf` → Configuración de Nginx
- ✅ `adres.api/README.md` → Documentación del backend
- ✅ `ENV_STRUCTURE.md` → Documentación de variables
- ✅ `REORGANIZATION.md` → Este archivo

---

## ✅ Commit

```bash
git commit -m "Reorganize project structure: one .env per project, consolidate Docker config"
```

**Cambios incluidos**:
- 12 archivos modificados
- 850 inserciones(+)
- 86 eliminaciones(-)

---

## 🔐 Seguridad

Recuerda que:
- ✅ Los archivos `.env` están en `.gitignore`
- ✅ Los archivos `.env.staging` y `.env.production` SÍ se versionan como plantillas
- ⚠️ Antes de desplegar, **revisa y actualiza** las variables sensibles (contraseñas, secrets, etc.)

---

**Fecha**: Octubre 2025  
**Autor**: GitHub Copilot  
**Estado**: ✅ Completado
