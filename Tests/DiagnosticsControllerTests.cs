using Xunit;
using PoHappyTrump.Controllers;
using PoHappyTrump.Services;
using PoHappyTrump.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected; // Add this line
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

namespace PoHappyTrump.Tests
{
    public class DiagnosticsControllerTests
    {
        private readonly Mock<TrumpMessageService> _mockTrumpMessageService;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<DiagnosticsController>> _mockLogger;
        private readonly DiagnosticsController _controller;

        public DiagnosticsControllerTests()
        {
            var mockHttpClientForService = new Mock<HttpClient>();
            var mockLoggerForService = new Mock<ILogger<TrumpMessageService>>();
            var mockSettings = new Mock<IOptions<TrumpMessageSettings>>();
            var mockOpenAiService = new Mock<IOpenAiTransformationService>();
            
            var settings = new TrumpMessageSettings { RssFeedUrl = "https://test.feed.com" };
            mockSettings.Setup(x => x.Value).Returns(settings);
            
            _mockTrumpMessageService = new Mock<TrumpMessageService>(
                mockHttpClientForService.Object, 
                mockLoggerForService.Object,
                mockSettings.Object,
                mockOpenAiService.Object);
            
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockLogger = new Mock<ILogger<DiagnosticsController>>();

            _controller = new DiagnosticsController(
                _mockTrumpMessageService.Object,
                _httpClient,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task RunDiagnostics_ReturnsListOfDiagnosticResults()
        {
            // Arrange - Setup mock HttpClient to return success for the internet check
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

            // Act
            var result = await _controller.RunDiagnostics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var diagnostics = Assert.IsType<List<DiagnosticResult>>(okResult.Value);
            Assert.NotNull(diagnostics);
            // TODO: Add more specific assertions about the number and content of diagnostic results
        }

        [Fact]
        public async Task RunDiagnostics_IncludesAzureTableStorageCheck()
        {
            // Act
            var result = await _controller.RunDiagnostics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var diagnostics = Assert.IsType<List<DiagnosticResult>>(okResult.Value);
            Assert.Contains(diagnostics, d => d.CheckName == "Azure Table Storage Connection");
        }

        [Fact]
        public async Task RunDiagnostics_IncludesTrumpMessageServiceAvailabilityCheck()
        {
            // Act
            var result = await _controller.RunDiagnostics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var diagnostics = Assert.IsType<List<DiagnosticResult>>(okResult.Value);
            Assert.Contains(diagnostics, d => d.CheckName == "TrumpMessageService Availability");
        }

        [Fact]
        public async Task RunDiagnostics_IncludesRssFeedConnectivityCheck()
        {
            // Act
            var result = await _controller.RunDiagnostics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var diagnostics = Assert.IsType<List<DiagnosticResult>>(okResult.Value);
            Assert.Contains(diagnostics, d => d.CheckName == "RSS Feed Connectivity");
        }

        [Fact]
        public async Task RunDiagnostics_IncludesAzureOpenAIConnectivityCheck()
        {
            // Act
            var result = await _controller.RunDiagnostics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var diagnostics = Assert.IsType<List<DiagnosticResult>>(okResult.Value);
            Assert.Contains(diagnostics, d => d.CheckName == "Azure OpenAI Connectivity");
        }

        [Fact]
        public async Task RunDiagnostics_IncludesInternetConnectionCheck_Success()
        {
            // Arrange - Setup mock HttpClient to return success for the internet check
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

            // Act
            var result = await _controller.RunDiagnostics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var diagnostics = Assert.IsType<List<DiagnosticResult>>(okResult.Value);
            var internetCheck = Assert.Single(diagnostics, d => d.CheckName == "Internet Connection");
            Assert.True(internetCheck.IsSuccessful);
        }

        [Fact]
        public async Task RunDiagnostics_IncludesInternetConnectionCheck_Failure()
        {
            // Arrange - Setup mock HttpClient to return failure for the internet check
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Simulated network error"));

            // Act
            var result = await _controller.RunDiagnostics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var diagnostics = Assert.IsType<List<DiagnosticResult>>(okResult.Value);
            var internetCheck = Assert.Single(diagnostics, d => d.CheckName == "Internet Connection");
            Assert.False(internetCheck.IsSuccessful);
            Assert.Contains("Simulated network error", internetCheck.Details);
        }

        [Fact]
        public async Task RunDiagnostics_IncludesAuthenticationServiceCheck()
        {
            // Act
            var result = await _controller.RunDiagnostics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var diagnostics = Assert.IsType<List<DiagnosticResult>>(okResult.Value);
            Assert.Contains(diagnostics, d => d.CheckName == "Authentication Service");
        }
    }
}
