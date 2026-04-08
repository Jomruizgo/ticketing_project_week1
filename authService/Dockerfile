# ─────────────────────────────────────────────────────────────
#  Stage 1: Build
# ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto primero para aprovechar caché de capas
COPY AuthService.sln ./
COPY src/AuthService.Domain/AuthService.Domain.csproj                        src/AuthService.Domain/
COPY src/AuthService.Application/AuthService.Application.csproj               src/AuthService.Application/
COPY src/AuthService.Infrastructure/AuthService.Infrastructure.csproj         src/AuthService.Infrastructure/
COPY src/AuthService.Api/AuthService.Api.csproj                               src/AuthService.Api/

# Restaurar dependencias
RUN dotnet restore src/AuthService.Api/AuthService.Api.csproj

# Copiar el resto del código fuente
COPY src/ src/

# Publicar en modo Release
RUN dotnet publish src/AuthService.Api/AuthService.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─────────────────────────────────────────────────────────────
#  Stage 2: Runtime
# ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Crear usuario no-root para mayor seguridad
RUN addgroup --system --gid 1001 authservice \
 && adduser  --system --uid 1001 --ingroup authservice --no-create-home authservice

# Copiar artefactos publicados
COPY --from=build /app/publish .

# Ajustar permisos
RUN chown -R authservice:authservice /app

USER authservice

# Puerto de escucha
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "AuthService.Api.dll"]
