import {
  LitElement,
  css,
  html,
  customElement,
  state,
} from "@umbraco-cms/backoffice/external/lit";

import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";

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

  override connectedCallback() {
    super.connectedCallback();

    void this._loadLatestSnapshot();
  }

  private async _getAuthToken() {
    const authContext = await this.getContext(UMB_AUTH_CONTEXT);

    return await authContext?.getLatestToken();
  }

  private async _loadLatestSnapshot() {
    this._loading = true;
    this._error = undefined;

    try {
      const token = await this._getAuthToken();

      const response = await fetch(
        "/umbraco/husignal/api/v1/clarity/latest",
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
        const text = await response.text();

        throw new Error(
          `Hu Signal API returned ${response.status}: ${text}`
        );
      }

      this._clarityData =
        (await response.json()) as ClarityLatestResponse;
    } catch (error) {
      this._error =
        error instanceof Error
          ? error.message
          : String(error);
    } finally {
      this._loading = false;
    }
  }

  private async _loadPeriodData() {
    this._loading = true;
    this._error = undefined;
    this._periodData = undefined;

    try {
      const token = await this._getAuthToken();

      let url = "";

      if (this._viewMode === "monthly") {
        url =
          `/umbraco/husignal/api/v1/clarity/monthly` +
          `?year=${this._selectedYear}` +
          `&month=${this._selectedMonth}`;
      }

      if (this._viewMode === "yearly") {
        url =
          `/umbraco/husignal/api/v1/clarity/yearly` +
          `?year=${this._selectedYear}`;
      }

      if (!url) {
        return;
      }

      const response = await fetch(url, {
        method: "GET",
        credentials: "include",
        headers: {
          Authorization: `Bearer ${token}`,
          Accept: "application/json",
        },
      });

      if (!response.ok) {
        const text = await response.text();

        throw new Error(
          `Hu Signal API returned ${response.status}: ${text}`
        );
      }

      this._periodData =
        (await response.json()) as ClarityPeriodSummary;
    } catch (error) {
      this._error =
        error instanceof Error
          ? error.message
          : String(error);
    } finally {
      this._loading = false;
    }
  }

  private async _importClarity() {
    this._loading = true;
    this._error = undefined;

    try {
      const token = await this._getAuthToken();

      const response = await fetch(
        "/umbraco/husignal/api/v1/clarity/import",
        {
          method: "POST",
          credentials: "include",
          headers: {
            Authorization: `Bearer ${token}`,
            Accept: "application/json",
          },
        }
      );

      if (!response.ok) {
        const text = await response.text();

        throw new Error(
          `Hu Signal API returned ${response.status}: ${text}`
        );
      }

      const result =
        (await response.json()) as ClarityImportResult;

      if (result.saved) {
        if (this._viewMode === "latest") {
          await this._loadLatestSnapshot();
        } else {
          await this._loadPeriodData();
        }
      }
    } catch (error) {
      this._error =
        error instanceof Error
          ? error.message
          : String(error);
    } finally {
      this._loading = false;
    }
  }

  private async _setViewMode(mode: ViewMode) {
    this._viewMode = mode;
    this._error = undefined;

    if (mode === "latest") {
      await this._loadLatestSnapshot();
      return;
    }

    await this._loadPeriodData();
  }

  private async _onYearChange(event: Event) {
    const select = event.target as HTMLSelectElement;

    this._selectedYear = Number(select.value);

    if (this._viewMode !== "latest") {
      await this._loadPeriodData();
    }
  }

  private async _onMonthChange(event: Event) {
    const select = event.target as HTMLSelectElement;

    this._selectedMonth = Number(select.value);

    if (this._viewMode === "monthly") {
      await this._loadPeriodData();
    }
  }

  private _formatDate(value: string) {
    return new Date(value).toLocaleDateString();
  }

  private _formatDateTime(value: string) {
    return new Date(value).toLocaleString();
  }

  private _formatNumber(value: number) {
    return new Intl.NumberFormat().format(value);
  }

  private _formatDecimal(value: number) {
    return new Intl.NumberFormat(undefined, {
      maximumFractionDigits: 2,
    }).format(value);
  }

  private _getLatestBreakdowns(metricName: string) {
    return this._clarityData?.breakdowns.filter(
      (item) => item.metricName === metricName
    ) ?? [];
  }

  private _getPeriodBreakdowns(metricName: string) {
    return this._periodData?.breakdowns.filter(
      (item) => item.metricName === metricName
    ) ?? [];
  }

  private _renderMetric(label: string, value: string) {
    return html`
      <div class="metric">
        <span class="metric-label">${label}</span>
        <strong class="metric-value">${value}</strong>
      </div>
    `;
  }

  private _renderSignal(
    label: string,
    value: string | number
  ) {
    return html`
      <div class="signal">
        <span>${label}</span>
        <strong>${value}</strong>
      </div>
    `;
  }

  private _renderLatestBreakdown(
    title: string,
    metricName: string
  ) {
    const items = this._getLatestBreakdowns(metricName);

    if (!items.length) {
      return html``;
    }

    return html`
      <uui-box headline=${title}>
        <div class="signal-list">
          ${items.map((item) => {
            const label =
              item.name ??
              item.url ??
              (metricName === "ReferrerUrl"
                ? "Direct"
                : "Unknown");

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
                  ${this._formatNumber(value)}
                </strong>
              </div>
            `;
          })}
        </div>
      </uui-box>
    `;
  }

  private _renderPeriodBreakdown(
    title: string,
    metricName: string
  ) {
    const items = this._getPeriodBreakdowns(metricName);

    if (!items.length) {
      return html``;
    }

    return html`
      <uui-box headline=${title}>
        <div class="signal-list">
          ${items.map((item) => {
            const label =
              item.name ??
              item.url ??
              (metricName === "ReferrerUrl"
                ? "Direct"
                : "Unknown");

            return html`
              <div class="signal">
                <span class="breakdown-label">
                  ${label}
                </span>

                <strong>
                  ${this._formatNumber(item.count)}
                </strong>
              </div>
            `;
          })}
        </div>
      </uui-box>
    `;
  }

  private _renderViewSwitcher() {
    return html`
      <div class="toolbar">
        <div class="view-switcher">
          <uui-button
            look=${this._viewMode === "latest"
              ? "primary"
              : "secondary"}
            @click=${() => this._setViewMode("latest")}
          >
            Latest
          </uui-button>

          <uui-button
            look=${this._viewMode === "monthly"
              ? "primary"
              : "secondary"}
            @click=${() => this._setViewMode("monthly")}
          >
            Monthly
          </uui-button>

          <uui-button
            look=${this._viewMode === "yearly"
              ? "primary"
              : "secondary"}
            @click=${() => this._setViewMode("yearly")}
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

  private _renderPeriodControls() {
    const currentYear = new Date().getFullYear();

    const years = Array.from(
      { length: 6 },
      (_, index) => currentYear - index
    );

    const months = [
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
    ];

    return html`
      <div class="period-controls">
        ${this._viewMode === "monthly"
          ? html`
              <label>
                <span>Month</span>

                <select
                  .value=${String(this._selectedMonth)}
                  @change=${this._onMonthChange}
                >
                  ${months.map(
                    (month, index) => html`
                      <option
                        value=${index + 1}
                        ?selected=${this._selectedMonth ===
                        index + 1}
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
          <span>Year</span>

          <select
            .value=${String(this._selectedYear)}
            @change=${this._onYearChange}
          >
            ${years.map(
              (year) => html`
                <option
                  value=${year}
                  ?selected=${this._selectedYear === year}
                >
                  ${year}
                </option>
              `
            )}
          </select>
        </label>
      </div>
    `;
  }

  render() {
    return html`
      <div class="dashboard">
        <header>
          <div>
            <h1>Hu Signal</h1>
            <p>
              SEO performance, behavioural insights and reporting.
            </p>
          </div>

          <uui-button
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
              <uui-box headline="Something went wrong">
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

  private _renderEmptyState() {
    return html`
      <uui-box headline="Microsoft Clarity">
        <div class="empty-state">
          <h2>No saved Clarity data yet</h2>

          <p>
            Import the latest Microsoft Clarity data to create
            the first Hu Signal snapshot.
          </p>

          <uui-button
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

  private _renderEmptyPeriodState() {
    return html`
      <uui-box headline="Microsoft Clarity">
        <div class="empty-state">
          <h2>No data for this period</h2>

          <p>
            Hu Signal does not currently have any saved Clarity
            snapshots for the selected period.
          </p>
        </div>
      </uui-box>
    `;
  }

  private _renderLatestSnapshot() {
    const snapshot = this._clarityData!.snapshot;

    return html`
      <uui-box headline="Microsoft Clarity">
        <div class="snapshot-meta">
          <span>
            Snapshot:
            <strong>
              ${this._formatDate(snapshot.snapshotDate)}
            </strong>
          </span>

          <span>
            Last imported:
            <strong>
              ${this._formatDateTime(snapshot.retrievedAtUtc)}
            </strong>
          </span>
        </div>

        <div class="metrics">
          ${this._renderMetric(
            "Sessions",
            this._formatNumber(snapshot.totalSessions)
          )}

          ${this._renderMetric(
            "Users",
            this._formatNumber(snapshot.distinctUsers)
          )}

          ${this._renderMetric(
            "Pages / session",
            this._formatDecimal(snapshot.pagesPerSession)
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
            this._formatNumber(snapshot.botSessions)
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
              this._formatDecimal(snapshot.pagesPerSession)
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

  private _renderPeriodSummary() {
    const data = this._periodData!;

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
              ${this._formatDate(data.from)}
            </strong>
          </span>

          <span>
            To:
            <strong>
              ${this._formatDate(data.to)}
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
            this._formatNumber(data.totalSessions)
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
            this._formatNumber(data.botSessions)
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
        margin: 0 0 var(--uui-size-space-2);
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
        gap: var(--uui-size-space-4);
      }

      .period-controls label {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-space-2);
        font-size: 0.9rem;
      }

      .period-controls select {
        min-width: 140px;
        padding: var(--uui-size-space-3);
        border: 1px solid var(--uui-color-border);
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
          repeat(auto-fit, minmax(160px, 1fr));
        gap: var(--uui-size-space-4);
      }

      .metric {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-space-2);
        padding: var(--uui-size-space-4);
        background: var(--uui-color-surface-alt);
        border-radius: var(--uui-border-radius);
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
        padding: var(--uui-size-space-4) 0;
        border-bottom:
          1px solid var(--uui-color-border);
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

        .period-controls {
          width: 100%;
          flex-direction: column;
        }

        .period-controls label,
        .period-controls select {
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
    "hu-signal-dashboard": HuSignalDashboardElement;
  }
}