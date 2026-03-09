# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copiar archivos de proyecto para restaurar dependencias en caché
COPY ["MsPaymentService.sln", "./"]
COPY ["src/MsPaymentService.Domain/MsPaymentService.Domain.csproj",           "src/MsPaymentService.Domain/"]
COPY ["src/MsPaymentService.Application/MsPaymentService.Application.csproj", "src/MsPaymentService.Application/"]
COPY ["src/MsPaymentService.Infrastructure/MsPaymentService.Infrastructure.csproj", "src/MsPaymentService.Infrastructure/"]
COPY ["src/MsPaymentService.Worker/MsPaymentService.Worker.csproj",           "src/MsPaymentService.Worker/"]

RUN dotnet restore "src/MsPaymentService.Worker/MsPaymentService.Worker.csproj"

# Copiar código fuente y compilar
COPY . .

# Publicar
RUN dotnet publish "MsPaymentService.Worker/MsPaymentService.Worker.csproj" \
    -c ${BUILD_CONFIGURATION} \
    --no-restore \
    -p:UseAppHost=false \
    -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

RUN groupadd --system appgroup \
    && useradd --system --gid appgroup --create-home --home-dir /home/appuser appuser

# Copiar artefactos compilados
COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

# Health check — Worker sin HTTP: verifica que el proceso principal (PID 1) esté vivo
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD kill -0 1 || exit 1

# Inicializar aplicación
ENTRYPOINT ["dotnet", "MsPaymentService.Worker.dll"]