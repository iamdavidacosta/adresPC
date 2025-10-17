# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias
COPY ["adres.api.csproj", "./"]
RUN dotnet restore "adres.api.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar y publicar la aplicación
RUN dotnet publish "adres.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Exponer puerto HTTP
EXPOSE 8080

# Copiar archivos publicados desde la etapa de build
COPY --from=build /app/publish .

# Configurar la URL de escucha
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Punto de entrada
ENTRYPOINT ["dotnet", "adres.api.dll"]
