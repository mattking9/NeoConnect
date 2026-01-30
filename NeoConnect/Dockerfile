
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

EXPOSE 8080

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NeoConnect.csproj", "."]
RUN dotnet restore "./NeoConnect.csproj" \
    --runtime alpine-x64 \
    --disable-parallel

COPY . .

# This stage is used to publish the service project to be copied to the final stage
RUN dotnet publish "./NeoConnect.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    --runtime alpine-x64 \
    --self-contained false \
    /p:DebugType=None \
    /p:DebugSymbols=false \
    /p:PublishReadyToRun=true \
    /p:EnableCompressionInSingleFile=true


# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Remove unnecessary files
RUN rm -rf \
    *.pdb \
    *.xml \
    runtimes/*/native/*.a \
    wwwroot/css/open-iconic/FONT-LICENSE \
    wwwroot/css/open-iconic/ICON-LICENSE \
    wwwroot/css/open-iconic/README.md

ENTRYPOINT ["dotnet", "./NeoConnect.dll"]