# Use runtime-deps for AOT since we don't need the full .NET runtime
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-jammy-chiseled AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RinhaBackend.Worker/RinhaBackend.Worker.csproj", "RinhaBackend.Worker/"]
RUN dotnet restore "RinhaBackend.Worker/RinhaBackend.Worker.csproj" -r linux-x64
COPY . .
WORKDIR "/src/RinhaBackend.Worker"
RUN dotnet build "RinhaBackend.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RinhaBackend.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
    -r linux-x64 \
    --self-contained true \
    /p:PublishAot=true \
    /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Use the native executable directly instead of dotnet
ENTRYPOINT ["./RinhaBackend.Worker"]
