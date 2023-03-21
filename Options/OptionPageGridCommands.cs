using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace JeffPires.VisualChatGPTStudio.Options
{
    /// <summary>
    /// Represents a class that provides a dialog page for displaying commands options.
    /// </summary>
    [ComVisible(true)]
    public class OptionPageGridCommands : DialogPage
    {
        [Category("Visual chatGPT Studio")]
        [DisplayName("Complete")]
        [Description("Set the \"Complete\" command")]
        [DefaultValue("Please complete")]
        public string Complete { get; set; } = "Please complete";

        [Category("Visual chatGPT Studio")]
        [DisplayName("Add Tests")]
        [Description("Set the \"Add Tests\" command")]
        [DefaultValue("Create unit tests")]
        public string AddTests { get; set; } = "Create unit tests";

        [Category("Visual chatGPT Studio")]
        [DisplayName("Find Bugs")]
        [Description("Set the \"Find Bugs\" command")]
        [DefaultValue("Find Bugs")]
        public string FindBugs { get; set; } = "Find Bugs";

        [Category("Visual chatGPT Studio")]
        [DisplayName("Optimize")]
        [Description("Set the \"Optimize\" command")]
        [DefaultValue("Optimize")]
        public string Optimize { get; set; } = "Optimize";

        [Category("Visual chatGPT Studio")]
        [DisplayName("Explain")]
        [Description("Set the \"Explain\" command")]
        [DefaultValue("Explain")]
        public string Explain { get; set; } = "Explain";

        [Category("Visual chatGPT Studio")]
        [DisplayName("Add Summary")]
        [Description("Set the \"Add Summary\" command")]
        [DefaultValue("Only write a comment as C# summary format like")]
        public string AddSummary { get; set; } = "Only write a comment as C# summary format like";

        [Category("Visual chatGPT Studio")]
        [DisplayName("Add Comments for one line")]
        [Description("Set the \"Add Comments\" command when one line was selected")]
        [DefaultValue("Comment")]
        public string AddCommentsForLine { get; set; } = "Comment";

        [Category("Visual chatGPT Studio")]
        [DisplayName("Add Comments for multiple lines")]
        [Description("Set the \"Add Comments\" command when multiple lines was selected")]
        [DefaultValue("Rewrite the code with comments")]
        public string AddCommentsForLines { get; set; } = "Rewrite the code with comments";

        [Category("Visual chatGPT Studio")]
        [DisplayName("Custom Command Before")]
        [Description("Define a custom command that will insert the response before the selected text")]
        [DefaultValue("")]
        public string CustomBefore { get; set; }

        [Category("Visual chatGPT Studio")]
        [DisplayName("Custom command After")]
        [Description("Define a custom command that will insert the response after the selected text")]
        [DefaultValue("")]
        public string CustomAfter { get; set; }

        [Category("Visual chatGPT Studio")]
        [DisplayName("Custom command Replace")]
        [Description("Define a custom command that will replace the selected text with the response")]
        [DefaultValue("")]
        public string CustomReplace { get; set; }
    }
}
