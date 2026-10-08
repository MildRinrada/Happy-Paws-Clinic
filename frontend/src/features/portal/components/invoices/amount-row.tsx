import { formatBaht } from '@/lib/format/money';

interface AmountRowProps {
  label: string;
  satang: number;
  /** Shown as a deduction: "-฿100.00". */
  minus?: boolean;
  /** Larger and bold, for the total and what is still owed. */
  strong?: boolean;
  /** Amber, for an amount still owed. */
  due?: boolean;
}

/** One label and one amount on a line, the amount to the right. */
export function AmountRow({ label, satang, minus, strong, due }: AmountRowProps) {
  const tone = due ? 'text-amber-800' : 'text-ink-900';

  return (
    <div className={`flex justify-between gap-4 ${strong ? 'text-sm font-semibold' : 'text-sm'}`}>
      <dt className={strong ? tone : 'text-site-muted'}>{label}</dt>
      <dd className={`tabular-nums ${tone}`}>
        {minus ? '-' : ''}
        {formatBaht(satang)}
      </dd>
    </div>
  );
}
