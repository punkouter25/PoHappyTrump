using System.Threading.Tasks;

namespace PoHappyTrump.Services
{
    public interface IOpenAiManagementService
    {
        Task<bool> IsAzureOpenAiConfiguredAsync();
    }
}
