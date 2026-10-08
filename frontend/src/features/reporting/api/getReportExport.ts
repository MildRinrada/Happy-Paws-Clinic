import { staffApi } from '@/lib/api/server-client';
import type { ReportExport } from '../types/report-export.types';

/** `GET /staff/reports/exports/{id}` (report:export). Only the requester's own jobs. */
export function getReportExport(id: string): Promise<ReportExport> {
  return staffApi().get<ReportExport>(`/staff/reports/exports/${encodeURIComponent(id)}`);
}
