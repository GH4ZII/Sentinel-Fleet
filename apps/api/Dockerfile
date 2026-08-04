FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY SentinelFleet.sln ./
COPY src/SentinelFleet.Api/SentinelFleet.Api.csproj src/SentinelFleet.Api/
COPY src/SentinelFleet.Application/SentinelFleet.Application.csproj src/SentinelFleet.Application/
COPY src/SentinelFleet.Domain/SentinelFleet.Domain.csproj src/SentinelFleet.Domain/
COPY src/SentinelFleet.Infrastructure/SentinelFleet.Infrastructure.csproj src/SentinelFleet.Infrastructure/
COPY src/SentinelFleet.Modules/Identity/SentinelFleet.Modules.Identity.csproj src/SentinelFleet.Modules/Identity/
COPY src/SentinelFleet.Modules/Organizations/SentinelFleet.Modules.Organizations.csproj src/SentinelFleet.Modules/Organizations/
COPY src/SentinelFleet.Modules/Assets/SentinelFleet.Modules.Assets.csproj src/SentinelFleet.Modules/Assets/
COPY src/SentinelFleet.Modules/Devices/SentinelFleet.Modules.Devices.csproj src/SentinelFleet.Modules/Devices/
COPY src/SentinelFleet.Modules/Telemetry/SentinelFleet.Modules.Telemetry.csproj src/SentinelFleet.Modules/Telemetry/
COPY src/SentinelFleet.Modules/Geofences/SentinelFleet.Modules.Geofences.csproj src/SentinelFleet.Modules/Geofences/
COPY src/SentinelFleet.Modules/Rules/SentinelFleet.Modules.Rules.csproj src/SentinelFleet.Modules/Rules/
COPY src/SentinelFleet.Modules/Detections/SentinelFleet.Modules.Detections.csproj src/SentinelFleet.Modules/Detections/
COPY src/SentinelFleet.Modules/Incidents/SentinelFleet.Modules.Incidents.csproj src/SentinelFleet.Modules/Incidents/
COPY src/SentinelFleet.Modules/RiskScoring/SentinelFleet.Modules.RiskScoring.csproj src/SentinelFleet.Modules/RiskScoring/
COPY src/SentinelFleet.Modules/AIAnalysis/SentinelFleet.Modules.AIAnalysis.csproj src/SentinelFleet.Modules/AIAnalysis/
COPY src/SentinelFleet.Modules/Audit/SentinelFleet.Modules.Audit.csproj src/SentinelFleet.Modules/Audit/

RUN dotnet restore src/SentinelFleet.Api/SentinelFleet.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/SentinelFleet.Api/SentinelFleet.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SentinelFleet.Api.dll"]
