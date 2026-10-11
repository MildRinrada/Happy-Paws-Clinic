'use client';

import { schedulePage } from '../content/schedule-page';
import { chipLabel } from '../lib/chip-label';
import { clinicClock } from '../lib/clinic-clock';
import type { ShiftChip } from '../lib/shift-chips';
import { useRosterDialogs } from './dialogs/roster-dialogs-context';

interface RosterShiftChipProps {
  chip: ShiftChip;
  staffId: string;
  name: string;
  /** The roster day the chip sits on. */
  day: string;
  /** False once the shift is over: history is not changed, so it is not offered. */
  editable: boolean;
}

const chipClass = 'block w-full rounded-md bg-brand-50 px-1.5 py-0.5 text-left text-xs tabular-nums text-brand-800 ring-1 ring-inset ring-brand-100';

/**
 * One shift on the roster: its hours, and under them each break when the shift was saved with one.
 * For a session that may change the roster it is a button that opens "แก้หรือลบเวร".
 */
export function RosterShiftChip({ chip, staffId, name, day, editable }: RosterShiftChipProps) {
  const open = useRosterDialogs();
  const content = (
    <>
      <span className="block whitespace-nowrap">
        {clinicClock(chip.startsAt)}-{clinicClock(chip.endsAt)}
      </span>
      {chip.breaks.map((gap) => (
        <span key={gap.startsAt} className="block whitespace-nowrap text-[11px] text-brand-700/80">
          {schedulePage.breakNote(clinicClock(gap.startsAt), clinicClock(gap.endsAt))}
        </span>
      ))}
    </>
  );

  if (!open || !editable) {
    return <span className={chipClass}>{content}</span>;
  }

  return (
    <button
      type="button"
      aria-label={schedulePage.editHere(name, chipLabel(chip))}
      className={`${chipClass} hover:bg-brand-100 focus-visible:outline-2 focus-visible:outline-brand-500`}
      onClick={() => open({ kind: 'editShift', staffId, day, chip })}
    >
      {content}
    </button>
  );
}
