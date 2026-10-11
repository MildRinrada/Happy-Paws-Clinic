import type { ReactNode } from 'react';
import { bookingFrame } from '../../content/booking-frame';

interface SummaryCardRowProps {
  label: string;
  /** Left out: the row says "ยังไม่ได้เลือก" in a quieter white. */
  value?: string;
  /** The day and time: larger, bold and yellow. */
  highlight?: boolean;
  /** A small yellow link at the right, such as "เปลี่ยน". */
  action?: ReactNode;
}

/** One line of the summary card: a small label over its value. */
export function SummaryCardRow({ label, value, highlight = false, action }: SummaryCardRowProps) {
  const look = !value ? 'text-[15px] text-white/60' : highlight ? 'text-lg font-bold text-site-sun' : 'text-[15px] font-medium';

  return (
    <div>
      <dt className="text-[13px] text-white/70">{label}</dt>
      <dd className="mt-1 flex items-baseline justify-between gap-3">
        <span className={`min-w-0 break-words ${look}`}>{value ?? bookingFrame.summary.notChosen}</span>
        {action}
      </dd>
    </div>
  );
}
