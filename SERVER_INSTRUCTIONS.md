# 🚀 Instrucciones para el Servidor Linux

## 📋 Pasos a seguir en el servidor

### 1️⃣ **Actualizar el código**

```bash
cd ~/adresPC
git pull origin stg
```

### 2️⃣ **Limpiar contenedores anteriores**

```bash
# Detener y eliminar todo (incluyendo volúmenes)
docker compose down -v

# Verificar que no quede nada
docker ps -a
```

### 3️⃣ **Ejecutar diagnóstico (opcional pero recomendado)**

```bash
# Dar permisos de ejecución
chmod +x diagnose-sqlserver.sh

# Primero inicia solo SQL Server
docker compose up -d sqlserver

# Espera 90 segundos
sleep 90

# Ejecuta el diagnóstico
./diagnose-sqlserver.sh
```

### 4️⃣ **Levantar todos los servicios**

```bash
# Reconstruir imágenes
docker compose build

# Iniciar todos los servicios
docker compose up -d

# Ver logs en tiempo real
docker compose logs -f
```

### 5️⃣ **Verificar estado**

```bash
# Ver estado de contenedores
docker compose ps

# Deberías ver algo como:
# NAME              STATUS
# adres-sqlserver   Up (healthy)
# adres-api         Up (healthy)
# adres-web         Up (healthy)
```

---

## 🔍 Si SQL Server sigue "unhealthy"

### **Opción A: Revisar logs**

```bash
docker logs adres-sqlserver --tail 50
```

Busca mensajes de error como:
- `insufficient memory`
- `permission denied`
- `The sa password must be...`

### **Opción B: Conectarse manualmente**

```bash
# Entrar al contenedor
docker exec -it adres-sqlserver bash

# Intentar conectar
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q 'SELECT @@VERSION'
```

### **Opción C: Aumentar recursos**

SQL Server necesita:
- **Mínimo 2GB RAM**
- **Espacio en disco suficiente**

```bash
# Verificar memoria
free -h

# Verificar espacio
df -h
```

### **Opción D: Cambiar el healthcheck temporalmente**

Si todo lo demás falla, edita `docker-compose.yml` y comenta el healthcheck:

```yaml
sqlserver:
  image: mcr.microsoft.com/mssql/server:2022-latest
  # healthcheck:
  #   test: ["CMD-SHELL", "..."]
```

Luego:
```bash
docker compose up -d
```

---

## 📊 Comandos útiles

```bash
# Ver estado detallado
docker compose ps

# Ver logs de un servicio específico
docker compose logs sqlserver
docker compose logs api
docker compose logs web

# Reiniciar un servicio
docker compose restart sqlserver

# Ver uso de recursos
docker stats --no-stream

# Inspeccionar health
docker inspect adres-sqlserver --format='{{.State.Health}}'
```

---

## 🎯 URLs después de que funcione

- **Frontend**: http://[IP_DEL_SERVIDOR]:3000
- **Backend API**: http://[IP_DEL_SERVIDOR]:8080
- **SQL Server**: [IP_DEL_SERVIDOR]:1433

---

## ⚠️ Notas importantes

1. El contenedor SQL Server tarda **90 segundos** en iniciar (start_period)
2. Se hacen **10 intentos** de healthcheck cada 15 segundos
3. El password por defecto es: `YourStrong@Passw0rd` (cámbialo en `.env`)
4. En Linux, `mssql-tools` (sin 18) suele funcionar mejor que `mssql-tools18`

---

## 🆘 Si nada funciona

Contacta con los logs completos:

```bash
# Guardar logs en un archivo
docker logs adres-sqlserver > sqlserver-logs.txt
docker compose logs > all-services-logs.txt

# Enviar esos archivos para análisis
```
