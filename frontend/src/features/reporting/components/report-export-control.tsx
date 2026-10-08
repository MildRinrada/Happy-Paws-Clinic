'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Select } from '@/components/ui/field';
import { reportExport } from '../content/report-export';
import { useExportJob } from '../hooks/use-export-job';
import type { ReportKind } from '../types/report-export.types';
import { ExportStatusLine } from './export-status-line';

interface ReportExportControlProps {
  /** The reports this session may export, already narrowed by scope. */
  reports: ReportKind[];
  from: string;
  to: string;
  branchId: string;
}

/**
 * "ส่งออกรายงาน": the range on screen, one report, as a CSV built in the background. The control
 * waits for the file and then offers the download.
 */
export function ReportExportControl({ reports, from, to, branchId }: ReportExportControlProps) {
  const text = reportExport;
  const [report, setReport] = useState<ReportKind>(reports[0]!);
  const { job, error, starting, polling, start, check } = useExportJob();

  return (
    <div className="space-y-2 rounded-xl border border-ink-200 bg-white px-4 py-3">
      <div className="flex flex-wrap items-center gap-3">
        <label className="flex items-center gap-2 text-sm text-ink-700">
          {text.report}
          <Select value={report} onChange={(event) => setReport(event.target.value as ReportKind)} className="h-9 w-64">
            {reports.map((kind) => (
              <option key={kind} value={kind}>
                {text.reports[kind]}
              </option>
            ))}
          </Select>
        </label>
        <Button variant="secondary" size="sm" disabled={starting || polling} onClick={() => start({ report, from, to, branchId })}>
          {starting ? text.starting : text.button}
        </Button>
        <span className="text-xs text-ink-500">{text.range(from, to)}</span>
      </div>

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error === 'unreachable' ? text.unreachable : error.message}
        </p>
      )}
      {job && <ExportStatusLine job={job} polling={polling} onCheck={check} />}
    </div>
  );
}
