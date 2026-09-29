using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

public class ClarityAiReportService : IClarityAiReportService
{
    private readonly HttpClient _httpClient;
    private readonly IClarityReportingService _clarityReportingService;
    private readonly AiGenerationOptions _options;

    public ClarityAiReportService(
        HttpClient httpClient,
        IClarityReportingService clarityReportingService,
        IOptions<AiGenerationOptions> options)
    {
        _httpClient = httpClient;
        _clarityReportingService = clarityReportingService;
        _options = options.Value;
    }

    public async Task<GenerateClarityReportResponse> GenerateReportAsync(
        GenerateClarityReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = GetPeriod(request);

        var summary =
            await _clarityReportingService.GetSummaryAsync(
                from,
                to,
                cancellationToken);

        if (summary.DaysWithData == 0)
        {
            throw new InvalidOperationException(
                "There is no Clarity data available for the selected period.");
        }

        var prompt = BuildPrompt(
            request.PeriodType,
            summary);

        var messages = new List<ChatMessage>
        {
            new()
            {
                Role = "system",
                Content = BuildSystemPrompt()
            },
            new()
            {
                Role = "user",
                Content = prompt
            }
        };

        var aiRequest = new ChatCompletionRequest
        {
            Model = _options.Model,
            Stream = false,
            Think = false,
            ConversationId = Guid.NewGuid(),
            Messages = messages,
            Options = new ChatOptions
            {
                ContextSize = _options.ContextSize,
                Temperature = _options.Temperature,
                TopP = _options.TopP
            }
        };

        var json = JsonSerializer.Serialize(aiRequest);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            _options.BaseUrl);

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _options.ApiKey);

        var contentBytes = Encoding.UTF8.GetBytes(json);

        httpRequest.Content = new ByteArrayContent(contentBytes);

        httpRequest.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/json");

        using var timeoutCts =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(
                    _options.TimeoutSeconds));

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

        using var response =
            await _httpClient.SendAsync(
                httpRequest,
                linkedCts.Token);

        response.EnsureSuccessStatusCode();

        var body =
            await response.Content.ReadAsStringAsync(
                linkedCts.Token);

        using var parsed = JsonDocument.Parse(body);

        var content =
            parsed.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
            ?? string.Empty;

        content = CleanJsonResponse(content);

        ClarityAiReport? report;

        try
        {
            report =
                JsonSerializer.Deserialize<ClarityAiReport>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "The AI returned malformed JSON.",
                ex);
        }

        if (report is null)
        {
            throw new InvalidOperationException(
                "The AI returned an empty report response.");
        }

        return new GenerateClarityReportResponse
        {
            PeriodType = request.PeriodType,
            From = from,
            To = to,
            Report = report
        };
    }

    private static (DateTime From, DateTime To) GetPeriod(
        GenerateClarityReportRequest request)
    {
        if (request.PeriodType.Equals(
                "monthly",
                StringComparison.OrdinalIgnoreCase))
        {
            if (request.Month is null ||
                request.Month < 1 ||
                request.Month > 12)
            {
                throw new ArgumentException(
                    "A valid month is required for a monthly report.");
            }

            var from =
                new DateTime(
                    request.Year,
                    request.Month.Value,
                    1);

            return (
                from,
                from.AddMonths(1).AddDays(-1)
            );
        }

        if (request.PeriodType.Equals(
                "yearly",
                StringComparison.OrdinalIgnoreCase))
        {
            return (
                new DateTime(request.Year, 1, 1),
                new DateTime(request.Year, 12, 31)
            );
        }

        throw new ArgumentException(
            "PeriodType must be monthly or yearly.");
    }

    private static string BuildSystemPrompt()
    {
        return """
            You are an SEO and digital analytics reporting assistant.

            Your job is to interpret Microsoft Clarity behavioural
            analytics data and write a clear report for people who
            may not have technical SEO or analytics knowledge.

            Rules:

            - Only make claims supported by the supplied data.
            - Do not invent traffic sources, rankings, conversions,
            search queries or causes that are not present in the data.
            - Clearly distinguish observations from recommendations.
            - Explain technical metrics in plain English.

            - Treat small sample sizes cautiously.
            - Do not describe a pattern as significant, strong, clear
            or important when it is based on only a small number of
            sessions or observations.
            - Explicitly mention limited sample size where it affects
            the reliability of a conclusion.

            - Do not infer a user's language from their country.
            - Do not recommend localisation or translation based only
            on country-level traffic.

            - Do not infer page performance, page speed, conversion
            performance or search rankings unless those metrics are
            explicitly supplied.

            - A dead click means a click or tap did not appear to result in a meaningful response.
            Do not automatically describe dead clicks as broken links or non-existent pages.

            - A quickback means a user returned quickly after navigating.
            Do not state a specific cause such as poor content, design, speed or relevance
            unless the supplied data proves it.

            - Google appearing as a referrer means traffic arrived from
            Google. Do not automatically describe that traffic as
            organic search unless the supplied data identifies it as
            organic.

            - Identify notable behavioural problems such as rage clicks,
            dead clicks, quickbacks, excessive scrolling and errors.

            - Identify useful patterns in popular pages, referrers,
            devices, browsers, operating systems and countries.

            - Suggest practical SEO, content and usability improvements
            only where the data reasonably supports them.

            - Do not claim that Microsoft Clarity data alone proves
            changes in Google rankings or organic search performance.

            - Prefer recommendations to investigate or test when the
            available data is insufficient to justify a stronger
            recommendation.

            - Be concise but useful.

            Output requirements:

            - Return JSON only.
            - Do not return Markdown.
            - Do not wrap the JSON in code fences.
            - Do not include trailing commas in JSON objects or arrays.
            - Do not include commentary before or after the JSON.
            - The response must match this structure exactly:
            - Do not use Markdown anywhere in string values.
            - URLs must be returned as plain text.

            {
            "title": "string",
            "executiveSummary": "string",
            "trafficAndEngagement": "string",
            "userBehaviourIssues": "string",
            "popularContent": "string",
            "audienceAndTechnology": "string",
            "referrals": "string",
            "recommendations": [
                {
                "priority": "High | Medium | Low",
                "title": "string",
                "description": "string",
                "requiresFurtherInvestigation": true
                }
            ],
            "limitations": "string"
            }
            """;
    }

    private static string BuildPrompt(
    string periodType,
    ClarityPeriodSummary summary)
    {
        var dataJson = JsonSerializer.Serialize(summary);

        return $"""
        Generate a structured {periodType} website behaviour and SEO insights report.

        Reporting period:
        {summary.From:yyyy-MM-dd} to {summary.To:yyyy-MM-dd}

        Days of data available:
        {summary.DaysWithData}

        Microsoft Clarity data:
        {dataJson}

        Populate every field in the required JSON response.

        executiveSummary:
        Explain the most important findings in plain English.

        trafficAndEngagement:
        Explain sessions, pages per session and scroll depth.

        userBehaviourIssues:
        Analyse quickbacks, rage clicks, dead clicks,
        excessive scrolling, error clicks and script errors.

        popularContent:
        Discuss the most visited pages and page titles.

        audienceAndTechnology:
        Summarise useful patterns across countries, devices,
        browsers and operating systems.

        referrals:
        Summarise referral information.
        Do not describe direct traffic as organic search.

        recommendations:
        Return practical recommendations ordered by priority.

        Each recommendation must state whether it requires
        further investigation.

        limitations:
        Explain important limitations such as small sample size,
        limited days of data, or conclusions that cannot be drawn
        from Microsoft Clarity alone.

        Recommendations must be supported by the supplied data.
        """;
    }

    private static string CleanJsonResponse(string content)
    {
        content = content.Trim();

        if (content.StartsWith("```json"))
        {
            content = content[7..];
        }
        else if (content.StartsWith("```"))
        {
            content = content[3..];
        }

        if (content.EndsWith("```"))
        {
            content = content[..^3];
        }

        return content.Trim();
    }
}