/// <summary>
/// Defines the service responsible for generating an AI-assisted
/// Microsoft Clarity report for a selected monthly or yearly period.
/// </summary>
/// <remarks>
/// The implementation is expected to:
/// <list type="bullet">
/// <item>
/// <description>
/// Retrieve the aggregated Microsoft Clarity data for the requested period.
/// </description>
/// </item>
/// <item>
/// <description>
/// Build the AI prompt using the reporting data and configured reporting rules.
/// </description>
/// </item>
/// <item>
/// <description>
/// Send the request to the configured AI/LLM endpoint.
/// </description>
/// </item>
/// <item>
/// <description>
/// Validate and deserialize the structured AI response into a
/// <see cref="ClarityAiReport"/>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Return the completed report together with the resolved reporting period.
/// </description>
/// </item>
/// </list>
/// </remarks>
public interface IClarityAiReportService
{
    /// <summary>
    /// Generates a structured AI-assisted Clarity report for the
    /// reporting period described by the supplied request.
    /// </summary>
    /// <param name="request">
    /// The report request containing the reporting period type,
    /// year, and optional month.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the report generation operation,
    /// including data retrieval and the outbound AI request.
    /// </param>
    /// <returns>
    /// A task that resolves to a <see cref="GenerateClarityReportResponse"/>
    /// containing the resolved reporting dates and structured AI report.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the requested period type is invalid or when a monthly
    /// report is requested without a valid month.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no Clarity data is available for the requested period,
    /// or when the AI response cannot be converted into a valid report.
    /// </exception>
    Task<GenerateClarityReportResponse> GenerateReportAsync(
        GenerateClarityReportRequest request,
        CancellationToken cancellationToken = default);
}