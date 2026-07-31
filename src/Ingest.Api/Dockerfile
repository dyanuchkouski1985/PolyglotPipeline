FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY src/Ingest.Api/Ingest.Api.csproj Ingest.Api/
COPY src/Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
RUN dotnet restore Ingest.Api/Ingest.Api.csproj
COPY src/Ingest.Api/ Ingest.Api/
COPY src/Shared.Contracts/ Shared.Contracts/

FROM restore AS build
RUN dotnet publish Ingest.Api/Ingest.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Ingest.Api.dll"]

FROM restore AS debug-build
RUN dotnet publish Ingest.Api/Ingest.Api.csproj -c Debug -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS debug
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg \
    && apt-get purge -y curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=debug-build /app .
ENTRYPOINT ["dotnet", "Ingest.Api.dll"]
