# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivos de proyecto para restaurar dependencias (cache de capas)
COPY ["CrudService.sln", "./"]
COPY ["src/CrudService.Api/CrudService.Api.csproj", "src/CrudService.Api/"]
COPY ["src/CrudService.Application/CrudService.Application.csproj", "src/CrudService.Application/"]
COPY ["src/CrudService.Domain/CrudService.Domain.csproj", "src/CrudService.Domain/"]
COPY ["src/CrudService.Infrastructure/CrudService.Infrastructure.csproj", "src/CrudService.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "CrudService.sln"

# Copiar código fuente
COPY . .

# Publicar
RUN dotnet publish "src/CrudService.Api/CrudService.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Instalar curl para healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar artefactos compilados
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 8080

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD curl -f http://localhost:8080/api/events || exit 1

# Inicializar aplicación
ENTRYPOINT ["dotnet", "CrudService.Api.dll"]
