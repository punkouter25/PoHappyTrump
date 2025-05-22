using Xunit;
using PoHappyTrump.Services;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure; 
using Azure.AI.OpenAI;

namespace PoHappyTrump.Tests
{
    public class TrumpMessageServiceTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<TrumpMessageService>> _mockLogger;
        private readonly TrumpMessageService _service;
        private const string TestRssFeedUrl = "https://www.trumpstruth.org/feed"; // Defined RSS feed URL locally

        public TrumpMessageServiceTests()
        {
            _mockHttpClient = new Mock<HttpClient>();
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<TrumpMessageService>>();
            
            // Setup configuration to return test values
            _mockConfig.Setup(c => c.GetSection("AzureOpenAI:Endpoint").Value).Returns("https://test.openai.azure.com/");
            _mockConfig.Setup(c => c.GetSection("AzureOpenAI:Key").Value).Returns("test-key");
            _mockConfig.Setup(c => c.GetSection("AzureOpenAI:DeploymentName").Value).Returns("gpt-35-turbo");

            _service = new TrumpMessageService(
                _mockHttpClient.Object, 
                _mockConfig.Object.GetSection("AzureOpenAI:Endpoint").Value ?? string.Empty,
                _mockConfig.Object.GetSection("AzureOpenAI:Key").Value ?? string.Empty,
                _mockConfig.Object.GetSection("AzureOpenAI:DeploymentName").Value ?? string.Empty,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetFilteredMessagesAsync_ReturnsOnlyMessagesWithAtLeast1Word()
        {
            // Arrange - create a test RSS feed with mixed message lengths
            var testFeed = $@"<?xml version=""1.0""?>
<rss version=""2.0"">
<channel>
  <item><description>Short</description></item>
  <item><description>This message has more than one word</description></item>
  <item><description></description></item>
  <item><description>   </description></item>
</channel>
</rss>";

            // Setup mock to return test feed when the specific RSS URL is requested
            _mockHttpClient.Setup(client => client.GetStringAsync(TestRssFeedUrl, It.IsAny<CancellationToken>()))
                .ReturnsAsync(testFeed);

            // Act
            var result = await _service.GetFilteredMessagesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Should have 2 messages with >= 1 word
            Assert.Contains("Short", result);
            Assert.Contains("This message has more than one word", result);
        }

        // [Fact] // Commented out due to issues with mocking beta Azure OpenAI SDK types
        // public async Task MakeMessagePositiveAsync_ReturnsPositiveMessage()
        // {
        //     // Arrange - mock OpenAI response
        //     var originalMessage = "This is a terrible situation with bad policies";
            
        //     // Create a mock OpenAI client
        //     var mockOpenAIClient = new Mock<OpenAIClient>(); // Use OpenAIClient directly
            
        //     // Create a mock Response<ChatCompletions>
        //     var mockResponse = new Mock<Response<ChatCompletions>>();
            
        //     // Create a mock ChatCompletions object
        //     var mockChatCompletions = new Mock<ChatCompletions>();
            
        //     // Create a mock ChatChoice object
        //     var mockChatChoice = new Mock<ChatChoice>();
            
        //     // Create a mock ChatMessage object
        //     var mockChatMessage = new Mock<ChatMessage>();

        //     // Setup the mock ChatMessage to return the positive content
        //     mockChatMessage.Setup(m => m.Content).Returns("This is a great opportunity with excellent policies");

        //     // Setup the mock ChatChoice to return the mock ChatMessage
        //     mockChatChoice.Setup(ch => ch.Message).Returns(mockChatMessage.Object);

        //     // Setup the mock ChatCompletions to return a list containing the mock ChatChoice
        //     mockChatCompletions.Setup(c => c.Choices).Returns(new List<ChatChoice> { mockChatChoice.Object });

        //     // Setup the mock Response<ChatCompletions> to return the mock ChatCompletions object
        //     mockResponse.Setup(r => r.Value).Returns(mockChatCompletions.Object);

        //     // Setup the mock OpenAIClient to return the mock Response<ChatCompletions>
        //     mockOpenAIClient.Setup(client => client.GetChatCompletionsAsync(It.IsAny<ChatCompletionsOptions>(), It.IsAny<CancellationToken>()))
        //         .ReturnsAsync(mockResponse.Object);
            
        //     // Replace the OpenAI client in the service with the mock
        //     typeof(TrumpMessageService).GetField("_openAIClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        //         .SetValue(_service, mockOpenAIClient.Object);
            
        //     // Act
        //     var result = await _service.MakeMessagePositiveAsync(originalMessage);

        //     // Assert
        //     Assert.NotNull(result);
        //     Assert.NotEqual(originalMessage, result); // Should return a modified message
        //     Assert.DoesNotContain("terrible", result.ToLower());
        //     Assert.DoesNotContain("bad", result.ToLower());
        // }

        [Fact]
        public async Task GetRandomPositiveMessageAsync_ReturnsSinglePositiveMessage()
        {
            // Arrange - mock HTTP client to return test feed
            var testFeed = $@"<?xml version=""1.0""?>
<rss version=""2.0"">
<channel>
  <item><description>This message has exactly ten words</description></item>
  <item><description>This longer message has more than ten words</description></item>
</channel>
</rss>";

            var mockResponse = new HttpResponseMessage
            {
                Content = new StringContent(testFeed)
            };
            _mockHttpClient.Setup(client => client.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testFeed);

            // Act
            var result = await _service.GetRandomPositiveMessageAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<string>(result);
            Assert.NotEmpty(result);
        }
    }
}
