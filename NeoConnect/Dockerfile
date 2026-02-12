
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NeoConnect.csproj", "."]
RUN dotnet restore "./NeoConnect.csproj" --runtime linux-musl-x64
COPY . .

# This stage is used to publish the service project to be copied to the final stage
RUN dotnet publish "./NeoConnect.csproj" \
	--runtime linux-musl-x64 \
	-c $BUILD_CONFIGURATION \
	-o /app/publish \
	--self-contained true \
	-p:PublishSingleFile=true \	
    -p:EnableCompressionInSingleFile=true \
    -p:DebuggerSupport=false \
    -p:InvariantGlobalization=true \
	-p:MetadataUpdaterSupport=false \
	-p:HttpActivityPropagationSupport=false \
    --no-restore

# Final stage - minimal runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine
WORKDIR /app

# Create non-root user for security
RUN addgroup -g 1000 appuser && adduser -u 1000 -G appuser -s /bin/sh -D appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["./NeoConnect"]