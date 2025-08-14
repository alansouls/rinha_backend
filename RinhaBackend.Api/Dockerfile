# Use runtime-deps for AOT since we don't need the full .NET runtime
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Install native AOT prerequisites
RUN apt-get update && apt-get install -y \
    clang \
    zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RinhaBackend.Api/RinhaBackend.Api.csproj", "RinhaBackend.Api/"]
RUN dotnet restore "RinhaBackend.Api/RinhaBackend.Api.csproj" -r linux-x64
COPY . .
WORKDIR "/src/RinhaBackend.Api"
RUN dotnet build "RinhaBackend.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RinhaBackend.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
    -r linux-x64 \
    --self-contained true \
    /p:PublishAot=true \
    /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Use the native executable directly instead of dotnet
ENTRYPOINT ["./RinhaBackend.Api"]
