FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY src/ReservationService.Worker/*.csproj ./ReservationService.Worker/
RUN dotnet restore ReservationService.Worker/ReservationService.Worker.csproj

# Copiar codigo fuente y compilar
COPY src/ReservationService.Worker/. ./ReservationService.Worker/
WORKDIR /src/ReservationService.Worker
RUN dotnet publish -c Release -o /app/publish --no-restore

# Imagen final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Usuario no-root por seguridad
RUN groupadd -r appgroup && useradd -r -g appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ReservationService.Worker.dll"]
