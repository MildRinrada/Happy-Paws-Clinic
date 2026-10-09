import Link from 'next/link';
import { formatDateTime } from '@/lib/format/datetime';
import { pointsKindLabels } from '../../content/crm-labels';
import { loyaltyPage } from '../../content/loyalty-page';
import { crmPaths } from '../../lib/crm-paths';
import type { PointsEntry } from '../../types/crm.types';

/** Every movement of the customer's points, newest first, each tied to its bill when it has one. */
export function PointsHistoryTable({ entries }: { entries: PointsEntry[] }) {
  const text = loyaltyPage.history;

  if (entries.length === 0) {
    return <p className="text-sm text-ink-500">{text.empty}</p>;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-ink-200 text-left text-xs text-ink-500">
            <th className="py-2 pr-3 font-medium">{text.when}</th>
            <th className="py-2 pr-3 font-medium">{text.kind}</th>
            <th className="py-2 pr-3 font-medium">{text.invoice}</th>
            <th className="py-2 pr-3 text-right font-medium">{text.points}</th>
            <th className="py-2 font-medium">{text.note}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {entries.map((entry) => (
            <tr key={entry.id}>
              <td className="whitespace-nowrap py-2 pr-3 text-ink-600">{formatDateTime(entry.occurred_at)}</td>
              <td className="py-2 pr-3 text-ink-800">{pointsKindLabels[entry.kind]}</td>
              <td className="py-2 pr-3">
                {entry.invoice_id ? (
                  <Link href={crmPaths.invoice(entry.invoice_id)} className="text-brand-700 hover:underline">
                    {entry.invoice_number ?? entry.invoice_id}
                  </Link>
                ) : (
                  '-'
                )}
              </td>
              <td className={`py-2 pr-3 text-right font-medium tabular-nums ${entry.points < 0 ? 'text-red-700' : 'text-emerald-700'}`}>
                {text.signed(entry.points)}
              </td>
              <td className="py-2 text-ink-600">{entry.note ?? ''}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
