'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { reportExport } from '../content/report-export';
import { useExportJob } from '../hooks/use-export-job';
import { REPORT_FORMATS } from '../lib/exportable-reports';
import type { ReportFormat, ReportKind } from '../types/report-export.types';
import { ExportFormatSelect } from './export-format-select';
import { ExportStatusLine } from './export-status-line';
import { ReportKindSelect } from './report-kind-select';

interface ReportExportControlProps {
  /** The reports this session may export, already narrowed by scope. */
  reports: ReportKind[];
  from: string;
  to: string;
  branchId: string;
}

/**
 * "ส่งออกรายงาน": the range on screen, one report, as an Excel, CSV or PDF file built in the
 * background. The control waits for the file and then offers the download.
 */
export function ReportExportControl({ reports, from, to, branchId }: ReportExportControlProps) {
  const text = reportExport;
  const [report, setReport] = useState<ReportKind>(reports[0]!);
  const [format, setFormat] = useState<ReportFormat>(REPORT_FORMATS[0]!);
  const { job, error, starting, polling, start, check } = useExportJob();

  return (
    <div className="space-y-2 rounded-xl border border-ink-200 bg-white px-4 py-3">
      <div className="flex flex-wrap items-center gap-3">
        <ReportKindSelect reports={reports} value={report} onChange={setReport} />
        <ExportFormatSelect value={format} onChange={setFormat} />
        <Button variant="secondary" size="sm" disabled={starting || polling} onClick={() => start({ report, format, from, to, branchId })}>
          {starting ? text.starting : text.button}
        </Button>
      </div>
      <p className="flex flex-wrap gap-x-3 text-xs text-ink-500">
        <span>{text.range(from, to)}</span>
        <span>{text.formatHints[format]}</span>
      </p>

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error === 'unreachable' ? text.unreachable : error.message}
        </p>
      )}
      {job && <ExportStatusLine job={job} polling={polling} onCheck={check} />}
    </div>
  );
}
