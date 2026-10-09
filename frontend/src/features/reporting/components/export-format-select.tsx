import { Select } from '@/components/ui/field';
import { reportExport } from '../content/report-export';
import { REPORT_FORMATS } from '../lib/exportable-reports';
import type { ReportFormat } from '../types/report-export.types';

interface ExportFormatSelectProps {
  value: ReportFormat;
  onChange: (format: ReportFormat) => void;
}

/** "ไฟล์": Excel, CSV or PDF. */
export function ExportFormatSelect({ value, onChange }: ExportFormatSelectProps) {
  const text = reportExport;

  return (
    <label className="flex items-center gap-2 text-sm text-ink-700">
      {text.format}
      <Select value={value} onChange={(event) => onChange(event.target.value as ReportFormat)} className="h-9 w-36">
        {REPORT_FORMATS.map((format) => (
          <option key={format} value={format}>
            {text.formats[format]}
          </option>
        ))}
      </Select>
    </label>
  );
}
