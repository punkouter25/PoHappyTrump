using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoHappyTrump.Models;

namespace PoHappyTrump.Services
{
    public class TrumpMessageService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TrumpMessageService> _logger;
        private readonly IOpenAiTransformationService _openAiTransformationService;
        private readonly TrumpMessageSettings _settings;
        private static readonly Random _random = new Random();
        private List<string>? _messages;
        private readonly object _lock = new object();

        public TrumpMessageService(
            HttpClient httpClient,
            ILogger<TrumpMessageService> logger,
            IOptions<TrumpMessageSettings> settings,
            IOpenAiTransformationService openAiTransformationService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;
            _openAiTransformationService = openAiTransformationService;
        }

        private async Task<List<string>> GetOrFetchMessagesAsync()
        {
            lock (_lock)
            {
                if (_messages != null)
                {
                    _logger.LogInformation("Returning cached messages.");
                    return _messages;
                }
            }

            _logger.LogInformation("Fetching and filtering messages from RSS feed: {RssFeedUrl}", _settings.RssFeedUrl);
            var messages = new List<string>();

            try
            {
                var feedXml = await _httpClient.GetStringAsync(_settings.RssFeedUrl);
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
                            var messageContent = item.Summary?.Text ?? (item.Content as TextSyndicationContent)?.Text;

                            if (!string.IsNullOrEmpty(messageContent))
                            {
                                var wordCount = messageContent.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                                if (wordCount >= 1)
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
                        _logger.LogInformation("Finished processing RSS feed items. Found {MessageCount} messages with at least 1 word.", messages.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to load RSS feed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching or parsing RSS feed from {RssFeedUrl}", _settings.RssFeedUrl);
            }

            lock (_lock)
            {
                _messages = messages;
            }

            return messages;
        }

        public async Task<string?> GetRandomPositiveMessageAsync()
        {
            _logger.LogInformation("Getting a random positive message.");

            var messages = await GetOrFetchMessagesAsync();

            if (messages.Count == 0)
            {
                _logger.LogWarning("No messages found with at least 1 word.");
                return null;
            }

            var randomIndex = _random.Next(messages.Count);
            var randomMessage = messages[randomIndex];

            _logger.LogInformation("Selected random message for positivity transformation.");

            return await _openAiTransformationService.MakeMessagePositiveAsync(randomMessage);
        }

        public async Task<string?> GetRandomOriginalMessageAsync()
        {
            _logger.LogInformation("Getting a random original message without transformation.");

            var messages = await GetOrFetchMessagesAsync();

            if (messages.Count == 0)
            {
                _logger.LogWarning("No messages found with at least 1 word.");
                return null;
            }

            var randomIndex = _random.Next(messages.Count);
            var randomMessage = messages[randomIndex];

            _logger.LogInformation("Selected random original message.");

            return randomMessage;
        }

        public async Task<MessageComparison?> GetMessageComparisonAsync()
        {
            _logger.LogInformation("Getting message comparison (original vs enhanced).");

            var messages = await GetOrFetchMessagesAsync();

            if (messages.Count == 0)
            {
                _logger.LogWarning("No messages found with at least 1 word.");
                return null;
            }

            var randomIndex = _random.Next(messages.Count);
            var originalMessage = messages[randomIndex];

            _logger.LogInformation("Selected random message for comparison transformation.");

            var enhancedMessage = await _openAiTransformationService.MakeMessagePositiveAsync(originalMessage);
            
            var (status, note) = DetermineTransformationStatus(enhancedMessage);
            var wasTransformed = status == TransformationStatus.Success;

            return new MessageComparison
            {
                OriginalMessage = originalMessage,
                EnhancedMessage = enhancedMessage,
                WasTransformed = wasTransformed,
                TransformationNote = note,
                Status = status
            };
        }

        private (TransformationStatus status, string note) DetermineTransformationStatus(string enhancedMessage)
        {
            if (enhancedMessage.Contains("[Note: This message was not transformed by Azure OpenAI because the service is not configured.]"))
            {
                return (TransformationStatus.NotConfigured, "Azure OpenAI service not configured");
            }
            
            if (enhancedMessage.Contains("[Note: Message transformation failed") && enhancedMessage.Contains("content_filter"))
            {
                return (TransformationStatus.ContentFiltered, "Content was filtered by Azure OpenAI content policy");
            }
            
            if (enhancedMessage.Contains("[Note: Message transformation failed"))
            {
                return (TransformationStatus.ServiceError, "Transformation failed due to service error");
            }
            
            return (TransformationStatus.Success, "Successfully transformed by Azure OpenAI");
        }
    }
}
