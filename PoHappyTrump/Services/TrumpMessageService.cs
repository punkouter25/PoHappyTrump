using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PoHappyTrump.Services
{
    public class TrumpMessageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAIEndpoint;
        private readonly string _openAIKey;
        private readonly string _openAIDeploymentName;
        private readonly OpenAIClient? _openAIClient;
        private readonly ILogger<TrumpMessageService> _logger;
        private const string RssFeedUrl = "https://www.trumpstruth.org/feed";

        public TrumpMessageService(HttpClient httpClient, string openAIEndpoint, string openAIKey, string openAIDeploymentName, ILogger<TrumpMessageService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _openAIDeploymentName = openAIDeploymentName;
            _openAIEndpoint = openAIEndpoint;
            _openAIKey = openAIKey;
            
            if (string.IsNullOrEmpty(openAIEndpoint) || string.IsNullOrEmpty(openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                _logger.LogError("Azure OpenAI configuration is missing or incomplete.");
                _openAIClient = null;
            }
            else
            {
                try 
                {
                    // Ensure the endpoint is a valid URI with http or https scheme
                    var uri = openAIEndpoint;
                    if (!uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                        !uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        uri = "https://" + uri;
                    }
                    
                    _logger.LogInformation("Initializing OpenAI client with URI: {Uri}", uri);
                    _openAIClient = new OpenAIClient(new Uri(uri), new AzureKeyCredential(openAIKey));
                }
                catch (UriFormatException)
                {
                    _logger.LogError("Invalid OpenAI endpoint URI format: {Uri}", openAIEndpoint);
                    _openAIClient = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create OpenAI client with endpoint {Endpoint}", openAIEndpoint);
                    _openAIClient = null;
                }
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

                            var messageContent = item.Summary?.Text ?? item.Content?.ToString();

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
        }        public async Task<string> MakeMessagePositiveAsync(string message)
        {
            _logger.LogInformation("Attempting to make message positive using Azure OpenAI.");
            if (_openAIClient is null)
            {
                _logger.LogWarning("Azure OpenAI client is not initialized. Cannot make message positive.");
                return $"{message}\n\n[Note: This message was not transformed by Azure OpenAI because the service is not configured.]";
            }
            
            try
            {
                var chatCompletionsOptions = new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatRequestSystemMessage("You are a helpful assistant. Rewrite the following text to make all negative words positive. Keep the original meaning as much as possible."),
                        new ChatRequestUserMessage(message)
                    }
                };
                
                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                var positiveMessage = response.Value.Choices[0].Message.Content;
                _logger.LogInformation("Successfully made message positive.");
                return positiveMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Azure OpenAI chat API. Returning original message.");
                return message;
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

            var positiveMessage = await MakeMessagePositiveAsync(randomMessage);

            _logger.LogInformation("Finished getting random positive message.");
            return positiveMessage;
        }
    }
}
