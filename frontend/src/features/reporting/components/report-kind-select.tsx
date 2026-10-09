import { Select } from '@/components/ui/field';
import { reportExport } from '../content/report-export';
import type { ReportKind } from '../types/report-export.types';

interface ReportKindSelectProps {
  reports: ReportKind[];
  value: ReportKind;
  onChange: (report: ReportKind) => void;
}

/** "รายงาน": only the reports this session may export. */
export function ReportKindSelect({ reports, value, onChange }: ReportKindSelectProps) {
  const text = reportExport;

  return (
    <label className="flex items-center gap-2 text-sm text-ink-700">
      {text.report}
      <Select value={value} onChange={(event) => onChange(event.target.value as ReportKind)} className="h-9 w-64">
        {reports.map((kind) => (
          <option key={kind} value={kind}>
            {text.reports[kind]}
          </option>
        ))}
      </Select>
    </label>
  );
}
