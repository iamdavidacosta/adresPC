#!/bin/bash
# ==================================================
# Script de diagnóstico para SQL Server en Docker
# ==================================================

echo "╔════════════════════════════════════════════════╗"
echo "║  Diagnóstico de SQL Server - Docker Compose   ║"
echo "╚════════════════════════════════════════════════╝"
echo ""

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "1️⃣  Verificando contenedor SQL Server..."
CONTAINER_ID=$(docker ps -a -q -f name=adres-sqlserver)

if [ -z "$CONTAINER_ID" ]; then
    echo -e "${RED}✗ Contenedor no encontrado${NC}"
    exit 1
else
    echo -e "${GREEN}✓ Contenedor encontrado: $CONTAINER_ID${NC}"
fi

echo ""
echo "2️⃣  Estado del contenedor..."
docker ps -a -f name=adres-sqlserver --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo ""
echo "3️⃣  Logs del contenedor (últimas 30 líneas)..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
docker logs adres-sqlserver --tail 30
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

echo ""
echo "4️⃣  Verificando herramientas SQL disponibles..."
if docker exec adres-sqlserver test -f /opt/mssql-tools/bin/sqlcmd 2>/dev/null; then
    echo -e "${GREEN}✓ sqlcmd (sin SSL) disponible en /opt/mssql-tools/bin/sqlcmd${NC}"
    SQLCMD_PATH="/opt/mssql-tools/bin/sqlcmd"
elif docker exec adres-sqlserver test -f /opt/mssql-tools18/bin/sqlcmd 2>/dev/null; then
    echo -e "${GREEN}✓ sqlcmd (con SSL) disponible en /opt/mssql-tools18/bin/sqlcmd${NC}"
    SQLCMD_PATH="/opt/mssql-tools18/bin/sqlcmd"
else
    echo -e "${RED}✗ sqlcmd no encontrado${NC}"
    SQLCMD_PATH=""
fi

echo ""
echo "5️⃣  Intentando conectar a SQL Server..."
if [ -n "$SQLCMD_PATH" ]; then
    echo "Usando: $SQLCMD_PATH"
    
    # Intentar sin SSL
    echo -n "   Intento 1 (sin SSL): "
    if docker exec adres-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q 'SELECT 1' -b -o /dev/null 2>&1; then
        echo -e "${GREEN}✓ EXITOSO${NC}"
    else
        echo -e "${RED}✗ FALLÓ${NC}"
        docker exec adres-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q 'SELECT 1' 2>&1 | head -5
    fi
    
    # Intentar con SSL
    echo -n "   Intento 2 (con SSL): "
    if docker exec adres-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q 'SELECT 1' -b -o /dev/null 2>&1; then
        echo -e "${GREEN}✓ EXITOSO${NC}"
    else
        echo -e "${RED}✗ FALLÓ${NC}"
        docker exec adres-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q 'SELECT 1' 2>&1 | head -5
    fi
fi

echo ""
echo "6️⃣  Variables de entorno del contenedor..."
docker exec adres-sqlserver env | grep -E "MSSQL|SA_PASSWORD"

echo ""
echo "7️⃣  Uso de recursos..."
docker stats adres-sqlserver --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"

echo ""
echo "8️⃣  Verificando puerto 1433..."
if nc -zv localhost 1433 2>&1 | grep -q succeeded; then
    echo -e "${GREEN}✓ Puerto 1433 accesible${NC}"
else
    echo -e "${YELLOW}⚠ Puerto 1433 no responde (puede ser normal si el contenedor está iniciando)${NC}"
fi

echo ""
echo "9️⃣  Health check del contenedor..."
docker inspect adres-sqlserver --format='{{.State.Health.Status}}' 2>/dev/null || echo "No configurado"

echo ""
echo "🔟 Últimos health checks..."
docker inspect adres-sqlserver --format='{{range .State.Health.Log}}{{.Output}}{{end}}' 2>/dev/null | tail -c 500

echo ""
echo "╔════════════════════════════════════════════════╗"
echo "║              RECOMENDACIONES                   ║"
echo "╚════════════════════════════════════════════════╝"
echo ""
echo "Si el contenedor está 'unhealthy':"
echo "  1. Espera 90 segundos después de iniciar (start_period)"
echo "  2. Verifica los logs con: docker logs adres-sqlserver"
echo "  3. Verifica memoria disponible: free -h"
echo "  4. Prueba reiniciar: docker compose restart sqlserver"
echo ""
echo "Si necesitas reconstruir desde cero:"
echo "  docker compose down -v"
echo "  docker compose up -d"
echo ""
