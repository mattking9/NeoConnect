using NeoConnect;
using NeoConnect.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseStaticWebAssets();

var inMemoryLoggerProvider = new InMemoryLoggerProvider(maxLogCount: 1000);
builder.Services.AddSingleton(inMemoryLoggerProvider);

builder.Services.AddLogging(logging => {
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    });
    logging.AddProvider(inMemoryLoggerProvider);
});

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<IDeviceRepository, DeviceRepository>();
builder.Services.AddSingleton<ISmtpClientWrapper, SmtpClientWrapper>();

builder.Services.AddSingleton<IDataService, DataService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IHeatingService, HeatingService>();
builder.Services.AddSingleton<INeoHubService, NeoHubService>();
builder.Services.AddSingleton<INeoConnectionFactory, NeoConnectionFactory>();
builder.Services.AddSingleton<ISolarService, SolarService>();
builder.Services.AddSingleton<IImmersionService, ImmersionService>();
builder.Services.AddSingleton<ISolarForecastService, SolarForecastService>();

builder.Services.AddSingleton<BathroomBoostAction>();
builder.Services.AddSingleton<GlobalHoldAction>();
builder.Services.AddSingleton<ReportDataCollectionAction>();
builder.Services.AddSingleton<RunImmersionAction>();
builder.Services.AddSingleton<ForcedChargeAction>();
builder.Services.AddScoped<IScheduledAction>(sp => sp.GetRequiredService<BathroomBoostAction>());
builder.Services.AddScoped<IScheduledAction>(sp => sp.GetRequiredService<GlobalHoldAction>());
builder.Services.AddScoped<IScheduledAction>(sp => sp.GetRequiredService<ReportDataCollectionAction>());
builder.Services.AddScoped<IScheduledAction>(sp => sp.GetRequiredService<RunImmersionAction>());
builder.Services.AddScoped<IScheduledAction>(sp => sp.GetRequiredService<ForcedChargeAction>());

builder.Services.AddHostedService<ScheduledWorker<BathroomBoostAction>>();
builder.Services.AddHostedService<ScheduledWorker<GlobalHoldAction>>();
builder.Services.AddHostedService<ScheduledWorker<ReportDataCollectionAction>>();
builder.Services.AddHostedService<ScheduledWorker<RunImmersionAction>>();
builder.Services.AddHostedService<ScheduledWorker<ForcedChargeAction>>();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

// After building the app
app.UseCors("AllowLocalhost");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
