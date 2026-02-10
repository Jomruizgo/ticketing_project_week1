# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivos de proyecto
COPY ["Producer/Producer.csproj", "Producer/"]

# Restaurar dependencias
RUN dotnet restore "Producer/Producer.csproj"

# Copiar código fuente
COPY Producer/ Producer/

# Compilar
RUN dotnet build "Producer/Producer.csproj" -c Release -o /app/build

# Publicar
RUN dotnet publish "Producer/Producer.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copiar artefactos compilados
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 8080

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD curl -f http://localhost:8080/api/tickets/health || exit 1

# Inicializar aplicación
ENTRYPOINT ["dotnet", "Producer.dll"]
