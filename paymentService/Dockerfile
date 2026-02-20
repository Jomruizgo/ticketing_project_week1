# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

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

RUN dotnet build  "src/MsPaymentService.Worker/MsPaymentService.Worker.csproj" -c Release -o /app/build
RUN dotnet publish "src/MsPaymentService.Worker/MsPaymentService.Worker.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MsPaymentService.Worker.dll"]