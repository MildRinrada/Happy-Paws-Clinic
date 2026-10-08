import { Button } from '@/components/ui/button';
import { reportExport } from '../content/report-export';
import type { ReportExport } from '../types/report-export.types';

interface ExportStatusLineProps {
  job: ReportExport;
  polling: boolean;
  onCheck: () => void;
}

/** Where the job is, and the download link once the file is ready. */
export function ExportStatusLine({ job, polling, onCheck }: ExportStatusLineProps) {
  const text = reportExport;
  const waiting = job.status === 'queued' || job.status === 'running';

  if (job.status === 'succeeded' && job.download_url) {
    return (
      <p className="text-sm text-ink-700">
        <a href={job.download_url} className="font-medium text-brand-700 underline" download={job.file_name}>
          {text.download(job.file_name ?? job.id)}
        </a>
        {job.row_count !== undefined && <span className="ml-2 text-xs text-ink-500">{text.rows(job.row_count)}</span>}
        <span className="block text-xs text-ink-500">{text.expires}</span>
      </p>
    );
  }

  return (
    <p role="status" className="flex flex-wrap items-center gap-2 text-sm text-ink-700">
      <span className={job.status === 'failed' ? 'text-red-700' : undefined}>{job.status === 'failed' ? text.failed : text.statuses[job.status]}</span>
      {waiting && !polling && <span className="text-xs text-ink-500">{text.stillWaiting}</span>}
      {waiting && !polling && (
        <Button variant="ghost" size="sm" onClick={onCheck}>
          {text.refresh}
        </Button>
      )}
    </p>
  );
}
