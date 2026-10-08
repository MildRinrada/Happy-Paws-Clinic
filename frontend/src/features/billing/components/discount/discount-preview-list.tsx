import { formatBaht } from '@/lib/format/money';
import { discountForm } from '../../content/discount-form';

interface Props {
  amountSatang: number;
  totalSatang: number;
  balanceSatang: number;
}

/** What the bill would look like after this discount. A preview; the bill keeps the backend's figures. */
export function DiscountPreviewList({ amountSatang, totalSatang, balanceSatang }: Props) {
  const text = discountForm.preview;
  const rows = [
    { label: text.discount, value: `− ${formatBaht(amountSatang)}` },
    { label: text.newTotal, value: formatBaht(totalSatang - amountSatang), strong: true },
    { label: text.newBalance, value: formatBaht(balanceSatang - amountSatang) },
  ];

  return (
    <dl className="space-y-1 rounded-lg bg-ink-50 px-3 py-2 text-sm">
      {rows.map((row) => (
        <div key={row.label} className={`flex justify-between gap-4 ${row.strong ? 'font-semibold text-ink-900' : 'text-ink-700'}`}>
          <dt>{row.label}</dt>
          <dd className="tabular-nums">{row.value}</dd>
        </div>
      ))}
    </dl>
  );
}
