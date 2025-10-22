# 🔧 Solución: SQL Server Unhealthy

## Problema
```bash
Container adres-sqlserver is unhealthy
dependency failed to start: container adres-sqlserver is unhealthy
```

---

## 🚨 Causas Comunes

1. **Password muy débil** - SQL Server 2022 requiere contraseñas fuertes
2. **Falta de memoria** - SQL Server necesita ~2GB RAM mínimo
3. **Volumen con permisos incorrectos**
4. **Health check fallando antes de que SQL inicie**
5. **Puerto 1433 ya en uso**

---

## ✅ Solución Paso a Paso

### **Opción 1: Script Automático (Recomendado)**

```bash
# Dar permisos de ejecución
chmod +x diagnose-sql.sh

# Ejecutar diagnóstico
./diagnose-sql.sh
```

Este script:
- Limpia contenedores previos
- Verifica el archivo .env
- Inicia solo SQL Server
- Monitorea el health check
- Muestra logs útiles

---

### **Opción 2: Manual**

#### **1. Limpiar todo**
```bash
docker compose down -v
docker volume prune -f
```

#### **2. Verificar archivo .env**
Asegúrate de que el archivo `.env` en la raíz del proyecto tenga una contraseña FUERTE:

```env
SA_PASSWORD=YourStrong@Passw0rd123!
SQL_SERVER_PORT=1433
MSSQL_PID=Developer
```

**Requisitos de la contraseña:**
- Mínimo 8 caracteres
- Mayúsculas y minúsculas
- Números
- Símbolos especiales (@, !, #, etc.)

#### **3. Verificar memoria disponible**
```bash
free -h
```

SQL Server necesita al menos 2GB de RAM libre.

#### **4. Iniciar solo SQL Server**
```bash
docker compose up -d sqlserver
```

#### **5. Monitorear logs en tiempo real**
```bash
docker logs -f adres-sqlserver
```

Busca:
- ✅ `SQL Server is now ready for client connections`
- ❌ `ERROR` o `FAILED`

#### **6. Verificar health check**
```bash
# Esperar 90 segundos
sleep 90

# Ver estado
docker ps
```

Si dice "healthy", continuar con:
```bash
docker compose up -d
```

---

## 🔍 Diagnóstico Avanzado

### **Ver logs completos**
```bash
docker logs adres-sqlserver
```

### **Entrar al contenedor**
```bash
docker exec -it adres-sqlserver /bin/bash
```

### **Probar conexión manualmente**
```bash
docker exec adres-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd123!' -C -Q "SELECT 1"
```

### **Ver detalles del health check**
```bash
docker inspect adres-sqlserver --format='{{json .State.Health}}' | jq
```

---

## 🛠️ Soluciones Específicas

### **Si el puerto está ocupado**
```bash
# Ver qué proceso usa el puerto 1433
sudo lsof -i :1433
# o
sudo netstat -tulpn | grep 1433

# Cambiar el puerto en .env
SQL_SERVER_PORT=1434
```

### **Si falta memoria**
```bash
# Ver uso de memoria
free -h
docker stats --no-stream

# Reducir memoria de SQL Server (añadir a docker-compose.yml)
deploy:
  resources:
    limits:
      memory: 2G
```

### **Si hay problemas de permisos**
```bash
# Cambiar propietario del volumen
sudo chown -R $(whoami) /var/lib/docker/volumes/adrespc_sqlserver_data

# O usar volumen local
mkdir -p ./data/sqlserver
# Cambiar en docker-compose.yml:
volumes:
  - ./data/sqlserver:/var/opt/mssql
```

---

## 🐛 Errores Comunes y Soluciones

### Error: "Password validation failed"
```bash
# La contraseña es muy débil
# Usar: YourStrong@Passw0rd123!
```

### Error: "Cannot allocate memory"
```bash
# Servidor sin memoria suficiente
# Verificar: free -h
# Cerrar otros servicios o aumentar RAM del servidor
```

### Error: "Address already in use"
```bash
# Puerto 1433 ocupado
# Cambiar SQL_SERVER_PORT en .env a otro puerto (ej: 1434)
```

---

## ✅ Verificación Final

Cuando SQL Server esté healthy:

```bash
# 1. Verificar estado
docker ps

# Debe mostrar:
# STATUS: Up X minutes (healthy)

# 2. Levantar todo
docker compose up -d

# 3. Verificar logs de la API
docker logs adres-api --tail=20

# Debe mostrar:
# ✅ Base de datos lista
# ✅ Application started
```

---

## 📞 Si Nada Funciona

### **Opción: Usar SQLite en lugar de SQL Server**

Si el servidor tiene recursos limitados, considera usar SQLite:

1. Editar `adres.api/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=app.db"
}
```

2. Cambiar en `docker-compose.yml` para no usar SQL Server

3. Instalar paquete SQLite en el proyecto:
```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

---

## 📝 Resumen de Comandos Útiles

```bash
# Limpiar todo
docker compose down -v

# Ver logs
docker logs adres-sqlserver -f

# Health check manual
docker exec adres-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd123!' -C -Q "SELECT 1"

# Reintentar
docker compose up -d sqlserver

# Ver estado después de 90 segundos
sleep 90 && docker ps
```

---

**Creado**: 20 de octubre de 2025  
**Actualizado**: Compatible con SQL Server 2022 en Linux
