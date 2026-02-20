# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

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

# Compilar
RUN dotnet build "src/Producer.Api/Producer.Api.csproj" -c Release -o /app/build

# Publicar
RUN dotnet publish "src/Producer.Api/Producer.Api.csproj" -c Release -o /app/publish

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
  CMD curl -f http://localhost:8080/api/tickets/health || exit 1

# Inicializar aplicación
ENTRYPOINT ["dotnet", "Producer.Api.dll"]
