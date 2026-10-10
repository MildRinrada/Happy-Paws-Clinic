import { emergencyPage } from '../content/emergency-page';
import { openingDay } from '../lib/opening-day';
import type { BranchOpenStatus } from '../types/emergency.types';

/** One branch: open or closed now, and until when or from when. */
export function OpenStatusCard({ status }: { status: BranchOpenStatus }) {
  const text = emergencyPage.status;
  const { state } = status;
  const open = state.kind === 'open';

  const detail =
    state.kind === 'open'
      ? text.closesAt(state.closes)
      : state.kind === 'opens-later'
        ? text.opensAt(openingDay(state.day, state.offset), state.opens)
        : text.noOpening;

  return (
    <li className="rounded-2xl border border-site-border/60 bg-white p-5">
      {status.branchName && <p className="mb-2 font-bold text-ink-900">{status.branchName}</p>}
      <p className="flex flex-wrap items-center gap-x-3 gap-y-2">
        <span
          className={`inline-flex h-7 items-center gap-1.5 rounded-full px-3 text-sm font-bold text-white ${open ? 'bg-stat-green' : 'bg-site-alert'}`}
        >
          <span aria-hidden="true" className="size-2 rounded-full bg-white" />
          {open ? text.open : text.closed}
        </span>
        <span className="text-[15px] font-semibold text-ink-900">{detail}</span>
      </p>
      {status.holiday && <p className="mt-2 text-sm text-site-muted">{text.holiday(status.holiday)}</p>}
    </li>
  );
}
