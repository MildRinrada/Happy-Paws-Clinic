import { formatBaht } from '@/lib/format/money';
import { consultationPage } from '../../content/consultation-page';
import type { Treatment } from '../../types/consultation.types';

/** The billable procedures, with the total the bill will be raised for on close. */
export function TreatmentList({ treatments, totalSatang }: { treatments: Treatment[]; totalSatang: number }) {
  const text = consultationPage.treatments;

  if (treatments.length === 0) return <p className="text-sm text-ink-500">{text.none}</p>;

  return (
    <table className="w-full text-sm">
      <tbody className="divide-y divide-ink-100">
        {treatments.map((treatment) => (
          <tr key={treatment.id}>
            <td className="py-2 pr-3">
              <span className="font-medium text-ink-900">{treatment.name}</span>
              {treatment.note && <span className="block text-ink-600">{treatment.note}</span>}
            </td>
            <td className="py-2 pr-3 text-right tabular-nums text-ink-600">
              {treatment.quantity} × {formatBaht(treatment.unit_price_satang)}
            </td>
            <td className="py-2 text-right font-medium tabular-nums text-ink-900">{formatBaht(treatment.line_total_satang)}</td>
          </tr>
        ))}
      </tbody>
      <tfoot>
        <tr className="border-t border-ink-200">
          <td colSpan={2} className="py-2 pr-3 text-right text-ink-600">{text.total}</td>
          <td className="py-2 text-right font-semibold tabular-nums text-ink-900">{formatBaht(totalSatang)}</td>
        </tr>
      </tfoot>
    </table>
  );
}
