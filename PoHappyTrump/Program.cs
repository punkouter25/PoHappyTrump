using PoHappyTrump.Client.Pages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using PoHappyTrump.Services;
using Serilog; // Added for Serilog
using Microsoft.Extensions.FileProviders; // Added for StaticFileOptions
using System.IO; // Added for Path.Combine
using PoHappyTrump.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog to create new log.txt each run
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Infinite, fileSizeLimitBytes: null, retainedFileCountLimit: null) // Create new log.txt each run
    .CreateLogger();

builder.Host.UseSerilog(); // Use Serilog for hosting

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add controllers
builder.Services.AddControllers();

// Add CORS for Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Commented out Application Insights services for troubleshooting
// builder.Services.AddApplicationInsightsTelemetry();

// Add logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders(); // Clear default providers
    loggingBuilder.AddSerilog(dispose: true); // Add Serilog
});

// Register HttpClient with a handler that ignores SSL errors for development
builder.Services.AddHttpClient<TrumpMessageService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

// Add HttpClient for diagnostics
builder.Services.AddHttpClient();

// Configure the Options pattern for TrumpMessageSettings
builder.Services.Configure<TrumpMessageSettings>(builder.Configuration.GetSection(TrumpMessageSettings.SectionName));

// Register the services
builder.Services.AddScoped<IOpenAiTransformationService, OpenAiTransformationService>();
builder.Services.AddScoped<TrumpMessageService>();

var app = builder.Build();

// Ensure Serilog is shut down when the application stops
app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors();

app.UseAntiforgery();
app.UseBlazorFrameworkFiles(); // For Blazor WebAssembly static files
app.UseStaticFiles(); // For serving client static files


// Map controllers
app.MapControllers();

app.MapRazorComponents<PoHappyTrump.Client.App>()
    .AddInteractiveWebAssemblyRenderMode();

app.MapFallbackToFile("index.html"); // Added for Blazor fallback

app.Run();
