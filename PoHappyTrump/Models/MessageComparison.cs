namespace PoHappyTrump.Models
{
    public class MessageComparison
    {
        public string? OriginalMessage { get; set; }
        public string? EnhancedMessage { get; set; }
        public bool WasTransformed { get; set; }
        public string? TransformationNote { get; set; }
        public TransformationStatus Status { get; set; }
    }
}
