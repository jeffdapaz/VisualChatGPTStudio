using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;

public static class WebAsset
{
    private static readonly string _localAppData = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Utils.Constants.EXTENSION_NAME);

    private static readonly string _root;

    static WebAsset()
    {
        _root = Path.Combine(
                     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     Utils.Constants.EXTENSION_NAME, "WebAssets");
        EnsureAssets();
    }

    public static string GetTurboPath() => $"file:///{Path.Combine(_root, "TurboChat.html").Replace('\\', '/')}";

    public static string Deploy(string packPath)
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

        return $"file:///{target.Replace('\\', '/')}";
    }

    public static void DeployTheme(string text, string back, string code, string gpt)
    {
        var css = @$":root {{
          --bg-color: {back};
          --text-color: {text};
          --code-bg-color: {code};
          --gpt-bg-color: {gpt};
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

    private static void Extract(string resourceId)
    {
        var fileName = Path.Combine(_localAppData, resourceId.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);

        Uri packUri = new($"pack://application:,,,/VisualChatGPTStudio;component/{resourceId}", UriKind.RelativeOrAbsolute);
        var info = Application.GetResourceStream(packUri);
        if (info == null)
        {
            Logger.Log($"Resource not found: {packUri}");
            return;
        }

        using (info.Stream)
        using (var fs = File.Create(fileName))
        {
            info.Stream.CopyTo(fs);
        }
    }
}
