using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

public class ClarityService : IClarityService
{
    private readonly HttpClient _httpClient;
    private readonly HuSignalSettings _settings;

    public ClarityService(
        HttpClient httpClient,
        IOptions<HuSignalSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<IReadOnlyList<ClarityMetricResponse>> GetDailyInsightsAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.MsClarityToken))
        {
            throw new InvalidOperationException(
                "HuSignal:MsClarityToken has not been configured."
            );
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://www.clarity.ms/export-data/api/v1/project-live-insights?numOfDays=1"
        );

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _settings.MsClarityToken
            );

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        var result =
            JsonSerializer.Deserialize<List<ClarityMetricResponse>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

        return result ?? [];
    }
}