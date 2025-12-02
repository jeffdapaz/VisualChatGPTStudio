using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JeffPires.VisualChatGPTStudio.Utils;

public static class JsonUtils
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize(object value) => JsonSerializer.Serialize(value, _jsonOptions);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOptions);

    public static string PrettyPrintFormat(string minifiedJson)
    {
        using JsonDocument document = JsonDocument.Parse(minifiedJson);
        return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }

    public static IReadOnlyDictionary<string, object>? DeserializeParameters(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public static object? GetValue(this IReadOnlyDictionary<string, object> parameters, string key)
    {
        return parameters.TryGetValue(key, out var value) ? value : null;
    }

    public static string? GetString(this IReadOnlyDictionary<string, object> parameters, string key)
    {
        return parameters.GetValue(key)?.ToString();
    }

    public static bool GetBool(this IReadOnlyDictionary<string, object> parameters, string key, bool defaultValue = false)
    {
        var value = parameters.GetValue(key);
        return value?.ToString()?.ToLowerInvariant() switch
        {
            "true" or "1" or "yes" => true,
            "false" or "0" or "no" => false,
            _ => defaultValue
        };
    }

    public static int GetInt(this IReadOnlyDictionary<string, object> parameters, string key, int defaultValue = 0)
    {
        var value = parameters.GetValue(key);
        if (value == null) return defaultValue;

        if (int.TryParse(value.ToString(), out int result))
            return result;

        return defaultValue;
    }

    public static T? GetObject<T>(this IReadOnlyDictionary<string, object> parameters, string key) where T : class
    {
        var value = parameters.GetValue(key);
        if (value == null) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, _jsonOptions), _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
