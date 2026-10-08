import { formatBaht } from '@/lib/format/money';
import { noShowText } from '../../content/advanced-booking';
import type { CustomerAttendance } from '../../types/appointment.types';

/**
 * What the no-show did to the customer's record, as the backend counted it. A fee is reported, not
 * charged: the counter decides whether to bill it, with the customer in front of them.
 */
export function NoShowOutcome({ attendance }: { attendance: CustomerAttendance }) {
  const fee = attendance.fee_due_satang;

  return (
    <div role="status" className="space-y-1.5 rounded-lg border border-ink-200 bg-ink-50/60 px-3.5 py-3 text-sm">
      <p className="font-medium text-ink-900">{noShowText.done}</p>
      <p className="text-ink-700">{noShowText.record(attendance.no_shows, attendance.late_cancellations)}</p>
      <p className="text-ink-700">
        {noShowText.strikes(attendance.strikes_in_window, attendance.window_days, attendance.free_allowance)}
      </p>
      {fee > 0 ? (
        <div className="rounded-md bg-amber-50 px-2.5 py-2 text-amber-900">
          <p className="font-semibold">{noShowText.fee(formatBaht(fee))}</p>
          <p className="text-xs">{noShowText.feeNote}</p>
        </div>
      ) : (
        <p className="text-ink-600">{noShowText.noFee}</p>
      )}
    </div>
  );
}
