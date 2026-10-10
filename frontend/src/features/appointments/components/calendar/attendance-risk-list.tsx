import { attendanceRiskText } from '../../content/attendance-risk';
import type { AttendanceRiskRow as Row } from '../../types/attendance-risk.types';
import { AttendanceRiskRow } from './attendance-risk-row';

/**
 * The day's appointments at medium or high risk of not being attended, folded away under one line
 * so the calendar stays first. Open by default when any is high.
 */
export function AttendanceRiskList({ rows }: { rows: Row[] }) {
  const text = attendanceRiskText;
  const risky = rows.filter((row) => row.risk !== 'low');

  if (risky.length === 0) {
    return null;
  }

  return (
    <details open={risky.some((row) => row.risk === 'high')} className="rounded-xl border border-amber-200 bg-amber-50/40 px-4 py-3">
      <summary className="cursor-pointer text-sm font-medium text-amber-900">{text.heading(risky.length)}</summary>
      <p className="mt-2 text-xs text-ink-600">{text.how}</p>
      <ul className="mt-1 divide-y divide-amber-100">
        {risky.map((row) => (
          <AttendanceRiskRow key={row.appointment_id} row={row} />
        ))}
      </ul>
    </details>
  );
}
