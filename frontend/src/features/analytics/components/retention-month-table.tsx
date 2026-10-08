import { businessReport } from '../content/business-report';
import { formatCount, formatMonth } from '../lib/format-report';
import type { RetentionAnalytics } from '../types/analytics.types';

/** Each month's customers, split into first-timers and those who had been before. */
export function RetentionMonthTable({ months }: { months: RetentionAnalytics['months'] }) {
  const columns = businessReport.retention.columns;

  return (
    <div className="overflow-x-auto rounded-lg border border-ink-100">
      <table className="w-full text-sm">
        <thead className="border-b border-ink-100 text-left text-ink-500">
          <tr>
            <th scope="col" className="px-3 py-2 font-medium">{columns.month}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{columns.active}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{columns.new}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{columns.returning}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100 tabular-nums">
          {months.map((month) => (
            <tr key={month.month}>
              <td className="px-3 py-2 text-ink-900">{formatMonth(month.month)}</td>
              <td className="px-3 py-2 text-right font-medium text-ink-900">{formatCount(month.active)}</td>
              <td className="px-3 py-2 text-right">{formatCount(month.new)}</td>
              <td className="px-3 py-2 text-right">{formatCount(month.returning)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
