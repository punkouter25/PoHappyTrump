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

        public async Task<List<string>> GetFilteredMessagesAsync()
        {
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

        public async Task<string?> GetRandomPositiveMessageAsync()
        {
            _logger.LogInformation("Getting a random positive message.");
            var messages = await GetFilteredMessagesAsync();

            if (messages == null || messages.Count == 0)
            {
                _logger.LogWarning("No filtered messages found.");
                return null;
            }

            var randomIndex = _random.Next(0, messages.Count);
            var randomMessage = messages[randomIndex];
            _logger.LogInformation("Selected random message for positivity transformation.");

            // Transform the message to positive sentiment using Azure OpenAI
            return await _openAiTransformationService.MakeMessagePositiveAsync(randomMessage);
        }
    }
}
