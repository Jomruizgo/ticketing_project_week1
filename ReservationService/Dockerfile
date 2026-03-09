FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

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
RUN dotnet publish \
		-c ${BUILD_CONFIGURATION} \
		--no-restore \
		-p:UseAppHost=false \
		-o /app/publish

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN groupadd --system appgroup \
		&& useradd --system --gid appgroup --create-home --home-dir /home/appuser appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
	CMD kill -0 1 || exit 1

ENTRYPOINT ["dotnet", "ReservationService.Worker.dll"]
