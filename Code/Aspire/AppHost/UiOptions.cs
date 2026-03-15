using Microsoft.Extensions.Configuration;

public sealed class UiOptions
{
    public const string SectionName = "Ui";

    public UiPortOptions Ports { get; set; } = new();
    public UiUrlOptions Urls { get; set; } = new();

    public static UiOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new UiOptions();
        options.Ports.Dev = GetInt(configuration, $"{SectionName}:Ports:Dev", options.Ports.Dev);
        options.Urls.Dev = GetString(configuration, $"{SectionName}:Urls:Dev", options.Urls.Dev);
        return options;
    }

    public string GetUrl(string environmentName)
    {
        _ = environmentName;
        return Urls.Dev;
    }

    public int GetPort(string environmentName)
    {
        _ = environmentName;

        var url = GetUrl(environmentName);
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Port > 0)
        {
            return uri.Port;
        }

        return Ports.Dev;
    }

    private static int GetInt(IConfiguration configuration, string key, int defaultValue)
    {
        return int.TryParse(configuration[key], out var value) ? value : defaultValue;
    }

    private static string GetString(IConfiguration configuration, string key, string defaultValue)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}

public sealed class UiPortOptions
{
    public int Dev { get; set; } = 5173;
}

public sealed class UiUrlOptions
{
    public string Dev { get; set; } = "http://localhost:5173";
}
