using Community.VisualStudio.Toolkit;using Microsoft.VisualStudio.Shell;namespace JeffPires.VisualChatGPTStudio.Commands{
    /// <summary>
    /// Command to cancel all another commands that is running.
    /// </summary>
    [Command(PackageIds.Cancel)]    internal sealed class Cancel : BaseCommand<Cancel>    {
        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        protected override async System.Threading.Tasks.Task ExecuteAsync(OleMenuCmdEventArgs e)        {            CancellationTokenSource?.Cancel();        }    }}