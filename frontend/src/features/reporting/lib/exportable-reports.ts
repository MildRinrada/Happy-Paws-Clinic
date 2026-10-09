import { Scope } from '@/lib/auth/scopes';
import type { ReportFormat, ReportKind } from '../types/report-export.types';

/**
 * Which reports a session may export: each needs `report:export` and the scope of the screen it
 * copies, the same rule the backend applies (`ReportCatalogue`). Only offering those keeps the
 * person from choosing a report the API will refuse.
 */
const READ_SCOPE: Record<ReportKind, string> = {
  appointments: Scope.StatsRead,
  queue: Scope.StatsRead,
  workload: Scope.StatsRead,
  revenue: Scope.AnalyticsRead,
  services: Scope.AnalyticsRead,
  retention: Scope.AnalyticsRead,
  staff_performance: Scope.AnalyticsHr,
};

export function exportableReports(can: (scope: string) => boolean): ReportKind[] {
  if (!can(Scope.ReportExport)) return [];

  return (Object.keys(READ_SCOPE) as ReportKind[]).filter((report) => can(READ_SCOPE[report]));
}

/** The file formats, in the order the control offers them. Excel first: it is what most clinics open. */
export const REPORT_FORMATS: ReportFormat[] = ['xlsx', 'csv', 'pdf'];

/** How long the control waits between status checks, and how many it makes before handing over to a button. */
export const POLL_INTERVAL_MS = 2_000;
export const POLL_ATTEMPTS = 30;
