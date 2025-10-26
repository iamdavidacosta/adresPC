#!/bin/bash

# Script de deployment para PKCE Authorization Code Flow
# Ejecutar en: administrator@VPS-PERFORCE

set -e  # Salir si hay error

echo "🚀 Iniciando deployment con soporte PKCE..."

# 1. Navegar al directorio del proyecto
cd ~/adresPC

# 2. Pull de los últimos cambios
echo "📥 Descargando últimos cambios..."
git pull origin stg

# 3. Detener contenedores actuales
echo "⏹️  Deteniendo contenedores..."
docker compose down

# 4. Reconstruir imagen del API (incluye cambios PKCE)
echo "🔨 Reconstruyendo imagen API..."
docker compose build api

# 5. Iniciar contenedores
echo "▶️  Iniciando contenedores..."
docker compose up -d

# 6. Esperar a que el API esté listo
echo "⏳ Esperando a que el API esté listo..."
sleep 10

# 7. Verificar estado
echo "🔍 Verificando estado..."
docker compose ps

# 8. Ver logs
echo ""
echo "📋 Últimos logs del API:"
docker logs adres-api --tail 50

echo ""
echo "✅ Deployment completado!"
echo ""
echo "🧪 Para probar:"
echo "   1. Navega a: https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize"
echo "   2. Serás redirigido a Autentic Sign"
echo "   3. Después del login, obtendrás los tokens"
echo ""
echo "📊 Ver logs en tiempo real:"
echo "   docker logs adres-api --tail 100 -f"
