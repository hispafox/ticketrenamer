using System.Net.Http;
using System.Net.Http.Headers;

namespace TicketRenamer.Wpf.Services;

public sealed class ApiKeyDelegatingHandler : DelegatingHandler
{
    private readonly ISettingsService _settingsService;

    public ApiKeyDelegatingHandler(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = _settingsService.GroqApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";

        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        return base.SendAsync(request, cancellationToken);
    }
}
