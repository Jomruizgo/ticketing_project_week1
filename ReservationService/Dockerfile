FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies (layer caching)
COPY src/ReservationService.Domain/*.csproj ./ReservationService.Domain/
COPY src/ReservationService.Application/*.csproj ./ReservationService.Application/
COPY src/ReservationService.Infrastructure/*.csproj ./ReservationService.Infrastructure/
COPY src/ReservationService.Worker/*.csproj ./ReservationService.Worker/
RUN dotnet restore ReservationService.Worker/ReservationService.Worker.csproj

# Copy source code and publish
COPY src/. ./
WORKDIR /src/ReservationService.Worker
RUN dotnet publish -c Release -o /app/publish

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN groupadd -r appgroup && useradd -r -g appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ReservationService.Worker.dll"]
