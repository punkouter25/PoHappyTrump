using PoHappyTrump.Client.Pages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using PoHappyTrump.Services;
using Serilog; // Added for Serilog
using Microsoft.ApplicationInsights.Extensibility; // Added for Application Insights
using Microsoft.Extensions.FileProviders; // Added for StaticFileOptions
using System.IO; // Added for Path.Combine

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

// Register TrumpMessageService with configuration and logging
builder.Services.AddScoped<TrumpMessageService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>(); // This will now be the configured HttpClient
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<TrumpMessageService>>();
    
    // Try to get values from configuration
    var endpoint = config["AzureOpenAI:Endpoint"];
    var key = config["AzureOpenAI:Key"];
    var deployment = config["AzureOpenAI:DeploymentName"] ?? "gpt-35-turbo";
    
    // Check if we have valid Azure OpenAI configuration
    if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
    {
        logger.LogWarning("Azure OpenAI configuration is missing. Using fallback mode without OpenAI transformation.");
        endpoint = "https://fallback.openai.azure.com"; // This is just a placeholder
        key = "fallback-key"; // This is just a placeholder
    }
    else
    {
        logger.LogInformation("Using Azure OpenAI configuration: Endpoint={Endpoint}, DeploymentName={DeploymentName}", endpoint, deployment);
    }
    
    return new TrumpMessageService(httpClient, endpoint, key, deployment, logger);
});

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
app.UseAntiforgery();
app.UseStaticFiles(); // Replaced MapStaticAssets with UseStaticFiles
app.UseBlazorFrameworkFiles(); // Added for Blazor static files
app.UseStaticFiles("/_content/PoHappyTrump.Client"); // Added for Blazor client static files


// Map controllers
app.MapControllers();

app.MapRazorComponents<PoHappyTrump.Client.App>()
    .AddInteractiveWebAssemblyRenderMode();

app.MapFallbackToFile("index.html"); // Added for Blazor fallback

app.Run();
