# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivo de proyecto
COPY ["CrudService.csproj", "./"]

# Restaurar dependencias
RUN dotnet restore "CrudService.csproj"

# Copiar código fuente
COPY . .

# Compilar
RUN dotnet build "CrudService.csproj" -c Release -o /app/build

# Publicar
RUN dotnet publish "CrudService.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copiar artefactos compilados
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 8080

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD curl -f http://localhost:8080/api/events || exit 1

# Inicializar aplicación
ENTRYPOINT ["dotnet", "CrudService.dll"]
