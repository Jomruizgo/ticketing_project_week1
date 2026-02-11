# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivos de proyecto
COPY ["MsPaymentService.Worker/MsPaymentService.Worker.csproj", "MsPaymentService.Worker/"]

# Restaurar dependencias
RUN dotnet restore "MsPaymentService.Worker/MsPaymentService.Worker.csproj"

# Copiar código fuente
COPY MsPaymentService.Worker/ MsPaymentService.Worker/

# Compilar
RUN dotnet build "MsPaymentService.Worker/MsPaymentService.Worker.csproj" -c Release -o /app/build

# Publicar
RUN dotnet publish "MsPaymentService.Worker/MsPaymentService.Worker.csproj" -c Release -o /app/publish

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
ENTRYPOINT ["dotnet", "MsPaymentService.Worker.dll"]