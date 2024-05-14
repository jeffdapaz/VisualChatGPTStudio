using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace JeffPires.VisualChatGPTStudio.Options
{
    /// <summary>
    /// Represents a class that provides a dialog page for displaying general options.
    /// </summary>
    [ComVisible(true)]
    public class OptionPageGridGeneral : DialogPage
    {
        #region General

        [Category("General")]
        [DisplayName("API Key")]
        [Description("Set API Key. For OpenAI API, see \"https://beta.openai.com/account/api-keys\" for more details.")]
        public string ApiKey { get; set; }

        [Category("General")]
        [DisplayName("OpenAI Service")]
        [Description("Select how to connect: OpenAI API or Azure OpenAI.")]
        [DefaultValue(OpenAIService.OpenAI)]
        [TypeConverter(typeof(EnumConverter))]
        public OpenAIService Service { get; set; }

        [Category("General")]
        [DisplayName("Single Response")]
        [Description("If true, the entire response will be displayed at once (less undo history but longer waiting time). The commands Add Summary, Add Tests, Complete and Optimize will only works as \"Single Response\".")]
        [DefaultValue(false)]
        public bool SingleResponse { get; set; } = false;

        [Category("General")]
        [DisplayName("Proxy")]
        [Description("Connect to OpenAI through a proxy.")]
        [DefaultValue("")]
        public string Proxy { get; set; } = string.Empty;

        [Category("General")]
        [DisplayName("Minify Requests")]
        [Description("If true, all requests to OpenAI will be minified. Ideal to save Tokens.")]
        [DefaultValue(false)]
        public bool MinifyRequests { get; set; } = false;

        [Category("General")]
        [DisplayName("Characters To Remove From Requests")]
        [Description("Add characters or words to be removed from all requests made to OpenAI. They must be separated by commas, e.g. a,1,TODO:,{")]
        [DefaultValue("")]
        public string CharactersToRemoveFromRequests { get; set; } = string.Empty;

        [Category("General")]
        [DisplayName("Log Requests")]
        [Description("If true, all requests to OpenAI will be logged to the Output window.")]
        [DefaultValue(false)]
        public bool LogRequests { get; set; } = false;

        [Category("General")]
        [DisplayName("Log Responses")]
        [Description("If true, all responses to OpenAI will be logged to the Output window.")]
        [DefaultValue(false)]
        public bool LogResponses { get; set; } = false;

        [Category("General")]
        [DisplayName("Generate Git Changes Comment Command")]
        [Description("Command to request to OpenAI generates a comment based on current repository git changes.")]
        [DefaultValue("Based on the changes, write a concise comment for I can use in readme file and/or git changes push comment. Write each change in a new line. Use Markdown format.")]
        public string GenerateGitCommentCommand { get; set; } = "Based on the changes, write a concise comment for I can use in readme file and/or git changes push comment. Write each change in a new line. Use Markdown format.";

        [Category("General")]
        [DisplayName("Code Review Command")]
        [Description("Command to request to OpenAI generates a Code Review on current repository git changes.")]
        [DefaultValue("Make a code view of the following changes and indicate if there is anything that can or should be modified taking into account the following points: performance, readable and well-structured code, use of known patterns, security, potential bugs that must be eliminated, etc. If there is no recommendation for a point, ignore it, otherwise, say what is wrong and provide suggestions, including code examples. Be very concise and direct in your response, provide the code review in a very summarized manner.")]
        public string CodeReviewCommand { get; set; } = "Make a code view of the following changes and indicate if there is anything that can or should be modified taking into account the following points: performance, readable and well-structured code, use of known patterns, security, potential bugs that must be eliminated, etc. If there is no recommendation for a point, ignore it, otherwise, say what is wrong and provide suggestions, including code examples. Be very concise and direct in your response, provide the code review in a very summarized manner.";

        #endregion General

        #region Model Parameters        

        [Category("Model Parameters")]
        [DisplayName("Max Tokens")]
        [Description("See \"https://help.openai.com/en/articles/4936856-what-are-tokens-and-how-to-count-them\" for more details.")]
        [DefaultValue(2048)]
        public int MaxTokens { get; set; } = 2048;

        [Category("Model Parameters")]
        [DisplayName("Temperature")]
        [Description("What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative applications, and 0 for ones with a well-defined answer.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double Temperature { get; set; } = 0;

        [Category("Model Parameters")]
        [DisplayName("Presence Penalty")]
        [Description("The scale of the penalty applied if a token is already present at all. Should generally be between 0 and 1, although negative numbers are allowed to encourage token reuse.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double PresencePenalty { get; set; } = 0;

        [Category("Model Parameters")]
        [DisplayName("Frequency Penalty")]
        [Description("The scale of the penalty for how often a token is used. Should generally be between 0 and 1, although negative numbers are allowed to encourage token reuse.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double FrequencyPenalty { get; set; } = 0;

        [Category("Model Parameters")]
        [DisplayName("top p")]
        [Description("An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double TopP { get; set; } = 0;

        [Category("Model Parameters")]
        [DisplayName("Stop Sequences")]
        [Description("Up to 4 sequences where the API will stop generating further tokens. The returned text will not contain the stop sequence. Separate different stop strings by a comma e.g. '},;,stop'")]
        [DefaultValue("")]
        public string StopSequences { get; set; } = string.Empty;

        #endregion Model Parameters   

        #region Azure

        [Category("Azure")]
        [DisplayName("Resource Name")]
        [Description("The Azure OpenAI resource name.")]
        [DefaultValue("")]
        public string AzureResourceName { get; set; } = string.Empty;

        [Category("Azure")]
        [DisplayName("Deployment Name")]
        [Description("Set Azure OpenAI deployment name.")]
        [DefaultValue("")]
        public string AzureDeploymentId { get; set; } = string.Empty;

        [Category("Azure")]
        [DisplayName("API Version")]
        [Description("Set the Azure OpenAI API version. You can check the available versions here: https://learn.microsoft.com/en-us/azure/ai-services/openai/reference#completions")]
        [DefaultValue("2023-05-15")]
        public string AzureApiVersion { get; set; } = "2023-05-15";

        #endregion Azure

        #region OpenAI

        [Category("OpenAI")]
        [DisplayName("Organization")]
        [Description("Set the OpenAI Organization. (Optional)")]
        public string OpenAIOrganization { get; set; }

        [Category("OpenAI")]
        [DisplayName("Base API URL")]
        [Description("Change the API connection URL if you wish to do so for some reason, for example use a custom LLM deployment. Example: https://myurl.openai.com")]
        [DefaultValue("")]
        public string BaseAPI { get; set; } = string.Empty;

        [Category("OpenAI")]
        [DisplayName("Model Language")]
        [Description("See \"https://platform.openai.com/docs/models/overview\" for more details.")]
        [DefaultValue(ModelLanguageEnum.GPT_3_5_Turbo)]
        [TypeConverter(typeof(EnumConverter))]
        public ModelLanguageEnum Model { get; set; } = ModelLanguageEnum.GPT_3_5_Turbo;

        [Category("OpenAI")]
        [DisplayName("Model Language Override")]
        [Description("Specify a custom model name for custom API's. Overrides Model Language if not empty.")]
        [DefaultValue("")]
        public string CustomModel { get; set; } = "";

        #endregion OpenAI

        #region Turbo Chat

        [Category("Turbo Chat")]
        [DisplayName("Turbo Chat Behavior")]
        [Description("Set the behavior of the assistant.")]
        [DefaultValue("You are a programmer assistant called Visual chatGPT Studio, and your role is help developers and resolve programmer problems.")]
        public string TurboChatBehavior { get; set; } = "You are a programmer assistant called Visual chatGPT Studio, and your role is help developers and resolve programmer problems.";

        [Category("Turbo Chat")]
        [DisplayName("Turbo Chat Code Command")]
        [Description("Define the instruction that will send to the assistant when requesting code assistance.")]
        [DefaultValue("Apply the change requested by the user to the code, but rewrite the original code that was not changed")]
        public string TurboChatCodeCommand { get; set; } = "Apply the change requested by the user to the code, but rewrite the original code that was not changed";

        #endregion Turbo Chat
    }
}
