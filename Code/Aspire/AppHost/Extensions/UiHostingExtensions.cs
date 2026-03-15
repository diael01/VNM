using Aspire.Hosting;
using System.Diagnostics;
using System.Net.Http;

namespace AppHost.Extensions;

public static class UiHostingExtensions
{
    public static void OpenUiWhenReady(this IDistributedApplicationBuilder builder, string uiUrl, bool autoOpenUi)
    {
        if (!autoOpenUi || !builder.ExecutionContext.IsRunMode)
        {
            return;
        }

        _ = Task.Run(() => OpenUiWhenReadyAsync(uiUrl));
    }

    private static async Task OpenUiWhenReadyAsync(string uiUrl)
    {
        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await http.GetAsync(uiUrl);
                if ((int)response.StatusCode >= 200)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = uiUrl,
                        UseShellExecute = true
                    });
                    return;
                }
            }
            catch
            {
                // UI not reachable yet.
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
