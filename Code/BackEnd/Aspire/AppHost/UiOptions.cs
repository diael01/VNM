using Microsoft.Extensions.Configuration;

public sealed class UiOptions
{
	public const string SectionName = "Ui";

	public UiPortOptions Ports { get; set; } = new();

	public static UiOptions FromConfiguration(IConfiguration configuration)
	{
		var options = new UiOptions();
		options.Ports.Dev = GetInt(configuration, $"{SectionName}:Ports:Dev", options.Ports.Dev);
		return options;
	}

	private static int GetInt(IConfiguration configuration, string key, int defaultValue)
	{
		return int.TryParse(configuration[key], out var value) ? value : defaultValue;
	}
}

public sealed class UiPortOptions
{
	public int Dev { get; set; } = 5173;
}
