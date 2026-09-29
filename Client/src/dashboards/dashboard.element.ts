import {
  LitElement,
  css,
  html,
  customElement,
  state,
} from "@umbraco-cms/backoffice/external/lit";

import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";


/* -------------------------------------------------------------------------- */
/* Types                                                                      */
/* -------------------------------------------------------------------------- */

type ClaritySnapshot = {
  id: number;
  snapshotDate: string;
  retrievedAtUtc: string;
  totalSessions: number;
  distinctUsers: number;
  botSessions: number;
  pagesPerSession: number;
  averageScrollDepth: number;
  engagementTotalTime: number;
  engagementActiveTime: number;
  deadClicks: number;
  rageClicks: number;
  quickbacks: number;
  scriptErrors: number;
  errorClicks: number;
  excessiveScrolls: number;
};

type ClarityBreakdown = {
  id: number;
  snapshotId: number;
  metricName: string;
  name?: string | null;
  url?: string | null;
  sessionsCount?: number | null;
  visitsCount?: number | null;
};

type ClarityLatestResponse = {
  snapshot: ClaritySnapshot;
  breakdowns: ClarityBreakdown[];
};

type ClarityImportResult = {
  snapshotDate: string;
  snapshotId: number;
  sessions: number;
  distinctUsers: number;
  averageScrollDepth: number;
  saved: boolean;
};

type ClarityPeriodBreakdown = {
  metricName: string;
  name?: string | null;
  url?: string | null;
  count: number;
};

type ClarityPeriodSummary = {
  from: string;
  to: string;
  daysWithData: number;
  totalSessions: number;
  botSessions: number;
  averagePagesPerSession: number;
  averageScrollDepth: number;
  deadClicks: number;
  rageClicks: number;
  quickbacks: number;
  scriptErrors: number;
  errorClicks: number;
  excessiveScrolls: number;
  breakdowns: ClarityPeriodBreakdown[];
};

type ViewMode = "latest" | "monthly" | "yearly";


/* -------------------------------------------------------------------------- */
/* Constants                                                                  */
/* -------------------------------------------------------------------------- */

const API_BASE = "/umbraco/husignal/api/v1/clarity";

const MONTHS = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
] as const;


/* -------------------------------------------------------------------------- */
/* Component                                                                  */
/* -------------------------------------------------------------------------- */

@customElement("hu-signal-dashboard")
export class HuSignalDashboardElement extends UmbElementMixin(LitElement) {
  @state()
  private _clarityData?: ClarityLatestResponse;

  @state()
  private _periodData?: ClarityPeriodSummary;

  @state()
  private _viewMode: ViewMode = "latest";

  @state()
  private _loading = false;

  @state()
  private _error?: string;

  @state()
  private _selectedYear = new Date().getFullYear();

  @state()
  private _selectedMonth = new Date().getMonth() + 1;

  @state()
  private _availableYears: number[] = [];

  @state()
  private _generatingReport = false;


  /* ------------------------------------------------------------------------ */
  /* Lifecycle                                                                */
  /* ------------------------------------------------------------------------ */

  /**
   * Loads the initial Clarity snapshot and available reporting years
   * when the dashboard is attached to the page.
   */
  override connectedCallback() {
    super.connectedCallback();

    void this._loadLatestSnapshot();
    void this._loadAvailableYears();
  }


  /* ------------------------------------------------------------------------ */
  /* Authentication and API helpers                                           */
  /* ------------------------------------------------------------------------ */

  /**
   * Retrieves the latest Umbraco backoffice authentication token.
   */
  private async _getAuthToken() {
    const authContext =
      await this.getContext(UMB_AUTH_CONTEXT);

    return await authContext?.getLatestToken();
  }

  /**
   * Sends an authenticated request to the Hu Signal API.
   *
   * Throws a readable error when the API returns a non-success response.
   */
  private async _fetchApi(
    url: string,
    options: RequestInit = {}
  ): Promise<Response> {
    const token = await this._getAuthToken();

    const headers = new Headers(options.headers);

    headers.set(
      "Authorization",
      `Bearer ${token}`
    );

    const response = await fetch(
      url,
      {
        ...options,
        credentials: "include",
        headers,
      }
    );

    if (!response.ok) {
      const text = await response.text();

      throw new Error(
        `Hu Signal API returned ${response.status}: ${text}`
      );
    }

    return response;
  }

  /**
   * Converts an unknown caught value into a readable dashboard error.
   */
  private _setError(error: unknown) {
    this._error =
      error instanceof Error
        ? error.message
        : String(error);
  }


  /* ------------------------------------------------------------------------ */
  /* Data loading                                                             */
  /* ------------------------------------------------------------------------ */

  /**
   * Loads the years for which Hu Signal has stored Clarity data.
   *
   * If the currently selected year is unavailable, the newest
   * available year is selected automatically.
   */
  private async _loadAvailableYears() {
    try {
      const response =
        await this._fetchApi(
          `${API_BASE}/years`,
          {
            method: "GET",
            headers: {
              Accept: "application/json",
            },
          }
        );

      this._availableYears =
        (await response.json()) as number[];

      if (
        this._availableYears.length > 0 &&
        !this._availableYears.includes(
          this._selectedYear
        )
      ) {
        this._selectedYear =
          this._availableYears[0];
      }
    } catch (error) {
      console.error(
        "Failed to load available Clarity years",
        error
      );
    }
  }

  /**
   * Loads the most recently imported Microsoft Clarity snapshot.
   *
   * A 404 response is treated as an empty state rather than an error.
   */
  private async _loadLatestSnapshot() {
    this._loading = true;
    this._error = undefined;

    try {
      const token =
        await this._getAuthToken();

      const response = await fetch(
        `${API_BASE}/latest`,
        {
          method: "GET",
          credentials: "include",
          headers: {
            Authorization: `Bearer ${token}`,
            Accept: "application/json",
          },
        }
      );

      if (response.status === 404) {
        this._clarityData = undefined;
        return;
      }

      if (!response.ok) {
        const text =
          await response.text();

        throw new Error(
          `Hu Signal API returned ${response.status}: ${text}`
        );
      }

      this._clarityData =
        (await response.json()) as ClarityLatestResponse;
    } catch (error) {
      this._setError(error);
    } finally {
      this._loading = false;
    }
  }

  /**
   * Loads the monthly or yearly Clarity summary for the
   * currently selected reporting period.
   */
  private async _loadPeriodData() {
    this._loading = true;
    this._error = undefined;
    this._periodData = undefined;

    try {
      const url =
        this._getPeriodDataUrl();

      if (!url) {
        return;
      }

      const response =
        await this._fetchApi(
          url,
          {
            method: "GET",
            headers: {
              Accept: "application/json",
            },
          }
        );

      this._periodData =
        (await response.json()) as ClarityPeriodSummary;
    } catch (error) {
      this._setError(error);
    } finally {
      this._loading = false;
    }
  }

  /**
   * Builds the monthly or yearly API URL for the active view.
   */
  private _getPeriodDataUrl():
    string | undefined {
    if (this._viewMode === "monthly") {
      return (
        `${API_BASE}/monthly` +
        `?year=${this._selectedYear}` +
        `&month=${this._selectedMonth}`
      );
    }

    if (this._viewMode === "yearly") {
      return (
        `${API_BASE}/yearly` +
        `?year=${this._selectedYear}`
      );
    }

    return undefined;
  }


  /* ------------------------------------------------------------------------ */
  /* Import                                                                   */
  /* ------------------------------------------------------------------------ */

  /**
   * Imports the latest Microsoft Clarity data and refreshes
   * the currently displayed dashboard view when data is saved.
   */
  private async _importClarity() {
    this._loading = true;
    this._error = undefined;

    try {
      const response =
        await this._fetchApi(
          `${API_BASE}/import`,
          {
            method: "POST",
            headers: {
              Accept: "application/json",
            },
          }
        );

      const result =
        (await response.json()) as ClarityImportResult;

      if (!result.saved) {
        return;
      }

      await this._loadAvailableYears();

      if (this._viewMode === "latest") {
        await this._loadLatestSnapshot();
        return;
      }

      await this._loadPeriodData();
    } catch (error) {
      this._setError(error);
    } finally {
      this._loading = false;
    }
  }


  /* ------------------------------------------------------------------------ */
  /* View controls                                                            */
  /* ------------------------------------------------------------------------ */

  /**
   * Changes the active dashboard view and loads the data
   * required by the selected reporting mode.
   */
  private async _setViewMode(
    mode: ViewMode
  ) {
    this._viewMode = mode;
    this._error = undefined;

    if (mode === "latest") {
      await this._loadLatestSnapshot();
      return;
    }

    await this._loadPeriodData();
  }

  /**
   * Updates the selected reporting year and reloads period data.
   */
  private async _onYearChange(
    event: Event
  ) {
    const select =
      event.target as HTMLSelectElement;

    this._selectedYear =
      Number(select.value);

    if (this._viewMode !== "latest") {
      await this._loadPeriodData();
    }
  }

  /**
   * Updates the selected reporting month and reloads monthly data.
   */
  private async _onMonthChange(
    event: Event
  ) {
    const select =
      event.target as HTMLSelectElement;

    this._selectedMonth =
      Number(select.value);

    if (this._viewMode === "monthly") {
      await this._loadPeriodData();
    }
  }


  /* ------------------------------------------------------------------------ */
  /* AI PDF report generation                                                 */
  /* ------------------------------------------------------------------------ */

  /**
   * Generates an AI-assisted PDF report for the currently
   * selected monthly or yearly reporting period.
   */
  private async _generateReport() {
    if (this._viewMode === "latest") {
      return;
    }

    this._generatingReport = true;
    this._error = undefined;

    try {
      const response =
        await this._fetchApi(
          `${API_BASE}/report/pdf`,
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Accept: "application/pdf",
            },
            body: JSON.stringify(
              this._getReportRequest()
            ),
          }
        );

      const blob =
        await response.blob();

      const contentDisposition =
        response.headers.get(
          "content-disposition"
        );

      const filename =
        this._getReportFilename(
          contentDisposition
        );

      this._downloadPdf(
        blob,
        filename
      );
    } catch (error) {
      this._setError(error);
    } finally {
      this._generatingReport = false;
    }
  }

  /**
   * Builds the request body used by the AI PDF report endpoint.
   */
  private _getReportRequest() {
    return {
      periodType: this._viewMode,
      year: this._selectedYear,
      month:
        this._viewMode === "monthly"
          ? this._selectedMonth
          : null,
    };
  }

  /**
   * Extracts the PDF filename from the Content-Disposition header,
   * falling back to a predictable Hu Signal filename.
   */
  private _getReportFilename(
    contentDisposition: string | null
  ) {
    if (contentDisposition) {
      const utf8Match =
        contentDisposition.match(
          /filename\*=UTF-8''([^;]+)/
        );

      if (utf8Match?.[1]) {
        return decodeURIComponent(
          utf8Match[1]
        );
      }

      const filenameMatch =
        contentDisposition.match(
          /filename="?([^";]+)"?/
        );

      if (filenameMatch?.[1]) {
        return filenameMatch[1];
      }
    }

    if (this._viewMode === "monthly") {
      const month =
        String(
          this._selectedMonth
        ).padStart(2, "0");

      return (
        `hu-signal-monthly-report-` +
        `${this._selectedYear}-${month}.pdf`
      );
    }

    return (
      `hu-signal-yearly-report-` +
      `${this._selectedYear}.pdf`
    );
  }

  /**
   * Downloads the generated PDF without allowing the Umbraco
   * backoffice router to intercept the temporary blob URL.
   */
  private _downloadPdf(
    blob: Blob,
    filename: string
  ) {
    const url =
      URL.createObjectURL(blob);

    const link =
      document.createElement("a");

    link.href = url;
    link.download = filename;
    link.target = "_blank";
    link.rel = "noopener";

    link.addEventListener(
      "click",
      (event) => {
        event.stopPropagation();
      }
    );

    document.body.appendChild(link);

    link.click();

    document.body.removeChild(link);

    setTimeout(() => {
      URL.revokeObjectURL(url);
    }, 1000);
  }


  /* ------------------------------------------------------------------------ */
  /* Formatting                                                               */
  /* ------------------------------------------------------------------------ */

  /**
   * Formats an ISO date for display using the current locale.
   */
  private _formatDate(
    value: string
  ) {
    return new Date(
      value
    ).toLocaleDateString();
  }

  /**
   * Formats an ISO date/time for display using the current locale.
   */
  private _formatDateTime(
    value: string
  ) {
    return new Date(
      value
    ).toLocaleString();
  }

  /**
   * Formats an integer using locale-aware number formatting.
   */
  private _formatNumber(
    value: number
  ) {
    return new Intl.NumberFormat()
      .format(value);
  }

  /**
   * Formats a decimal value to a maximum of two decimal places.
   */
  private _formatDecimal(
    value: number
  ) {
    return new Intl.NumberFormat(
      undefined,
      {
        maximumFractionDigits: 2,
      }
    ).format(value);
  }


  /* ------------------------------------------------------------------------ */
  /* Breakdown helpers                                                        */
  /* ------------------------------------------------------------------------ */

  /**
   * Returns breakdown records from the latest Clarity snapshot
   * matching the supplied metric name.
   */
  private _getLatestBreakdowns(
    metricName: string
  ) {
    return (
      this._clarityData
        ?.breakdowns
        .filter(
          (item) =>
            item.metricName === metricName
        ) ?? []
    );
  }

  /**
   * Returns breakdown records from the current period summary
   * matching the supplied metric name.
   */
  private _getPeriodBreakdowns(
    metricName: string
  ) {
    return (
      this._periodData
        ?.breakdowns
        .filter(
          (item) =>
            item.metricName === metricName
        ) ?? []
    );
  }


  /* ------------------------------------------------------------------------ */
  /* Small rendering helpers                                                  */
  /* ------------------------------------------------------------------------ */

  /**
   * Renders a headline metric card.
   */
  private _renderMetric(
    label: string,
    value: string
  ) {
    return html`
      <div class="metric">
        <span class="metric-label">
          ${label}
        </span>

        <strong class="metric-value">
          ${value}
        </strong>
      </div>
    `;
  }

  /**
   * Renders a single behavioural or engagement signal.
   */
  private _renderSignal(
    label: string,
    value: string | number
  ) {
    return html`
      <div class="signal">
        <span>
          ${label}
        </span>

        <strong>
          ${value}
        </strong>
      </div>
    `;
  }

  /**
   * Renders a breakdown box for the latest Clarity snapshot.
   */
  private _renderLatestBreakdown(
    title: string,
    metricName: string
  ) {
    const items =
      this._getLatestBreakdowns(
        metricName
      );

    if (!items.length) {
      return html``;
    }

    return html`
      <uui-box headline=${title}>
        <div class="signal-list">
          ${items.map(
            (item) => {
              const label =
                item.name ??
                item.url ??
                (
                  metricName === "ReferrerUrl"
                    ? "Direct"
                    : "Unknown"
                );

              const value =
                item.sessionsCount ??
                item.visitsCount ??
                0;

              return html`
                <div class="signal">
                  <span class="breakdown-label">
                    ${label}
                  </span>

                  <strong>
                    ${this._formatNumber(
                      value
                    )}
                  </strong>
                </div>
              `;
            }
          )}
        </div>
      </uui-box>
    `;
  }

  /**
   * Renders a breakdown box for a monthly or yearly Clarity summary.
   */
  private _renderPeriodBreakdown(
    title: string,
    metricName: string
  ) {
    const items =
      this._getPeriodBreakdowns(
        metricName
      );

    if (!items.length) {
      return html``;
    }

    return html`
      <uui-box headline=${title}>
        <div class="signal-list">
          ${items.map(
            (item) => {
              const label =
                item.name ??
                item.url ??
                (
                  metricName === "ReferrerUrl"
                    ? "Direct"
                    : "Unknown"
                );

              return html`
                <div class="signal">
                  <span class="breakdown-label">
                    ${label}
                  </span>

                  <strong>
                    ${this._formatNumber(
                      item.count
                    )}
                  </strong>
                </div>
              `;
            }
          )}
        </div>
      </uui-box>
    `;
  }


  /* ------------------------------------------------------------------------ */
  /* Main rendering                                                           */
  /* ------------------------------------------------------------------------ */

  /**
   * Renders the main Hu Signal dashboard.
   */
  render() {
    return html`
      <div class="dashboard">
        <header>
          <div>
            <h1>
              Hu Signal
            </h1>

            <p>
              SEO performance, behavioural insights
              and reporting.
            </p>
          </div>

          <uui-button
            label=${this._loading
              ? "Importing latest Clarity data"
              : "Import latest Clarity data"}
            aria-label=${this._loading
              ? "Importing latest Microsoft Clarity data"
              : "Import latest Microsoft Clarity data"}
            look="primary"
            color="positive"
            ?disabled=${this._loading}
            @click=${this._importClarity}
          >
            ${this._loading
              ? "Loading..."
              : "Import latest Clarity data"}
          </uui-button>
        </header>

        ${this._renderViewSwitcher()}

        ${this._error
          ? html`
              <uui-box
                headline="Something went wrong"
              >
                <div class="error">
                  ${this._error}
                </div>
              </uui-box>
            `
          : ""}

        ${this._renderContent()}
      </div>
    `;
  }

  /**
   * Renders the Latest, Monthly and Yearly reporting controls.
   */
  private _renderViewSwitcher() {
    return html`
      <div class="toolbar">
        <div
          class="view-switcher"
          role="group"
          aria-label="Clarity reporting period"
        >
          <uui-button
            label="Show latest Clarity data"
            aria-label="Show latest Clarity data"
            look=${this._viewMode === "latest"
              ? "primary"
              : "secondary"}
            @click=${() =>
              this._setViewMode("latest")}
          >
            Latest
          </uui-button>

          <uui-button
            label="Show monthly Clarity report"
            aria-label="Show monthly Clarity report"
            look=${this._viewMode === "monthly"
              ? "primary"
              : "secondary"}
            @click=${() =>
              this._setViewMode("monthly")}
          >
            Monthly
          </uui-button>

          <uui-button
            label="Show yearly Clarity report"
            aria-label="Show yearly Clarity report"
            look=${this._viewMode === "yearly"
              ? "primary"
              : "secondary"}
            @click=${() =>
              this._setViewMode("yearly")}
          >
            Yearly
          </uui-button>
        </div>

        ${this._viewMode !== "latest"
          ? this._renderPeriodControls()
          : ""}
      </div>
    `;
  }

  /**
   * Renders month/year selectors and the PDF report action.
   */
  private _renderPeriodControls() {
    return html`
      <div
        class="period-controls"
        aria-label="Report period controls"
      >
        ${this._viewMode === "monthly"
          ? html`
              <label>
                <span>
                  Month
                </span>

                <select
                  aria-label="Select report month"
                  .value=${String(
                    this._selectedMonth
                  )}
                  @change=${this._onMonthChange}
                >
                  ${MONTHS.map(
                    (month, index) => html`
                      <option
                        value=${index + 1}
                        ?selected=${
                          this._selectedMonth ===
                          index + 1
                        }
                      >
                        ${month}
                      </option>
                    `
                  )}
                </select>
              </label>
            `
          : ""}

        <label>
          <span>
            Year
          </span>

          <select
            aria-label="Select report year"
            .value=${String(
              this._selectedYear
            )}
            @change=${this._onYearChange}
          >
            ${this._availableYears.map(
              (year) => html`
                <option
                  value=${year}
                  ?selected=${
                    this._selectedYear === year
                  }
                >
                  ${year}
                </option>
              `
            )}
          </select>
        </label>

        <uui-button
          class="report-button"
          label=${this._generatingReport
            ? "Generating PDF report"
            : "Generate PDF report"}
          aria-label=${this._generatingReport
            ? "Generating PDF report"
            : "Generate PDF report for selected period"}
          look="primary"
          color="positive"
          ?disabled=${this._generatingReport}
          @click=${this._generateReport}
        >
          ${this._generatingReport
            ? "Generating PDF..."
            : "Generate PDF report"}
        </uui-button>
      </div>
    `;
  }

  /**
   * Renders the appropriate dashboard content for the current state.
   */
  private _renderContent() {
    if (this._loading) {
      return html`
        <uui-box>
          <div class="loading-state">
            Loading Hu Signal data...
          </div>
        </uui-box>
      `;
    }

    if (this._viewMode === "latest") {
      return this._clarityData
        ? this._renderLatestSnapshot()
        : this._renderEmptyState();
    }

    return this._periodData
      ? this._renderPeriodSummary()
      : this._renderEmptyPeriodState();
  }

  /**
   * Renders the empty state shown when no Clarity snapshots exist.
   */
  private _renderEmptyState() {
    return html`
      <uui-box headline="Microsoft Clarity">
        <div class="empty-state">
          <h2>
            No saved Clarity data yet
          </h2>

          <p>
            Import the latest Microsoft Clarity data
            to create the first Hu Signal snapshot.
          </p>

          <uui-button
            label="Import Clarity data"
            aria-label="Import Microsoft Clarity data"
            look="primary"
            color="positive"
            ?disabled=${this._loading}
            @click=${this._importClarity}
          >
            Import Clarity data
          </uui-button>
        </div>
      </uui-box>
    `;
  }

  /**
   * Renders the empty state for a monthly or yearly period
   * with no stored Clarity data.
   */
  private _renderEmptyPeriodState() {
    return html`
      <uui-box headline="Microsoft Clarity">
        <div class="empty-state">
          <h2>
            No data for this period
          </h2>

          <p>
            Hu Signal does not currently have any
            saved Clarity snapshots for the selected
            period.
          </p>
        </div>
      </uui-box>
    `;
  }

  /**
   * Renders the most recently imported Clarity snapshot.
   */
  private _renderLatestSnapshot() {
    const snapshot =
      this._clarityData!.snapshot;

    return html`
      <uui-box headline="Microsoft Clarity">
        <div class="snapshot-meta">
          <span>
            Snapshot:

            <strong>
              ${this._formatDate(
                snapshot.snapshotDate
              )}
            </strong>
          </span>

          <span>
            Last imported:

            <strong>
              ${this._formatDateTime(
                snapshot.retrievedAtUtc
              )}
            </strong>
          </span>
        </div>

        <div class="metrics">
          ${this._renderMetric(
            "Sessions",
            this._formatNumber(
              snapshot.totalSessions
            )
          )}

          ${this._renderMetric(
            "Users",
            this._formatNumber(
              snapshot.distinctUsers
            )
          )}

          ${this._renderMetric(
            "Pages / session",
            this._formatDecimal(
              snapshot.pagesPerSession
            )
          )}

          ${this._renderMetric(
            "Avg. scroll depth",
            `${this._formatDecimal(
              snapshot.averageScrollDepth
            )}%`
          )}

          ${this._renderMetric(
            "Active time",
            `${this._formatNumber(
              snapshot.engagementActiveTime
            )}s`
          )}

          ${this._renderMetric(
            "Bot sessions",
            this._formatNumber(
              snapshot.botSessions
            )
          )}
        </div>
      </uui-box>

      <div class="grid">
        <uui-box headline="Behaviour signals">
          <div class="signal-list">
            ${this._renderSignal(
              "Quickbacks",
              snapshot.quickbacks
            )}

            ${this._renderSignal(
              "Rage clicks",
              snapshot.rageClicks
            )}

            ${this._renderSignal(
              "Dead clicks",
              snapshot.deadClicks
            )}

            ${this._renderSignal(
              "Excessive scroll",
              snapshot.excessiveScrolls
            )}

            ${this._renderSignal(
              "Error clicks",
              snapshot.errorClicks
            )}

            ${this._renderSignal(
              "Script errors",
              snapshot.scriptErrors
            )}
          </div>
        </uui-box>

        <uui-box headline="Engagement">
          <div class="signal-list">
            ${this._renderSignal(
              "Total engagement time",
              `${snapshot.engagementTotalTime}s`
            )}

            ${this._renderSignal(
              "Active engagement time",
              `${snapshot.engagementActiveTime}s`
            )}

            ${this._renderSignal(
              "Average scroll depth",
              `${this._formatDecimal(
                snapshot.averageScrollDepth
              )}%`
            )}

            ${this._renderSignal(
              "Pages per session",
              this._formatDecimal(
                snapshot.pagesPerSession
              )
            )}
          </div>
        </uui-box>
      </div>

      <div class="grid">
        ${this._renderLatestBreakdown(
          "Devices",
          "Device"
        )}

        ${this._renderLatestBreakdown(
          "Countries",
          "Country"
        )}
      </div>

      <div class="grid">
        ${this._renderLatestBreakdown(
          "Browsers",
          "Browser"
        )}

        ${this._renderLatestBreakdown(
          "Operating systems",
          "OS"
        )}
      </div>

      <div class="grid">
        ${this._renderLatestBreakdown(
          "Top pages",
          "PopularPages"
        )}

        ${this._renderLatestBreakdown(
          "Referrers",
          "ReferrerUrl"
        )}
      </div>

      <div class="single">
        ${this._renderLatestBreakdown(
          "Page titles",
          "PageTitle"
        )}
      </div>
    `;
  }

  /**
   * Renders the aggregated monthly or yearly Clarity summary.
   */
  private _renderPeriodSummary() {
    const data =
      this._periodData!;

    const title =
      this._viewMode === "monthly"
        ? "Monthly Clarity summary"
        : "Yearly Clarity summary";

    return html`
      <uui-box headline=${title}>
        <div class="snapshot-meta">
          <span>
            From:

            <strong>
              ${this._formatDate(
                data.from
              )}
            </strong>
          </span>

          <span>
            To:

            <strong>
              ${this._formatDate(
                data.to
              )}
            </strong>
          </span>

          <span>
            Days with data:

            <strong>
              ${data.daysWithData}
            </strong>
          </span>
        </div>

        <div class="metrics">
          ${this._renderMetric(
            "Sessions",
            this._formatNumber(
              data.totalSessions
            )
          )}

          ${this._renderMetric(
            "Pages / session",
            this._formatDecimal(
              data.averagePagesPerSession
            )
          )}

          ${this._renderMetric(
            "Avg. scroll depth",
            `${this._formatDecimal(
              data.averageScrollDepth
            )}%`
          )}

          ${this._renderMetric(
            "Bot sessions",
            this._formatNumber(
              data.botSessions
            )
          )}
        </div>
      </uui-box>

      <div class="grid">
        <uui-box headline="Behaviour signals">
          <div class="signal-list">
            ${this._renderSignal(
              "Quickbacks",
              data.quickbacks
            )}

            ${this._renderSignal(
              "Rage clicks",
              data.rageClicks
            )}

            ${this._renderSignal(
              "Dead clicks",
              data.deadClicks
            )}

            ${this._renderSignal(
              "Excessive scroll",
              data.excessiveScrolls
            )}

            ${this._renderSignal(
              "Error clicks",
              data.errorClicks
            )}

            ${this._renderSignal(
              "Script errors",
              data.scriptErrors
            )}
          </div>
        </uui-box>

        ${this._renderPeriodBreakdown(
          "Devices",
          "Device"
        )}
      </div>

      <div class="grid">
        ${this._renderPeriodBreakdown(
          "Countries",
          "Country"
        )}

        ${this._renderPeriodBreakdown(
          "Browsers",
          "Browser"
        )}
      </div>

      <div class="grid">
        ${this._renderPeriodBreakdown(
          "Operating systems",
          "OS"
        )}

        ${this._renderPeriodBreakdown(
          "Referrers",
          "ReferrerUrl"
        )}
      </div>

      <div class="grid">
        ${this._renderPeriodBreakdown(
          "Top pages",
          "PopularPages"
        )}

        ${this._renderPeriodBreakdown(
          "Page titles",
          "PageTitle"
        )}
      </div>
    `;
  }


  /* ------------------------------------------------------------------------ */
  /* Styles                                                                   */
  /* ------------------------------------------------------------------------ */

  static styles = [
    css`
      :host {
        display: block;
        padding: var(--uui-size-layout-1);
      }

      .dashboard {
        max-width: 1400px;
        margin: 0 auto;
      }

      header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--uui-size-space-6);
        margin-bottom: var(--uui-size-layout-1);
      }

      h1 {
        margin:
          0
          0
          var(--uui-size-space-2);
      }

      header p {
        margin: 0;
        color: var(--uui-color-text-alt);
      }

      .toolbar {
        display: flex;
        align-items: flex-end;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: var(--uui-size-space-4);
        margin-bottom: var(--uui-size-layout-1);
      }

      .view-switcher {
        display: flex;
        gap: var(--uui-size-space-2);
      }

      .period-controls {
        display: flex;
        align-items: flex-end;
        gap: var(--uui-size-space-4);
      }

      .period-controls label {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-space-2);
        font-size: 0.9rem;
        color: var(--uui-color-text-alt);
      }

      .period-controls select {
        min-width: 140px;
        padding: var(--uui-size-space-3);
        border:
          1px solid
          var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        background: var(--uui-color-surface);
        color: var(--uui-color-text);
        font: inherit;
      }

      .snapshot-meta {
        display: flex;
        flex-wrap: wrap;
        gap: var(--uui-size-space-6);
        margin-bottom: var(--uui-size-space-5);
        color: var(--uui-color-text-alt);
      }

      .metrics {
        display: grid;
        grid-template-columns:
          repeat(
            auto-fit,
            minmax(160px, 1fr)
          );
        gap: var(--uui-size-space-4);
      }

      .metric {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-space-2);
        padding: var(--uui-size-space-4);
        background:
          var(--uui-color-surface-alt);
        border-radius:
          var(--uui-border-radius);
      }

      .metric-label {
        color: var(--uui-color-text-alt);
        font-size: 0.9rem;
      }

      .metric-value {
        font-size: 1.8rem;
      }

      .grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: var(--uui-size-layout-1);
        margin-top: var(--uui-size-layout-1);
      }

      .single {
        margin-top: var(--uui-size-layout-1);
      }

      .signal-list {
        display: flex;
        flex-direction: column;
      }

      .signal {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--uui-size-space-4);
        padding:
          var(--uui-size-space-4)
          0;
        border-bottom:
          1px solid
          var(--uui-color-border);
      }

      .signal:last-child {
        border-bottom: 0;
      }

      .breakdown-label {
        overflow-wrap: anywhere;
      }

      .empty-state,
      .loading-state {
        padding: var(--uui-size-layout-1);
        text-align: center;
      }

      .empty-state h2 {
        margin-top: 0;
      }

      .empty-state p {
        color: var(--uui-color-text-alt);
      }

      .error {
        color: var(--uui-color-danger);
        white-space: pre-wrap;
      }

      @media (max-width: 900px) {
        header {
          align-items: flex-start;
          flex-direction: column;
        }

        .toolbar {
          align-items: flex-start;
          flex-direction: column;
        }

        .view-switcher {
          flex-wrap: wrap;
        }

        .period-controls {
          width: 100%;
          align-items: stretch;
          flex-direction: column;
        }

        .period-controls label,
        .period-controls select,
        .report-button {
          width: 100%;
        }

        .grid {
          grid-template-columns: 1fr;
        }

        .snapshot-meta {
          flex-direction: column;
          gap: var(--uui-size-space-2);
        }
      }
    `,
  ];
}


export default HuSignalDashboardElement;


declare global {
  interface HTMLElementTagNameMap {
    "hu-signal-dashboard":
      HuSignalDashboardElement;
  }
}