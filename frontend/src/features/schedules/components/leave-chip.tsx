import { schedulePage } from '../content/schedule-page';
import type { TimeOff } from '../types/schedule.types';

/** "ลา" on a day any of which is taken by leave, with the reason when one was given. */
export function LeaveChip({ entry }: { entry: TimeOff }) {
  return (
    <span title={entry.reason} className="block rounded-md bg-amber-100 px-1.5 py-0.5 text-xs text-amber-900">
      {schedulePage.leave}
      {entry.reason && <span className="block truncate text-[11px] text-amber-800/80">{entry.reason}</span>}
    </span>
  );
}
