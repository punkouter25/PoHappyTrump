using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using Azure;
using System.ClientModel;

namespace PoHappyTrump.Services
{
    public class TrumpMessageService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TrumpMessageService> _logger;
        private readonly AzureOpenAIClient? _openAIClient;
        private readonly string _deploymentName;
        private readonly bool _openAIConfigured;
        private const string RssFeedUrl = "https://www.trumpstruth.org/feed";

        public TrumpMessageService(HttpClient httpClient, string openAIEndpoint, string openAIKey, string openAIDeploymentName, ILogger<TrumpMessageService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _deploymentName = openAIDeploymentName;

            // Check if Azure OpenAI is properly configured
            if (!string.IsNullOrEmpty(openAIEndpoint) && !string.IsNullOrEmpty(openAIKey) && 
                openAIEndpoint != "https://fallback.openai.azure.com" && openAIKey != "fallback-key")
            {
                try
                {
                    _openAIClient = new AzureOpenAIClient(new Uri(openAIEndpoint), new ApiKeyCredential(openAIKey));
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

        public async Task<List<string>> GetFilteredMessagesAsync()
        {
            _logger.LogInformation("Fetching and filtering messages from RSS feed: {RssFeedUrl}", RssFeedUrl);
            var messages = new List<string>();

            try
            {
                var feedXml = await _httpClient.GetStringAsync(RssFeedUrl);
                _logger.LogInformation("Successfully fetched RSS feed content.");

                using (var reader = XmlReader.Create(new System.IO.StringReader(feedXml)))
                {
                    var feed = SyndicationFeed.Load(reader);

                    if (feed != null)
                    {
                        _logger.LogInformation("RSS feed loaded successfully. Processing items.");
                        foreach (var item in feed.Items)
                        {
                            _logger.LogDebug("Processing RSS item: Title='{Title}'", item.Title?.Text);
                            _logger.LogDebug("  Summary: '{Summary}'", item.Summary?.Text);
                            _logger.LogDebug("  Content: '{Content}'", item.Content?.ToString());

                            var messageContent = item.Summary?.Text ?? (item.Content as TextSyndicationContent)?.Text;

                            if (!string.IsNullOrEmpty(messageContent))
                            {
                                var wordCount = messageContent.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                                if (wordCount >= 1) // Changed threshold from 10 to 1
                                {
                                    messages.Add(messageContent);
                                    _logger.LogDebug("Added message with {WordCount} words.", wordCount);
                                }
                                else
                                {
                                    _logger.LogDebug("Skipped message with {WordCount} words (less than 1).", wordCount);
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Skipped empty message content.");
                            }
                        }
                        _logger.LogInformation("Finished processing RSS feed items. Found {MessageCount} messages with at least 1 word.", messages.Count); // Updated log message
                    }
                    else
                    {
                        _logger.LogWarning("Failed to load RSS feed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching or parsing RSS feed");
            }

            return messages;
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
                
                var chatClient = _openAIClient.GetChatClient(_deploymentName);
                
                var systemPrompt = "You are a helpful AI assistant that transforms messages to have a positive, uplifting sentiment while maintaining the core message. Make the tone happy, optimistic, and encouraging. Keep the response concise and natural.";
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

        public async Task<string?> GetRandomPositiveMessageAsync()
        {
            _logger.LogInformation("Getting a random positive message.");
            var messages = await GetFilteredMessagesAsync();

            if (messages == null || messages.Count == 0)
            {
                _logger.LogWarning("No filtered messages found.");
                return null;
            }

            var random = new Random();
            var randomIndex = random.Next(0, messages.Count);
            var randomMessage = messages[randomIndex];
            _logger.LogInformation("Selected random message for positivity transformation.");

            // Transform the message to positive sentiment using Azure OpenAI
            return await MakeMessagePositiveAsync(randomMessage);
        }
    }
}
