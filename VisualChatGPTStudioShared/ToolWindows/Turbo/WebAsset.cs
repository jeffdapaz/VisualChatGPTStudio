using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;

public static class WebAsset
{
    private static readonly string _root;

    static WebAsset()
    {
        _root = Path.Combine(
                     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     Utils.Constants.EXTENSION_NAME, "WebAssets");
        EnsureAssets();
    }

    public static string GetTurboPath() => $"file:///{Path.Combine(_root, "TurboChat.html").Replace('\\', '/')}";

    public static bool IsDarkTheme { get; private set; }

    private static void Deploy(string packPath)
    {
        var fileName = Path.GetFileName(packPath);
        var target = Path.Combine(_root, fileName);

        // 1. pack-uri
        var asmName = Assembly.GetExecutingAssembly().GetName().Name;
        var uri = new Uri($"pack://application:,,,/{asmName};component/{packPath}");

        // read
        var info = Application.GetResourceStream(uri)
                   ?? throw new FileNotFoundException($"Resource {uri} not found");
        string content;
        using (info.Stream)
        using (var r = new StreamReader(info.Stream))
            content = r.ReadToEnd();

        File.WriteAllText(target, content, Encoding.UTF8);
    }

    public static void DeployTheme()
    {
        try
        {
            IsDarkTheme = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey).GetBrightness() > 0.9; // white text in dark bg
            var userBubbleColor = IsDarkTheme ? "#0b8060" : "#acc0e5";
            var css = @$"
            :root {{
                --text-color: {VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey).ToCss()};
                --bg-color: {VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey).ToCss()};
                --user-bg-color: {userBubbleColor};
                --gpt-bg-color: {VSColorTheme.GetThemedColor(CommonDocumentColors.CaptionColorKey).ToCss()};
                --code-bg-color: {VSColorTheme.GetThemedColor(EnvironmentColors.DesignerBackgroundColorKey).ToCss()};
                --code-text-color: {VSColorTheme.GetThemedColor(CommonDocumentColors.CaptionTextColorKey).ToCss()};
                --code-header-color: {VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderBrushKey).ToCss()};
                --code-border-color: {VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderBrushKey).ToCss()};
                --highlight-bg-color: {VSColorTheme.GetThemedColor(EnvironmentColors.StatusBarHighlightColorKey).ToCss()};
                --highlight-color: {VSColorTheme.GetThemedColor(EnvironmentColors.StatusBarHighlightTextColorKey).ToCss()};
            }}

            a {{
                color: {VSColorTheme.GetThemedColor(EnvironmentColors.ControlLinkTextBrushKey).ToCss()};
            }}
        ";
            File.WriteAllText(Path.Combine(_root, "theme.css"), css);
        }
        catch (Exception e)
        {
            Logger.Log(e);
            MessageBox.Show("Deploy theme error: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Converts a <see cref="System.Drawing.Color"/> to its CSS hexadecimal color string representation (e.g., "#RRGGBB").
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>
    /// A string representing the color in CSS hexadecimal format.
    /// </returns>
    private static string ToCss(this System.Drawing.Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static void EnsureAssets()
    {
        Directory.CreateDirectory(_root);

        Deploy("WebAssets/TurboChat.html");
        Deploy("WebAssets/TurboChat.css");
        Deploy("WebAssets/TurboChat.js");
    }
}
