import Link from 'next/link';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { formatTime } from '@/lib/format/datetime';
import type { AttendanceRiskRow as Row } from '../../types/attendance-risk.types';
import { RiskBadge } from './risk-badge';

/** One appointment at risk: when, whose, and why. */
export function AttendanceRiskRow({ row }: { row: Row }) {
  return (
    <li className="space-y-1 py-2.5">
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-sm font-semibold tabular-nums text-ink-900">{formatTime(row.starts_at)}</span>
        <span className="font-medium text-ink-900">{row.pet_name}</span>
        <Link href={customerPaths.detail(row.customer_id)} className="text-sm text-brand-700 hover:underline">
          {row.customer_name}
        </Link>
        <RiskBadge risk={row} />
      </div>
      <ul className="list-disc space-y-0.5 pl-5 text-sm text-ink-700">
        {row.reasons.map((reason) => (
          <li key={reason}>{reason}</li>
        ))}
      </ul>
    </li>
  );
}
