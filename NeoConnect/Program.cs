using NeoConnect;
using NeoConnect.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logging =>
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    })
);

builder.Services.AddHttpClient();


builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();


builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<DeviceRepository>();

builder.Services.AddSingleton<IDataService, DataService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IHeatingService, HeatingService>();
builder.Services.AddSingleton<INeoHubService, NeoHubService>();

builder.Services.AddScoped<ClientWebSocketWrapper>();

builder.Services.AddSingleton<BathroomBoostAction>();
builder.Services.AddSingleton<GlobalHoldAction>();
builder.Services.AddSingleton<ReportDataCollectionAction>();

builder.Services.AddHostedService<ScheduledWorker<BathroomBoostAction>>();
builder.Services.AddHostedService<ScheduledWorker<GlobalHoldAction>>();
builder.Services.AddHostedService<ScheduledWorker<ReportDataCollectionAction>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
