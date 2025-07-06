namespace PoHappyTrump.Models
{
    public class TrumpMessageSettings
    {
        public const string SectionName = "TrumpMessage";

        public string RssFeedUrl { get; set; } = string.Empty;
        public string OpenAiSystemPrompt { get; set; } = string.Empty;
        public AzureOpenAISettings AzureOpenAI { get; set; } = new();
    }

    public class AzureOpenAISettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
    }
}
