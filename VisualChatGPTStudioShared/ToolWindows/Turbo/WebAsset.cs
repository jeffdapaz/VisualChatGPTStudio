using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

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

    public static void DeployTheme(string text, string textBg, string gptBg,
        string codeBg, string codeText, string codeHeader, string codeBorder, string highlight)
    {
        var css = @$":root {{
          --text-color: {text};
          --bg-color: {textBg};
          --gpt-bg-color: {gptBg};
          --code-bg-color: {codeBg};
          --code-text-color: {codeText}
          --code-header-color: {codeHeader};
          --code-border-color: {codeBorder};
          --highlight-color: {highlight};
        }}";
        File.WriteAllText(Path.Combine(_root, "theme.css"), css);
    }

    private static void EnsureAssets()
    {
        Directory.CreateDirectory(_root);

        Deploy("WebAssets/TurboChat.html");
        Deploy("WebAssets/TurboChat.css");
        Deploy("WebAssets/TurboChat.js");
    }
}
