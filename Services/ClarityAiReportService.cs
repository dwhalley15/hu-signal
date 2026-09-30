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

    /// <summary>
    /// Generates a structured AI-assisted Clarity report for the
    /// requested reporting period.
    ///
    /// AI responses are validated before being returned. If validation
    /// fails, the existing AI conversation is asked to repair its previous
    /// response using the same conversation identifier.
    /// </summary>
    public async Task<GenerateClarityReportResponse> GenerateReportAsync(
        GenerateClarityReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var (from, to) =
            GetPeriod(request);

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

        var conversationId =
            Guid.NewGuid();

        const int maxRepairAttempts = 2;

        ClarityAiReport? report = null;
        string? lastValidationError = null;

        // The first request creates the conversation and supplies
        // the system instructions plus the source Clarity data.
        IReadOnlyList<ChatMessage> messages =
            new List<ChatMessage>
            {
            new()
            {
                Role = "system",
                Content = BuildSystemPrompt()
            },
            new()
            {
                Role = "user",
                Content = BuildPrompt(
                    request.PeriodType,
                    summary)
            }
            };

        for (
            var attempt = 0;
            attempt <= maxRepairAttempts;
            attempt++)
        {
            var content =
                await SendAiRequestAsync(
                    conversationId,
                    messages,
                    cancellationToken);

            var validation =
                ValidateReportJson(
                    content,
                    summary);

            if (validation.IsValid)
            {
                report =
                    validation.Report;

                break;
            }

            lastValidationError =
                validation.Error;

            if (attempt == maxRepairAttempts)
            {
                break;
            }

            /*
             * The Unified LLM API persists conversation history using
             * ConversationId.
             *
             * Therefore, repair requests must send ONLY the new user
             * instruction. Re-sending the existing system/user/assistant
             * messages would duplicate conversation history and break
             * llama.cpp's required user/assistant role ordering.
             */
            messages =
                new List<ChatMessage>
                {
                new()
                {
                    Role = "user",
                    Content = BuildJsonRepairPrompt(
                        validation.Error)
                }
                };
        }

        if (report is null)
        {
            throw new InvalidOperationException(
                "The AI failed to produce a valid Hu Signal report. " +
                $"Last validation error: {lastValidationError}");
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

            Your job is to interpret Microsoft Clarity behavioural analytics
            data and produce a clear, evidence-based report for people who may
            not have technical SEO or analytics knowledge.

            General rules:

            - Only make claims supported by the supplied data.
            - Do not invent metrics, causes, rankings, conversions, search queries,
            traffic sources, user intent, business outcomes or behavioural causes.
            - Clearly distinguish observations from recommendations.
            - Explain technical metrics in plain English.
            - Prefer cautious language when the available evidence is limited.
            - Do not present assumptions as facts.

            Reporting period and data availability:

            - The reporting period and the number of days with available data are
            different concepts.
            - Never describe the reporting period as lasting only the number of
            DaysWithData.
            - For example, if the reporting period is 1 September to 30 September
            but DaysWithData is 5, say:
            "This report covers September 2026 and is based on 5 days of
            available Microsoft Clarity data."
            - Explicitly mention limited data coverage when it affects confidence
            in the findings.

            Sample size and confidence:

            - Treat small sample sizes cautiously.
            - Do not describe a pattern as significant, strong, clear, dominant
            or conclusive when it is based on a small number of sessions or
            observations.
            - Use wording such as "may indicate", "suggests", "is worth
            investigating" or "early data shows" where appropriate.
            - Do not describe limited data as representative of normal website
            behaviour unless the supplied data supports that conclusion.

            Microsoft Clarity limitations:

            - Microsoft Clarity behavioural data does not by itself prove Google
            rankings, organic search performance, conversions, revenue,
            page speed, Core Web Vitals, keyword performance or search intent.
            - Do not claim that behavioural signals caused changes in SEO
            performance.
            - Do not infer page performance or technical performance unless the
            relevant metrics are explicitly supplied.
            - Do not infer user intent unless it is directly supported by the data.

            Dead clicks:

            - A dead click means a click or tap did not appear to result in a
            meaningful response.
            - Do not automatically describe dead clicks as broken links,
            incorrect URLs, missing pages, 404 errors or navigation failures.
            - Do not state a specific cause unless the supplied data proves it.
            - Recommend investigating the affected pages or interactive elements
            to determine the actual cause.

            Quickbacks:

            - A quickback means a user returned quickly after navigating.
            - Do not automatically claim that the content was poor, irrelevant,
            confusing or slow.
            - Do not state that users were frustrated or unable to find what they
            needed unless the supplied data proves it.
            - Recommend investigating the affected pages to understand the cause.

            Rage clicks, excessive scrolling and errors:

            - Treat rage clicks, excessive scrolling, error clicks and script
            errors as behavioural or technical signals that may warrant
            investigation.
            - Do not invent a cause that is not present in the supplied data.

            Referrals and search traffic:

            - Google appearing as a referrer means traffic arrived from Google.
            - Do not automatically describe Google referrals as organic search.
            - Do not claim that Google referrals prove improved or declining SEO
            performance.
            - Direct traffic must not be described as organic traffic.
            - Internal or QA-domain referrals should be described as internal or
            environment-related traffic where appropriate.

            Audience and technology:

            - Do not infer a user's language from their country.
            - Do not recommend localisation or translation based only on
            country-level traffic.
            - Do not describe users as technically skilled, tech-savvy,
            professional or part of a target market based only on browser,
            operating system, device or country.
            - Do not infer market demand from a small number of country visits.
            - Technology breakdowns should normally be presented as compatibility
            and usage observations rather than SEO conclusions.

            Popular content:

            - Identify which pages and page titles received the most visits.
            - Do not describe a page as highly successful, high converting or
            strategically important unless the supplied data supports that.
            - A small number of visits may indicate an area worth monitoring,
            but should not automatically be described as strong demand.
            - Recommendations to expand or promote content should be cautious
            when the sample size is small.

            Recommendations:

            - Recommendations must be based on the supplied data.
            - Prefer investigation or testing when the data is insufficient to
            support a stronger action.
            - Priorities must reflect the evidence available.
            - Do not use "High Priority" solely because a raw count appears large;
            consider the number of sessions and DaysWithData as context.
            - Do not include priority labels inside the recommendation description.
            - Do not include "Requires Further Investigation: Yes", "No", or
            similar metadata inside the recommendation description.
            - Use only the dedicated "priority" and
            "requiresFurtherInvestigation" JSON properties for that metadata.
            - Recommendation descriptions should contain only the explanation and
            suggested action.

            Writing style:

            - Use clear UK English.
            - Be concise but useful.
            - Avoid unnecessary jargon.
            - Avoid dramatic or alarmist wording.
            - Explain findings so they can be understood by a non-technical reader.
            - Do not use Markdown anywhere in string values.
            - URLs must be returned as plain text.
            - Do not create Markdown links.

            Output requirements:

            - Return JSON only.
            - Do not return Markdown.
            - Do not wrap the JSON in code fences.
            - Do not include commentary before or after the JSON.
            - Do not include trailing commas in JSON objects or arrays.
            - Use valid JSON syntax.
            - Every property below must be present.
            - The response must match this structure exactly:

            Facts requirements:

            - The "facts" object must copy the supplied numerical data exactly.
            - Do not estimate, reinterpret or recalculate the supplied values.
            - directReferrals means the ReferrerUrl breakdown whose Name and Url are both null.
            - googleReferrals means the ReferrerUrl breakdown for https://www.google.com/.
            - The narrative sections must agree with the values in "facts".

            {
                "title": "string",
                "facts": {
                    "daysWithData": 0,
                    "totalSessions": 0,
                    "botSessions": 0,
                    "averagePagesPerSession": 0,
                    "averageScrollDepth": 0,
                    "deadClicks": 0,
                    "rageClicks": 0,
                    "quickbacks": 0,
                    "scriptErrors": 0,
                    "errorClicks": 0,
                    "excessiveScrolls": 0,
                    "directReferrals": 0,
                    "googleReferrals": 0
                },
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
            Generate a structured {periodType} website behaviour and SEO insights report
            using only the Microsoft Clarity data supplied below.

            Reporting period:
            {summary.From:yyyy-MM-dd} to {summary.To:yyyy-MM-dd}

            Days with available data:
            {summary.DaysWithData}

            Important reporting context:

            - The reporting period is the full date range shown above.
            - DaysWithData is only the number of days within that period for which
            Hu Signal currently has saved Clarity data.
            - Do not describe the reporting period as a
            "{summary.DaysWithData}-day period".
            - Where appropriate, state that the report covers the full reporting
            period but is based on only {summary.DaysWithData} days of available
            data.

            Microsoft Clarity data:

            {dataJson}

            Before writing the narrative, populate the "facts" object by copying
            the supplied metrics exactly.

            Do not modify, estimate, round to different whole numbers, or reinterpret
            the supplied counts.

            For referral facts:

            - directReferrals is the count from the ReferrerUrl breakdown where both
            Name and Url are null.
            - googleReferrals is the count for the Google referrer
            https://www.google.com/.

            All narrative sections must remain consistent with the "facts" object.

            Populate every field in the required JSON response.

            title:

            Create a concise report title that identifies the website behaviour
            and SEO insights report and the relevant month or year.

            Do not include Markdown formatting.

            executiveSummary:

            Summarise the most important findings in plain English.

            Include appropriate context about:
            - total sessions
            - engagement
            - notable behavioural signals
            - popular content
            - data limitations

            Do not overstate conclusions.

            If the number of sessions or DaysWithData is small, make that limitation
            clear in the executive summary.

            trafficAndEngagement:

            Explain:
            - total sessions
            - average pages per session
            - average scroll depth
            - bot sessions where relevant

            Explain what these metrics represent in plain English.

            Do not state that a particular scroll depth or pages-per-session value
            automatically proves good or poor engagement.

            Describe the numbers and, where appropriate, identify them as areas to
            monitor rather than assigning unsupported performance labels.

            userBehaviourIssues:

            Analyse the available behavioural signals:
            - dead clicks
            - quickbacks
            - rage clicks
            - excessive scrolling
            - error clicks
            - script errors

            Dead clicks must be described as clicks or taps that did not appear to
            result in a meaningful response.

            Do not automatically describe dead clicks as broken links, incorrect
            URLs, missing pages or 404 errors.

            Quickbacks must be described as users returning quickly after navigating.

            Do not claim that quickbacks prove poor content, confusing navigation,
            slow loading or another cause.

            Clearly identify which behaviours warrant further investigation and
            which signals were not observed.

            popularContent:

            Discuss the most visited pages and page titles.

            Include useful visit counts where available.

            URLs must be plain text.

            Do not use Markdown links.

            Do not claim that a popular page proves:
            - search demand
            - conversions
            - user intent
            - commercial success
            - SEO performance

            If a page appears relatively popular within the supplied data, describe
            it as something worth monitoring or investigating.

            audienceAndTechnology:

            Summarise useful patterns across:
            - countries
            - devices
            - browsers
            - operating systems

            Do not infer language, market demand, technical sophistication or user
            intent from these values.

            Do not recommend localisation based only on country traffic.

            Technology observations should mainly help identify compatibility or
            testing considerations.

            referrals:

            Summarise the available referral information.

            Distinguish where possible between:
            - Google referrals
            - direct traffic
            - internal website referrals
            - QA or development environment referrals
            - other external referrals

            Do not describe Google referrals as organic search unless the data
            explicitly identifies them as organic.

            Do not claim referral counts prove SEO improvement or decline.

            recommendations:

            Return practical recommendations ordered by priority.

            Use only:
            - High
            - Medium
            - Low

            Each recommendation must contain:
            - priority
            - title
            - description
            - requiresFurtherInvestigation

            The description must contain only:
            - why the action is worth considering
            - what action should be taken

            Do not repeat:
            - the priority inside the description
            - "Requires Further Investigation"
            - Yes or No metadata
            - any JSON property names

            Set requiresFurtherInvestigation to true when the supplied Clarity data
            identifies a signal but does not establish its cause.

            Prefer investigation-focused recommendations when the sample size or
            data coverage is limited.

            Avoid recommendations that require evidence not supplied by Clarity.

            limitations:

            Clearly explain important limitations of the report.

            Consider:
            - the number of sessions
            - DaysWithData
            - whether the reporting period contains incomplete data
            - the inability of Microsoft Clarity alone to establish rankings,
            organic search performance, conversions, search queries or causes
            of behavioural signals

            Do not use Markdown formatting anywhere in the response.

            Return valid JSON only.
            """;
    }

    /// <summary>
    /// Removes common Markdown code fences that some models may add
    /// around otherwise valid JSON responses.
    /// </summary>
    private static string CleanJsonResponse(
        string content)
    {
        content =
            content.Trim();

        if (content.StartsWith(
                "```json",
                StringComparison.OrdinalIgnoreCase))
        {
            content =
                content[7..];
        }
        else if (content.StartsWith("```"))
        {
            content =
                content[3..];
        }

        if (content.EndsWith("```"))
        {
            content =
                content[..^3];
        }

        return content.Trim();
    }

    /// <summary>
    /// Sends the current conversation to the configured AI endpoint
    /// and returns the assistant's raw response content.
    /// </summary>
    private async Task<string> SendAiRequestAsync(
        Guid conversationId,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var aiRequest =
            new ChatCompletionRequest
            {
                Model = _options.Model,
                Stream = false,
                Think = false,
                ConversationId = conversationId,
                Messages = messages.ToList(),
                Options = new ChatOptions
                {
                    ContextSize = _options.ContextSize,
                    Temperature = _options.Temperature,
                    TopP = _options.TopP
                }
            };

        var json =
            JsonSerializer.Serialize(
                aiRequest);

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                _options.BaseUrl);

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _options.ApiKey);

        var contentBytes =
            Encoding.UTF8.GetBytes(
                json);

        httpRequest.Content =
            new ByteArrayContent(
                contentBytes);

        httpRequest.Content.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/json");

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

        using var parsed =
            JsonDocument.Parse(body);

        var content =
            parsed.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "The AI returned an empty response.");
        }

        return content;
    }

    /// <summary>
    /// Represents the result of validating an AI-generated
    /// Clarity report response.
    /// </summary>
    private sealed class ReportValidationResult
    {
        public bool IsValid { get; init; }

        public ClarityAiReport? Report { get; init; }

        public string Error { get; init; } =
            string.Empty;
    }

    /// <summary>
    /// Validates the raw AI response as JSON and verifies that it
    /// can be converted into a complete Clarity AI report.
    /// </summary>
    private static ReportValidationResult ValidateReportJson(
        string content, ClarityPeriodSummary summary)
    {
        var cleanedContent =
            CleanJsonResponse(content);

        ClarityAiReport? report;

        try
        {
            report =
                JsonSerializer.Deserialize<ClarityAiReport>(
                    cleanedContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });
        }
        catch (JsonException ex)
        {
            return new ReportValidationResult
            {
                IsValid = false,
                Error =
                    "The response is not valid JSON. " +
                    ex.Message
            };
        }

        if (report is null)
        {
            return new ReportValidationResult
            {
                IsValid = false,
                Error =
                    "The JSON response could not be converted " +
                    "into a Clarity report."
            };
        }

        var errors =
            ValidateReportStructure(report);

        errors.AddRange(
            ValidateReportFacts(
                report,
                summary));

        if (errors.Count > 0)
        {
            return new ReportValidationResult
            {
                IsValid = false,
                Error =
                    string.Join(
                        " ",
                        errors)
            };
        }

        return new ReportValidationResult
        {
            IsValid = true,
            Report = report
        };
    }

    /// <summary>
    /// Verifies that the factual metrics returned by the AI exactly match
    /// the source Microsoft Clarity summary used to generate the report.
    /// </summary>
    private static List<string> ValidateReportFacts(
        ClarityAiReport report,
        ClarityPeriodSummary summary)
    {
        var errors =
            new List<string>();

        var facts =
            report.Facts;

        if (facts is null)
        {
            errors.Add(
                "The facts object is missing.");

            return errors;
        }

        if (facts.DaysWithData != summary.DaysWithData)
        {
            errors.Add(
                $"daysWithData is incorrect. " +
                $"Expected {summary.DaysWithData}, " +
                $"received {facts.DaysWithData}.");
        }

        if (facts.TotalSessions != summary.TotalSessions)
        {
            errors.Add(
                $"totalSessions is incorrect. " +
                $"Expected {summary.TotalSessions}, " +
                $"received {facts.TotalSessions}.");
        }

        if (facts.BotSessions != summary.BotSessions)
        {
            errors.Add(
                $"botSessions is incorrect. " +
                $"Expected {summary.BotSessions}, " +
                $"received {facts.BotSessions}.");
        }

        if (!ApproximatelyEqual(
                facts.AveragePagesPerSession,
                summary.AveragePagesPerSession))
        {
            errors.Add(
                $"averagePagesPerSession is incorrect. " +
                $"Expected {summary.AveragePagesPerSession:F2}, " +
                $"received {facts.AveragePagesPerSession:F2}.");
        }

        if (!ApproximatelyEqual(
                facts.AverageScrollDepth,
                summary.AverageScrollDepth))
        {
            errors.Add(
                $"averageScrollDepth is incorrect. " +
                $"Expected {summary.AverageScrollDepth:F2}, " +
                $"received {facts.AverageScrollDepth:F2}.");
        }

        if (facts.DeadClicks != summary.DeadClicks)
        {
            errors.Add(
                $"deadClicks is incorrect. " +
                $"Expected {summary.DeadClicks}, " +
                $"received {facts.DeadClicks}.");
        }

        if (facts.RageClicks != summary.RageClicks)
        {
            errors.Add(
                $"rageClicks is incorrect. " +
                $"Expected {summary.RageClicks}, " +
                $"received {facts.RageClicks}.");
        }

        if (facts.Quickbacks != summary.Quickbacks)
        {
            errors.Add(
                $"quickbacks is incorrect. " +
                $"Expected {summary.Quickbacks}, " +
                $"received {facts.Quickbacks}.");
        }

        if (facts.ScriptErrors != summary.ScriptErrors)
        {
            errors.Add(
                $"scriptErrors is incorrect. " +
                $"Expected {summary.ScriptErrors}, " +
                $"received {facts.ScriptErrors}.");
        }

        if (facts.ErrorClicks != summary.ErrorClicks)
        {
            errors.Add(
                $"errorClicks is incorrect. " +
                $"Expected {summary.ErrorClicks}, " +
                $"received {facts.ErrorClicks}.");
        }

        if (facts.ExcessiveScrolls != summary.ExcessiveScrolls)
        {
            errors.Add(
                $"excessiveScrolls is incorrect. " +
                $"Expected {summary.ExcessiveScrolls}, " +
                $"received {facts.ExcessiveScrolls}.");
        }

        var expectedDirectReferrals =
            GetReferrerCount(
                summary,
                name: null,
                url: null);

        var expectedGoogleReferrals =
            GetReferrerCount(
                summary,
                name: "https://www.google.com/",
                url: null);

        if (
            facts.DirectReferrals !=
            expectedDirectReferrals)
        {
            errors.Add(
                $"directReferrals is incorrect. " +
                $"Expected {expectedDirectReferrals}, " +
                $"received {facts.DirectReferrals}.");
        }

        if (
            facts.GoogleReferrals !=
            expectedGoogleReferrals)
        {
            errors.Add(
                $"googleReferrals is incorrect. " +
                $"Expected {expectedGoogleReferrals}, " +
                $"received {facts.GoogleReferrals}.");
        }

        return errors;
    }

    /// <summary>
    /// Returns the count for a specific ReferrerUrl breakdown record.
    /// </summary>
    private static int GetReferrerCount(
        ClarityPeriodSummary summary,
        string? name,
        string? url)
    {
        var breakdown =
            summary.Breakdowns.FirstOrDefault(
                item =>
                    item.MetricName.Equals(
                        "ReferrerUrl",
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    string.Equals(
                        item.Name,
                        name,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    string.Equals(
                        item.Url,
                        url,
                        StringComparison.OrdinalIgnoreCase));

        return breakdown?.Count ?? 0;
    }

    /// <summary>
    /// Compares decimal metrics using a small tolerance so harmless
    /// rounding differences do not fail report validation.
    /// </summary>
    private static bool ApproximatelyEqual(
        decimal first,
        decimal second,
        decimal tolerance = 0.01m)
    {
        return Math.Abs(
            first - second) <= tolerance;
    }

    /// <summary>
    /// Checks that a deserialized report contains all content required
    /// by the Hu Signal PDF report.
    /// </summary>
    private static List<string> ValidateReportStructure(
        ClarityAiReport report)
    {
        var errors =
            new List<string>();

        if (string.IsNullOrWhiteSpace(
                report.Title))
        {
            errors.Add(
                "The title field is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(
                report.ExecutiveSummary))
        {
            errors.Add(
                "The executiveSummary field is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(
                report.TrafficAndEngagement))
        {
            errors.Add(
                "The trafficAndEngagement field is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(
                report.UserBehaviourIssues))
        {
            errors.Add(
                "The userBehaviourIssues field is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(
                report.PopularContent))
        {
            errors.Add(
                "The popularContent field is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(
                report.AudienceAndTechnology))
        {
            errors.Add(
                "The audienceAndTechnology field is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(
                report.Referrals))
        {
            errors.Add(
                "The referrals field is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(
                report.Limitations))
        {
            errors.Add(
                "The limitations field is missing or empty.");
        }

        if (report.Recommendations is null)
        {
            errors.Add(
                "The recommendations array is missing.");

            return errors;
        }

        for (
            var index = 0;
            index < report.Recommendations.Count;
            index++)
        {
            var recommendation =
                report.Recommendations[index];

            if (string.IsNullOrWhiteSpace(
                    recommendation.Title))
            {
                errors.Add(
                    $"Recommendation {index + 1} " +
                    "is missing a title.");
            }

            if (string.IsNullOrWhiteSpace(
                    recommendation.Description))
            {
                errors.Add(
                    $"Recommendation {index + 1} " +
                    "is missing a description.");
            }

            if (string.IsNullOrWhiteSpace(
                    recommendation.Priority))
            {
                errors.Add(
                    $"Recommendation {index + 1} " +
                    "is missing a priority.");

                continue;
            }

            var validPriority =
                recommendation.Priority.Equals(
                    "High",
                    StringComparison.OrdinalIgnoreCase)
                ||
                recommendation.Priority.Equals(
                    "Medium",
                    StringComparison.OrdinalIgnoreCase)
                ||
                recommendation.Priority.Equals(
                    "Low",
                    StringComparison.OrdinalIgnoreCase);

            if (!validPriority)
            {
                errors.Add(
                    $"Recommendation {index + 1} has invalid " +
                    $"priority '{recommendation.Priority}'. " +
                    "Priority must be High, Medium or Low.");
            }
        }

        return errors;
    }

    /// <summary>
    /// Builds the corrective prompt sent to the same AI conversation
    /// when the previous report fails JSON, structural or factual validation.
    /// </summary>
    /// <summary>
    /// Builds the corrective prompt sent to the same AI conversation
    /// when the previous report fails JSON, structural or factual validation.
    /// </summary>
    private static string BuildJsonRepairPrompt(
        string validationError)
    {
        return $"""
        Your previous response failed Hu Signal validation and cannot be used.

        Validation errors:

        {validationError}

        Correct your previous response and return the complete report again.

        Requirements:

        - Return the COMPLETE corrected JSON object.
        - Do not return only the corrected fields.
        - Preserve valid content from the previous response where possible.
        - Correct every issue listed in the validation errors.
        - Any expected factual values shown in the validation errors must be used exactly.
        - Ensure the narrative agrees with the corrected facts.
        - Do not invent or substitute different metric values.
        - Do not explain the error.
        - Do not apologise.
        - Do not include Markdown.
        - Do not use code fences.
        - Do not include text before or after the JSON.
        - Ensure every required property is present.
        - Ensure all strings have closing quotation marks.
        - Ensure all arrays and objects are properly closed.
        - Do not include trailing commas.
        - Keep narrative fields concise enough to avoid unnecessary output.
        - Return valid JSON only.
        """;
    }
}