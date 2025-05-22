using Microsoft.AspNetCore.Mvc;
using PoHappyTrump.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Added for logging

namespace PoHappyTrump.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly TrumpMessageService _trumpMessageService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DiagnosticsController> _logger; // Added for logging

        public DiagnosticsController(TrumpMessageService trumpMessageService, HttpClient httpClient, ILogger<DiagnosticsController> logger)
        {
            _trumpMessageService = trumpMessageService;
            _httpClient = httpClient;
            _logger = logger; // Injected logger
        }

        [HttpGet]
        public async Task<ActionResult<List<DiagnosticResult>>> RunDiagnostics()
        {
            _logger.LogInformation("Running diagnostic checks.");

            var results = new List<DiagnosticResult>();

            // Data Connection Check (Azure Table Storage - Simulated for now)
            try
            {
                // In a real implementation, this would check connectivity to Azure Table Storage
                var dataConnectionCheck = true; // Simulate success for now
                var result = new DiagnosticResult { CheckName = "Azure Table Storage Connection", IsSuccessful = dataConnectionCheck, Details = dataConnectionCheck ? "Simulated connection to Azure Table Storage successful." : "Simulated connection to Azure Table Storage failed." };
                results.Add(result);
                _logger.LogInformation("Azure Table Storage Connection Check: {Status} - {Details}", result.IsSuccessful ? "Passed" : "Failed", result.Details);
            }
            catch (Exception ex)
            {
                var result = new DiagnosticResult { CheckName = "Azure Table Storage Connection", IsSuccessful = false, Details = $"Error checking Azure Table Storage connection: {ex.Message}" };
                results.Add(result);
                _logger.LogError(ex, "Azure Table Storage Connection Check: Failed - {Details}", result.Details);
            }

            // API Health Check (Checking if TrumpMessageService is available)
            try
            {
                 var serviceCheck = _trumpMessageService != null;
                 var result = new DiagnosticResult { CheckName = "TrumpMessageService Availability", IsSuccessful = serviceCheck, Details = serviceCheck ? "TrumpMessageService is available." : "TrumpMessageService is not available." };
                 results.Add(result);
                 _logger.LogInformation("TrumpMessageService Availability Check: {Status} - {Details}", result.IsSuccessful ? "Passed" : "Failed", result.Details);
            }
             catch (Exception ex)
            {
                var result = new DiagnosticResult { CheckName = "TrumpMessageService Availability", IsSuccessful = false, Details = $"Error checking TrumpMessageService availability: {ex.Message}" };
                results.Add(result);
                _logger.LogError(ex, "TrumpMessageService Availability Check: Failed - {Details}", result.Details);
            }

            // RSS Feed Connectivity Check
            try
            {
                // In a real implementation, this would attempt to fetch the RSS feed
                var rssFeedCheck = true; // Simulate success for now
                var result = new DiagnosticResult { CheckName = "RSS Feed Connectivity", IsSuccessful = rssFeedCheck, Details = rssFeedCheck ? "Simulated connection to RSS feed successful." : "Simulated connection to RSS feed failed." };
                results.Add(result);
                _logger.LogInformation("RSS Feed Connectivity Check: {Status} - {Details}", result.IsSuccessful ? "Passed" : "Failed", result.Details);
            }
            catch (Exception ex)
            {
                var result = new DiagnosticResult { CheckName = "RSS Feed Connectivity", IsSuccessful = false, Details = $"Error checking RSS feed connectivity: {ex.Message}" };
                results.Add(result);
                _logger.LogError(ex, "RSS Feed Connectivity Check: Failed - {Details}", result.Details);
            }

            // Azure OpenAI Connectivity Check
            try
            {
                // In a real implementation, this would attempt a simple call to Azure OpenAI
                var openAICheck = true; // Simulate success for now
                var result = new DiagnosticResult { CheckName = "Azure OpenAI Connectivity", IsSuccessful = openAICheck, Details = openAICheck ? "Simulated connection to Azure OpenAI successful." : "Simulated connection to Azure OpenAI failed." };
                results.Add(result);
                _logger.LogInformation("Azure OpenAI Connectivity Check: {Status} - {Details}", result.IsSuccessful ? "Passed" : "Failed", result.Details);
            }
            catch (Exception ex)
            {
                var result = new DiagnosticResult { CheckName = "Azure OpenAI Connectivity", IsSuccessful = false, Details = $"Error checking Azure OpenAI connectivity: {ex.Message}" };
                results.Add(result);
                _logger.LogError(ex, "Azure OpenAI Connectivity Check: Failed - {Details}", result.Details);
            }


            // Internet Connection Check (using existing check)
            try
            {
                var response = await _httpClient.GetAsync("https://www.google.com", HttpCompletionOption.ResponseHeadersRead);
                var result = new DiagnosticResult { CheckName = "Internet Connection", IsSuccessful = response.IsSuccessStatusCode, Details = response.IsSuccessStatusCode ? "Successfully reached google.com." : $"Failed to reach google.com. Status Code: {response.StatusCode}" };
                results.Add(result);
                _logger.LogInformation("Internet Connection Check: {Status} - {Details}", result.IsSuccessful ? "Passed" : "Failed", result.Details);
            }
            catch (Exception ex)
            {
                var result = new DiagnosticResult { CheckName = "Internet Connection", IsSuccessful = false, Details = $"Error checking internet connection: {ex.Message}" };
                results.Add(result);
                _logger.LogError(ex, "Internet Connection Check: Failed - {Details}", result.Details);
            }

            // Authentication Service Status (Not implemented yet)
            var authResult = new DiagnosticResult { CheckName = "Authentication Service", IsSuccessful = false, Details = "Authentication service is not configured." };
            results.Add(authResult);
            _logger.LogInformation("Authentication Service Check: {Status} - {Details}", authResult.IsSuccessful ? "Passed" : "Failed", authResult.Details);


            // Add other critical dependencies here

            _logger.LogInformation("Diagnostic checks completed.");
            return Ok(results);
        }
    }

    public class DiagnosticResult
    {
        public string? CheckName { get; set; }
        public bool IsSuccessful { get; set; }
        public string? Details { get; set; }
    }
}
