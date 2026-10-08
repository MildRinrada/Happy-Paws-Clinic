import { staffApi } from '@/lib/api/server-client';
import type { ReportExport, StartExportInput } from '../types/report-export.types';

/**
 * `POST /staff/reports/exports` (report:export, plus the report's own read scope). Answers 202
 * with the queued job; the file is built in the background.
 */
export function startReportExport(input: StartExportInput): Promise<ReportExport> {
  return staffApi().post<ReportExport>('/staff/reports/exports', { body: input });
}
