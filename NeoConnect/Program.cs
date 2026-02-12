using NeoConnect;
using NeoConnect.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseStaticWebAssets();

builder.Services.AddLogging(logging =>
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    })
);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<DeviceRepository>();
builder.Services.AddSingleton<InMemoryDataService>();

builder.Services.AddSingleton<IDataService, DataService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IHeatingService, HeatingService>();
builder.Services.AddSingleton<INeoHubService, NeoHubService>();
builder.Services.AddSingleton<INeoConnectionFactory, NeoConnectionFactory>();

builder.Services.AddSingleton<BathroomBoostAction>();
builder.Services.AddSingleton<GlobalHoldAction>();
builder.Services.AddSingleton<ReportDataCollectionAction>();


builder.Services.AddHostedService<ScheduledWorker<BathroomBoostAction>>();
builder.Services.AddHostedService<ScheduledWorker<GlobalHoldAction>>();
builder.Services.AddHostedService<ScheduledWorker<ReportDataCollectionAction>>();

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
