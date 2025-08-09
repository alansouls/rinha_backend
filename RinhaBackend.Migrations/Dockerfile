FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RinhaBackend.Migrations/RinhaBackend.Migrations.csproj", "RinhaBackend.Migrations/"]
RUN dotnet restore "RinhaBackend.Migrations/RinhaBackend.Migrations.csproj"
COPY . .
WORKDIR "/src/RinhaBackend.Migrations"
RUN dotnet build "./RinhaBackend.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RinhaBackend.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RinhaBackend.Migrations.dll"]
