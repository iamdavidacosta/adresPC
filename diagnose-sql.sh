#!/bin/bash

# ========================================
# Script de Diagnóstico - SQL Server
# ========================================

echo "========================================="
echo "  DIAGNÓSTICO SQL SERVER CONTAINER"
echo "========================================="
echo ""

# 1. Verificar que Docker está corriendo
echo "1️⃣  Verificando Docker..."
if ! docker ps > /dev/null 2>&1; then
    echo "❌ Docker no está corriendo o no tienes permisos"
    exit 1
fi
echo "✅ Docker está corriendo"
echo ""

# 2. Limpiar contenedores previos
echo "2️⃣  Limpiando contenedores previos..."
docker compose down -v
echo "✅ Limpieza completada"
echo ""

# 3. Verificar archivo .env
echo "3️⃣  Verificando archivo .env..."
if [ ! -f .env ]; then
    echo "⚠️  Archivo .env no encontrado. Creando uno por defecto..."
    cat > .env << 'EOF'
# SQL Server
SA_PASSWORD=YourStrong@Passw0rd123!
SQL_SERVER_PORT=1433
MSSQL_PID=Developer

# Backend API
API_PORT=8080
DB_NAME=AdresDb
DB_USER=sa

# Frontend Web
WEB_PORT=3000
REACT_APP_API_BASE_URL=http://localhost:8080/api
EOF
    echo "✅ Archivo .env creado"
else
    echo "✅ Archivo .env encontrado"
    echo "   Contenido:"
    cat .env | grep -v "^#" | grep -v "^$"
fi
echo ""

# 4. Iniciar solo SQL Server
echo "4️⃣  Iniciando SQL Server..."
docker compose up -d sqlserver
echo ""

# 5. Esperar y monitorear
echo "5️⃣  Monitoreando inicio de SQL Server (90 segundos)..."
for i in {1..18}; do
    sleep 5
    status=$(docker inspect adres-sqlserver --format='{{.State.Health.Status}}' 2>/dev/null)
    echo "   [$((i*5))s] Estado: $status"
    
    if [ "$status" = "healthy" ]; then
        echo "✅ SQL Server está HEALTHY!"
        break
    fi
done
echo ""

# 6. Ver logs
echo "6️⃣  Últimos logs de SQL Server:"
echo "-----------------------------------"
docker logs adres-sqlserver --tail=30
echo "-----------------------------------"
echo ""

# 7. Estado final
echo "7️⃣  Estado final del contenedor:"
docker ps -a --filter "name=adres-sqlserver"
echo ""

# 8. Verificar health check manualmente
echo "8️⃣  Probando health check manualmente..."
docker exec adres-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Health check manual: EXITOSO"
else
    echo "❌ Health check manual: FALLÓ"
fi
echo ""

# 9. Recursos del sistema
echo "9️⃣  Recursos del sistema:"
echo "   CPU y Memoria del contenedor:"
docker stats adres-sqlserver --no-stream
echo ""

echo "========================================="
echo "  DIAGNÓSTICO COMPLETADO"
echo "========================================="
echo ""
echo "📝 Si SQL Server está healthy, ejecuta:"
echo "   docker compose up -d"
echo ""
echo "📝 Si sigue fallando, revisa:"
echo "   - Los logs arriba"
echo "   - Memoria disponible (SQL necesita ~2GB)"
echo "   - Permisos del volumen"
