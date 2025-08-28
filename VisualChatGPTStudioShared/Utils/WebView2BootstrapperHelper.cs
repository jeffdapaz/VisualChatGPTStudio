using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Provides helper methods for bootstrapping and managing the WebView2 environment.
    /// </summary>
    public static class WebView2BootstrapperHelper
    {
        // Official small bootstrapper used by Microsoft pages / samples
        private const string BootstrapperUrl = "https://go.microsoft.com/fwlink/?LinkId=2124703";
        private const string BootstrapperFileName = "MicrosoftEdgeWebview2Setup.exe";

        /// <summary>
        /// Ensures WebView2 runtime is available. If missing, downloads+runs bootstrapper.
        /// Call BEFORE creating any WebView2 control.
        /// </summary>
        public static async Task<bool> EnsureRuntimeAvailableAsync(bool silentInstallIfPossible = false, CancellationToken ct = default)
        {
            // 1) detect
            if (IsRuntimeAvailable())
            {
                return true;
            }

            MessageBoxResult consent = MessageBox.Show("Microsoft WebView2 runtime is necessary to use Turbo Chat. Install it now?", Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (consent != MessageBoxResult.Yes)
            {
                return false;
            }

            // 2) download bootstrapper to temp
            string tmpPath = Path.Combine(Path.GetTempPath(), BootstrapperFileName);

            try
            {
                using (HttpClient http = new())
                using (HttpResponseMessage resp = await http.GetAsync(BootstrapperUrl, ct))
                {
                    resp.EnsureSuccessStatusCode();
                    using FileStream fs = new(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await resp.Content.CopyToAsync(fs);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                string errorMessage = $"Failed to download the WebView2 bootstrapper: {ex.Message}{Environment.NewLine}{Environment.NewLine}You can try install it manually through https://go.microsoft.com/fwlink/?LinkId=2124703";

                MessageBox.Show(errorMessage, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }

            // 3) run installer (bootstrapper). The bootstrapper will download the matching runtime.
            try
            {
                ProcessStartInfo psi = new(tmpPath)
                {
                    UseShellExecute = true,
                    // if you want silent attempt: many installers support /silent /install for silent; may vary.
                    // If you prefer visible installer, leave Arguments empty.
                    Arguments = silentInstallIfPossible ? "/silent /install" : string.Empty,
                    // do NOT require admin by default: bootstrapper will do per-user install when possible
                };

                Process proc = Process.Start(psi);

                if (proc == null)
                {
                    return false;
                }

                // wait for installer to finish (avoid blocking UI thread)
                await Task.Run(() => proc.WaitForExit(), ct);

                // 4) re-check
                return IsRuntimeAvailable();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                string errorMessage = $"Failed to install the WebView2 bootstrapper: {ex.Message}{Environment.NewLine}{Environment.NewLine}You can try install it manually through https://go.microsoft.com/fwlink/?LinkId=2124703";

                MessageBox.Show(errorMessage, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
            finally
            {
                // try delete temp file (ignore errors)
                try { if (File.Exists(tmpPath)) { File.Delete(tmpPath); } } catch { }
            }
        }

        /// <summary>
        /// Checks if the WebView2 runtime is available on the system.
        /// </summary>
        /// <returns>
        /// True if the WebView2 runtime is installed and its version string can be retrieved; otherwise, false.
        /// </returns>
        private static bool IsRuntimeAvailable()
        {
            try
            {
                // Official detection API — docs: CoreWebView2Environment.GetAvailableBrowserVersionString()
                string ver = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return !string.IsNullOrWhiteSpace(ver);
            }
            catch
            {
                return false;
            }
        }
    }
}
