# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copiar archivos de proyecto para restaurar dependencias (cache de capas)
COPY ["src/CrudService.Api/CrudService.Api.csproj", "src/CrudService.Api/"]
COPY ["src/CrudService.Application/CrudService.Application.csproj", "src/CrudService.Application/"]
COPY ["src/CrudService.Domain/CrudService.Domain.csproj", "src/CrudService.Domain/"]
COPY ["src/CrudService.Infrastructure/CrudService.Infrastructure.csproj", "src/CrudService.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "src/CrudService.Api/CrudService.Api.csproj"

# Copiar código fuente y compilar
COPY . .

# Publicar
RUN dotnet publish "src/CrudService.Api/CrudService.Api.csproj" \
  -c ${BUILD_CONFIGURATION} \
  --no-restore \
  -p:UseAppHost=false \
  -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Instalar curl para healthcheck y crear usuario no root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system appgroup \
    && useradd --system --gid appgroup --create-home --home-dir /home/appuser appuser

# Copiar artefactos compilados
COPY --from=build --chown=appuser:appgroup /app/publish .

EXPOSE 8080

USER appuser

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD curl -fsS http://localhost:8080/health || exit 1

# Inicializar aplicación
ENTRYPOINT ["dotnet", "CrudService.Api.dll"]
