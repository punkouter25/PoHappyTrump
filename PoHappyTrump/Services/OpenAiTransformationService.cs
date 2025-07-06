using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Extensions.Options;
using PoHappyTrump.Models;
using System.Threading.Tasks;

namespace PoHappyTrump.Services
{
    public interface IOpenAiTransformationService
    {
        Task<string> MakeMessagePositiveAsync(string message);
    }

    public class OpenAiTransformationService : IOpenAiTransformationService
    {
        private readonly AzureOpenAIClient? _openAIClient;
        private readonly TrumpMessageSettings _settings;
        private readonly ILogger<OpenAiTransformationService> _logger;
        private readonly bool _openAIConfigured;

        public OpenAiTransformationService(IOptions<TrumpMessageSettings> settings, ILogger<OpenAiTransformationService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            if (!string.IsNullOrEmpty(_settings.AzureOpenAI.Endpoint) && !string.IsNullOrEmpty(_settings.AzureOpenAI.ApiKey) &&
                _settings.AzureOpenAI.Endpoint != "https://fallback.openai.azure.com" && _settings.AzureOpenAI.ApiKey != "fallback-key")
            {
                try
                {
                    _openAIClient = new AzureOpenAIClient(new Uri(_settings.AzureOpenAI.Endpoint), new AzureKeyCredential(_settings.AzureOpenAI.ApiKey));
                    _openAIConfigured = true;
                    _logger.LogInformation("Azure OpenAI client initialized successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Azure OpenAI client. Falling back to non-transformed messages.");
                    _openAIClient = null;
                    _openAIConfigured = false;
                }
            }
            else
            {
                _logger.LogWarning("Azure OpenAI configuration is missing or using fallback values. OpenAI transformation disabled.");
                _openAIClient = null;
                _openAIConfigured = false;
            }
        }

        public async Task<string> MakeMessagePositiveAsync(string message)
        {
            if (!_openAIConfigured || _openAIClient == null)
            {
                _logger.LogWarning("Azure OpenAI client is not initialized. Cannot transform message sentiment.");
                return $"{message}\n\n[Note: This message was not transformed by Azure OpenAI because the service is not configured.]";
            }

            try
            {
                _logger.LogInformation("Transforming message to positive sentiment using Azure OpenAI.");

                var chatClient = _openAIClient.GetChatClient(_settings.AzureOpenAI.DeploymentName);

                var systemPrompt = _settings.OpenAiSystemPrompt;
                var userPrompt = $"Transform this message to have a positive, happy sentiment: {message}";

                var response = await chatClient.CompleteChatAsync(systemPrompt, userPrompt);

                if (response?.Value?.Content?.Count > 0)
                {
                    var transformedMessage = response.Value.Content[0].Text;
                    _logger.LogInformation("Successfully transformed message using Azure OpenAI.");
                    return transformedMessage ?? message;
                }
                else
                {
                    _logger.LogWarning("Azure OpenAI returned no content. Returning original message.");
                    return message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Azure OpenAI API. Returning original message.");
                return $"{message}\n\n[Note: Message transformation failed - {ex.Message}]";
            }
        }
    }
}
