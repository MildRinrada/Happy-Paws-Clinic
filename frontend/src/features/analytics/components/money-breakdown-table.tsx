import { formatBaht } from '@/lib/format/money';
import { businessReport } from '../content/business-report';

export interface MoneyRow {
  key: string;
  name: string;
  collected_satang: number;
  refunded_satang: number;
  net_satang: number;
}

/** Collected, refunded and net for a handful of rows: tenders, or branches. */
export function MoneyBreakdownTable({ heading, nameColumn, rows }: { heading: string; nameColumn: string; rows: MoneyRow[] }) {
  const columns = businessReport.revenue.columns;

  return (
    <div className="space-y-1.5">
      <h4 className="text-xs font-semibold text-ink-700">{heading}</h4>
      <div className="overflow-x-auto rounded-lg border border-ink-100">
        <table className="w-full text-sm">
          <thead className="border-b border-ink-100 text-left text-ink-500">
            <tr>
              <th scope="col" className="px-3 py-2 font-medium">{nameColumn}</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">{columns.collected}</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">{columns.refunded}</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">{columns.net}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-ink-100 tabular-nums">
            {rows.map((row) => (
              <tr key={row.key}>
                <td className="px-3 py-2 text-ink-900">{row.name}</td>
                <td className="px-3 py-2 text-right">{formatBaht(row.collected_satang)}</td>
                <td className="px-3 py-2 text-right">{formatBaht(row.refunded_satang)}</td>
                <td className="px-3 py-2 text-right font-medium text-ink-900">{formatBaht(row.net_satang)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
