# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar todos los archivos de proyecto para restaurar dependencias en caché
COPY ["CrudService.sln", "./"]
COPY ["src/CrudService.Domain/CrudService.Domain.csproj",           "src/CrudService.Domain/"]
COPY ["src/CrudService.Application/CrudService.Application.csproj", "src/CrudService.Application/"]
COPY ["src/CrudService.Infrastructure/CrudService.Infrastructure.csproj", "src/CrudService.Infrastructure/"]
COPY ["src/CrudService.Api/CrudService.Api.csproj",                 "src/CrudService.Api/"]

RUN dotnet restore "src/CrudService.Api/CrudService.Api.csproj"

# Copiar código fuente y compilar
COPY . .

RUN dotnet build  "src/CrudService.Api/CrudService.Api.csproj" -c Release -o /app/build
RUN dotnet publish "src/CrudService.Api/CrudService.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD curl -f http://localhost:8080/api/events || exit 1

ENTRYPOINT ["dotnet", "CrudService.Api.dll"]
