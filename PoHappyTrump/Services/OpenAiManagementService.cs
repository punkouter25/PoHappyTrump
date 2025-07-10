using Microsoft.Extensions.Options;
using PoHappyTrump.Models;
using System.Threading.Tasks;

namespace PoHappyTrump.Services
{
    public class OpenAiManagementService : IOpenAiManagementService
    {
        private readonly TrumpMessageSettings _settings;

        public OpenAiManagementService(IOptions<TrumpMessageSettings> settings)
        {
            _settings = settings.Value;
        }

        public Task<bool> IsAzureOpenAiConfiguredAsync()
        {
            var isConfigured = !string.IsNullOrEmpty(_settings.AzureOpenAI.Endpoint) &&
                               !string.IsNullOrEmpty(_settings.AzureOpenAI.ApiKey) &&
                               !string.IsNullOrEmpty(_settings.AzureOpenAI.DeploymentName);
            return Task.FromResult(isConfigured);
        }
    }
}
