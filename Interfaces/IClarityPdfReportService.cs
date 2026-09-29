/// <summary>
/// Defines the service responsible for converting a structured
/// Hu Signal Clarity report into a PDF document.
/// </summary>
/// <remarks>
/// This service is responsible only for PDF generation and presentation.
/// It does not retrieve Microsoft Clarity data or generate AI report content.
///
/// The supplied <see cref="GenerateClarityReportResponse"/> is expected to
/// contain the completed structured report and resolved reporting period.
/// The implementation uses this information to build the formatted PDF
/// document returned to the caller.
/// </remarks>
public interface IClarityPdfReportService
{
    /// <summary>
    /// Generates a PDF document from a completed structured
    /// Clarity AI report.
    /// </summary>
    /// <param name="response">
    /// The completed report response containing the reporting period,
    /// structured AI-generated report content, recommendations,
    /// and report limitations to include in the PDF.
    /// </param>
    /// <returns>
    /// A byte array containing the generated PDF document.
    /// The returned bytes can be written directly to an HTTP response
    /// using the <c>application/pdf</c> content type.
    /// </returns>
    /// <remarks>
    /// This operation performs PDF rendering only. The report content
    /// should already have been generated and validated before this
    /// method is called.
    ///
    /// The implementation is responsible for applying the Hu Signal
    /// document layout, typography, section formatting, recommendation
    /// presentation, pagination, and other PDF-specific formatting.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// May be thrown when the supplied report cannot be rendered into
    /// a valid PDF document.
    /// </exception>
    byte[] GeneratePdf(
        GenerateClarityReportResponse response);
}