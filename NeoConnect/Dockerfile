
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

EXPOSE 8080

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NeoConnect.csproj", "."]
RUN dotnet restore "./NeoConnect.csproj"
COPY . .

# This stage is used to publish the service project to be copied to the final stage
RUN dotnet publish "./NeoConnect.csproj" -c $BUILD_CONFIGURATION -o /app/publish


# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "./NeoConnect.dll"]