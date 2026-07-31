FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY src/RedisIndexer.Worker/RedisIndexer.Worker.csproj RedisIndexer.Worker/
COPY src/Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
RUN dotnet restore RedisIndexer.Worker/RedisIndexer.Worker.csproj
COPY src/RedisIndexer.Worker/ RedisIndexer.Worker/
COPY src/Shared.Contracts/ Shared.Contracts/

FROM restore AS build
RUN dotnet publish RedisIndexer.Worker/RedisIndexer.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "RedisIndexer.Worker.dll"]
