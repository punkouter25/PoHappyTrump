using Xunit;
using PoHappyTrump.Services;
using System.Net.Http;
using System.Net.Http.Headers; // Add this line
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected; // Add this line
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PoHappyTrump.Tests
{
    public class TrumpMessageServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<TrumpMessageService>> _mockLogger;
        private readonly TrumpMessageService _service;
        private const string TestRssFeedUrl = "https://www.trumpstruth.org/feed"; // Defined RSS feed URL locally

        public TrumpMessageServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<TrumpMessageService>>();
            
            // Setup configuration to return test values
            _mockConfig.Setup(c => c.GetSection("AzureOpenAI:Endpoint").Value).Returns("https://test.openai.azure.com/");
            _mockConfig.Setup(c => c.GetSection("AzureOpenAI:Key").Value).Returns("test-key");
            _mockConfig.Setup(c => c.GetSection("AzureOpenAI:DeploymentName").Value).Returns("gpt-35-turbo");

            _service = new TrumpMessageService(
                _httpClient, 
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
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri == new Uri(TestRssFeedUrl)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(testFeed)
                });

            // Act
            var result = await _service.GetFilteredMessagesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Should have 2 messages with >= 1 word
            Assert.Contains("Short", result);
            Assert.Contains("This message has more than one word", result);
        }

        [Fact]
        public async Task MakeMessagePositiveAsync_ReturnsMessageWithNote()
        {
            // Arrange
            var originalMessage = "This is a test message";
            
            // Act
            var result = await _service.MakeMessagePositiveAsync(originalMessage);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(originalMessage, result); // Original message should be included
            Assert.Contains("[Note: This message was not transformed by Azure OpenAI because the service is not configured.]", result);
        }

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

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(testFeed)
                });

            // Act
            var result = await _service.GetRandomPositiveMessageAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<string>(result);
            Assert.NotEmpty(result);
        }
    }
}
