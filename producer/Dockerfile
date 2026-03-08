# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copiar archivos de proyecto
COPY ["src/Producer.Domain/Producer.Domain.csproj", "src/Producer.Domain/"]
COPY ["src/Producer.Application/Producer.Application.csproj", "src/Producer.Application/"]
COPY ["src/Producer.Infrastructure/Producer.Infrastructure.csproj", "src/Producer.Infrastructure/"]
COPY ["src/Producer.Api/Producer.Api.csproj", "src/Producer.Api/"]

# Restaurar dependencias
RUN dotnet restore "src/Producer.Api/Producer.Api.csproj"

# Copiar código fuente
COPY src/ src/

# Publicar
RUN dotnet publish "src/Producer.Api/Producer.Api.csproj" \
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

# Exponer puerto
EXPOSE 8080

USER appuser

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD curl -fsS http://localhost:8080/health || exit 1

# Inicializar aplicación
ENTRYPOINT ["dotnet", "Producer.Api.dll"]
