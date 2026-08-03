FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY src/ElasticIndexer.Worker/ElasticIndexer.Worker.csproj ElasticIndexer.Worker/
COPY src/Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
RUN dotnet restore ElasticIndexer.Worker/ElasticIndexer.Worker.csproj
COPY src/ElasticIndexer.Worker/ ElasticIndexer.Worker/
COPY src/Shared.Contracts/ Shared.Contracts/

FROM restore AS build
RUN dotnet publish ElasticIndexer.Worker/ElasticIndexer.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ElasticIndexer.Worker.dll"]
