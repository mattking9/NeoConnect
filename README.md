# NeoConnect

NeoConnect is a cross-platform service designed to automate and optimize Heatmiser Neo smart heating systems by adding intelligent overrides based on external weather conditions.

## Features

- **Automated Heating Management:**
  - Adjusts preheat times for individual thermostats based on the weather forecast for the day ahead to avoid unnecessary heating when natural temperature increases are expected.
  - Runs user-defined recipes (e.g. switching between winter and summer profiles) based on the forecast external temperature.
  - User-configurable schedule.
  - Email notifications for all changes or issues.
- **NeoHub Integration:**
  - Connects to NeoHub devices to retrieve device and profile data, and to apply configuration changes.
- **Change Tracking:**
  - Maintains a log of changes made to device settings for auditing and troubleshooting.
- **Weather-Aware Automation:**
  - Integrates with weather forecast data to make intelligent decisions about heating schedules and recipes.

## Cross-Platform Service

NeoConnect is implemented as a .NET Worker Service using the `BackgroundService` class, making it suitable for running as a background service on both Windows and Linux (systemd) environments. It targets .NET 8, ensuring compatibility across major platforms.

## Configuration

Configuration is managed via appsettings.json or environment variables. Key settings include:
- NeoHub connection details
- Heating variables and threshold values
- Weather API integration
- SMTP configuration

## Usage

- While running, the service will automatically connect to the specified NeoHub, retrieve schedules and weather data, and adjust heating settings on the connected NeoStat devices as appropriate.
- The default schedule is set to run once every day at 2:00 AM. This can be overriden via appsettings or environment variables.
- All actions and changes are logged for review.

## Requirements

- .NET 8 SDK or runtime
- Docker (optional, for containerized deployments)

## Running in Docker

NeoConnect can be containerized and run in Docker for easy deployment and management. Example steps:

1. **Build the Docker image:**
   ```sh
   docker build -t neoconnect .
   ```
2. **Run the container:**
   ```sh
   docker run -d --name neoconnect neoconnect:latest \
      -e NeoHub__Uri=wss://local_hub_uri_and_port \
      -e NeoHub__ApiKey=local_hub_api_key \
      -e WeatherApi__ApiKey=your_weatherapi.com_api_key \
      -e WeatherApi__Location=area_or_postcode \
      -e Smtp__Host=smtp.mailhost.com \
      -e Smtp__Port=587 \
      -e Smtp__Username=from@sender.com \
      -e Smtp__Password=sender_password \
      -e Smtp__ToAddress=to@recipient.com \
   ```
   Adjust environment variables and volume mounts as needed for your configuration files.

## License

This project is provided under the MIT License.
