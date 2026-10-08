import { formatDateTime } from '@/lib/format/datetime';
import { conflictCheckText, repeatText } from '../../content/advanced-booking';
import type { ConflictReport, SeriesConflict } from '../../types/advanced-booking.types';
import { ConflictLines } from './conflict-lines';

interface OccurrenceRowProps {
  index: number;
  startsAt: string;
  /** What a conflict check found at this time, if one was run. */
  report?: ConflictReport;
  /** Why the backend would not book this date, from the last refused attempt. */
  refusal?: SeriesConflict;
}

/** One visit in the course: when, and anything known to be wrong with it. */
export function OccurrenceRow({ index, startsAt, report, refusal }: OccurrenceRowProps) {
  const troubled = Boolean(refusal) || (report && !report.clear);

  return (
    <li className={`rounded-lg border px-3 py-2 ${troubled ? 'border-amber-300 bg-amber-50/60' : 'border-ink-200 bg-white'}`}>
      <div className="flex flex-wrap items-baseline justify-between gap-x-3">
        <p className="text-sm text-ink-900">
          <span className="mr-2 text-ink-500">{repeatText.visit(index)}</span>
          <span className="tabular-nums">{formatDateTime(startsAt)}</span>
        </p>
        {report?.clear && !refusal && <span className="text-xs font-medium text-emerald-700">{conflictCheckText.clear}</span>}
      </div>

      {refusal && (
        <div className="mt-1 space-y-0.5 text-sm">
          <p className="text-red-800">
            <span className="font-medium">{repeatText.refused}</span>: {refusal.reason}
          </p>
          {refusal.suggested.length > 0 && (
            <p className="text-ink-600">
              {repeatText.suggested}: <span className="tabular-nums">{refusal.suggested.join(' ')}</span>
            </p>
          )}
        </div>
      )}

      {report && !report.clear && (
        <div className="mt-1">
          <ConflictLines conflicts={report.conflicts} />
        </div>
      )}
    </li>
  );
}
