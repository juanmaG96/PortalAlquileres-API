# Multi-stage Dockerfile for .NET 10 Web API
# Optimized for Northflank / Serverless deployments

# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copiamos el archivo de proyecto y restauramos dependencias
# (Aprovechamos el caché de capas de Docker)
COPY Marketplace.API.csproj ./
RUN dotnet restore

# Copiamos el resto del código y compilamos en modo Release
COPY . ./
RUN dotnet publish Marketplace.API.csproj -c Release -o /app/out --no-restore

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Northflank suele escuchar por defecto en el puerto 5000 u 8080.
# Configuramos la variable de entorno para forzar a Kestrel a usar el 5000.
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

# Copiamos los archivos ya compilados de la etapa anterior
COPY --from=build-env /app/out .

# Ejecutamos el contenedor sin permisos de root por seguridad
USER $APP_UID

# Punto de entrada de la aplicación
ENTRYPOINT ["dotnet", "Marketplace.API.dll"]