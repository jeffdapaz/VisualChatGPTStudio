namespace JeffPires.VisualChatGPTStudio.Agents
{
    public enum ApprovalKind
    {
        Ask,
        AutoApprove
    }

    public class Tool
    {
        public string Name { get; init; }

        public string Description { get; init; }

        public bool Enabled { get; set; } = true;

        public ApprovalKind Approval { get; set; } = ApprovalKind.Ask;

        public bool InPrompt { get; set; } = true;

        public string GeneratePromptFragment()
        {
            if (!Enabled || !InPrompt) return string.Empty;
            return Description;
        }
    }
}
