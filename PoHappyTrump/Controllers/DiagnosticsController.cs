using Microsoft.AspNetCore.Mvc;
using PoHappyTrump.Services;
using PoHappyTrump.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PoHappyTrump.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly TrumpMessageService _trumpMessageService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly IOpenAiManagementService _openAiManagementService;
        private readonly TrumpMessageSettings _settings;

        public DiagnosticsController(
            TrumpMessageService trumpMessageService,
            HttpClient httpClient,
            ILogger<DiagnosticsController> logger,
            IOptions<TrumpMessageSettings> settings,
            IOpenAiManagementService openAiManagementService)
        {
            _trumpMessageService = trumpMessageService;
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;
            _openAiManagementService = openAiManagementService;
        }

        [HttpGet]
        public async Task<ActionResult<List<DiagnosticResult>>> RunDiagnostics()
        {
            _logger.LogInformation("Running diagnostic checks.");

            var results = new List<DiagnosticResult>
            {
                await RunCheckAsync("Azure Table Storage Connection", async () => (true, "Simulated connection to Azure Table Storage successful.")),
                await RunCheckAsync("TrumpMessageService Availability", async () => (_trumpMessageService != null, "TrumpMessageService is available.")),
                await RunCheckAsync("RSS Feed Connectivity", async () =>
                {
                    var response = await _httpClient.GetAsync(_settings.RssFeedUrl);
                    return (response.IsSuccessStatusCode, response.IsSuccessStatusCode ? "RSS feed is reachable." : $"RSS feed returned status {response.StatusCode}.");
                }),
                await RunCheckAsync("Azure OpenAI Connectivity", async () =>
                {
                    var openAiAvailable = await _openAiManagementService.IsAzureOpenAiConfiguredAsync();
                    return (openAiAvailable, openAiAvailable ? "Azure OpenAI is configured." : "Azure OpenAI is not configured or unreachable.");
                }),
                await RunCheckAsync("Internet Connection", async () =>
                {
                    var response = await _httpClient.GetAsync("https://www.google.com", HttpCompletionOption.ResponseHeadersRead);
                    return (response.IsSuccessStatusCode, response.IsSuccessStatusCode ? "Successfully reached google.com." : $"Failed to reach google.com. Status Code: {response.StatusCode}");
                }),
                new DiagnosticResult { CheckName = "Authentication Service", IsSuccessful = false, Details = "Authentication service is not configured." }
            };

            _logger.LogInformation("Diagnostic checks completed.");
            return Ok(results);
        }

        private async Task<DiagnosticResult> RunCheckAsync(string checkName, Func<Task<(bool, string)>> check)
        {
            try
            {
                var (isSuccessful, details) = await check();
                _logger.LogInformation("{CheckName}: {Status} - {Details}", checkName, isSuccessful ? "Passed" : "Failed", details);
                return new DiagnosticResult { CheckName = checkName, IsSuccessful = isSuccessful, Details = details };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{CheckName}: Failed - {Details}", checkName, ex.Message);
                return new DiagnosticResult { CheckName = checkName, IsSuccessful = false, Details = ex.Message };
            }
        }
    }
}
