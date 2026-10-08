/** Shapes from `/staff/reports/exports`. Field names are the API's snake_case ones. */

export type ReportKind = 'revenue' | 'services' | 'retention' | 'staff_performance' | 'appointments' | 'queue' | 'workload';

export type ExportStatus = 'queued' | 'running' | 'succeeded' | 'failed';

export interface ReportExport {
  id: string;
  report: ReportKind;
  format: 'csv';
  status: ExportStatus;
  from: string;
  to: string;
  branch_id?: string;
  branch_ids?: string[];
  created_at: string;
  started_at?: string;
  completed_at?: string;
  file_name?: string;
  size_bytes?: number;
  row_count?: number;
  /** Signed afresh on each read of a finished job, good for an hour. */
  download_url?: string;
  download_expires_at?: string;
  error_code?: string;
}

export interface StartExportInput {
  report: ReportKind;
  format: 'csv';
  from: string;
  to: string;
  branch_id?: string;
}
