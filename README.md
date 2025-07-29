# NeoConnect

NeoConnect is a cross-platform service designed to automate and optimize Heatmiser Neo smart heating systems by adding intelligent overrides based on external weather conditions.

## Features

- **Automated Heating Management:**
  - Adjusts preheat times for individual thermostats based on the weather forecast for the day ahead to avoid unnecessary heating when natural temperature increases are expected.
  - Runs user-defined recipes (e.g. switching between winter and summer profiles) based on the forecast external temperature.
- **Fully-Configurable Schedule:**
  - User-configurable schedule ensures it runs exactly when you need it to.
- **Direct NeoHub Connection:**
  - Connects directly to NeoHub devices without needing to expose them to the internet.
- **Email Reports:**
  - Can be configured to send an email after each run giving a summary of heating changes made.

## Cross-Platform Service

NeoConnect is implemented as a .NET Worker Service using the `BackgroundService` class, making it suitable for running as a background service on both Windows and Linux (systemd) environments. It targets .NET 8, ensuring compatibility across major platforms.

## Configuration

Settings are managed via appsettings.json or environment variables. Key settings include:
- Schedule
- NeoHub connection details
- Heating variables and threshold values
- Weather API integration
- SMTP configuration

## Scheduling

While running in the background, the service will trigger according to the schedule defined in the 'Schedule' app setting / environment variable.

The schedule is defined using a Cron Expression - a mask which defines fixed times, dates and intervals. The mask consists of minute, hour, day-of-month, month and day-of-week fields:

                                           Allowed values    Allowed special characters   Comment
                    
    ┌───────────── minute                0-59              * , - /                      
    │ ┌───────────── hour                0-23              * , - /                      
    │ │ ┌───────────── day of month      1-31              * , - / L W ?                
    │ │ │ ┌───────────── month           1-12 or JAN-DEC   * , - /                      
    │ │ │ │ ┌───────────── day of week   0-6  or SUN-SAT   * , - / # L ?                Both 0 and 7 means SUN
    │ │ │ │ │
    * * * * *

| Expression           | Description                                                                           |
|----------------------|---------------------------------------------------------------------------------------|
| `* * * * *`          | Every minute                                                                          |
| `0  0 1 * *`         | At midnight, on day 1 of every month                                                  |
| `*/5 * * * *`        | Every 5 minutes                                                                       |
| `30,45-15/2 1 * * *` | Every 2 minute from 1:00 AM to 01:15 AM and from 1:45 AM to 1:59 AM and at 1:30 AM    |
| `0 0 * * MON-FRI`    | At 00:00, Monday through Friday                                                       |

Note, if you do not require the service to run to a schedule (e.g. if the service is being managed by an external scheduler, or if you just want to test it), you can simply omit the Schedule and it will trigger immediately.

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
