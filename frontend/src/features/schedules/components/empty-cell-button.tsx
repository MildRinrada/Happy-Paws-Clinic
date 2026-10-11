'use client';

import { schedulePage } from '../content/schedule-page';
import { dayLabel } from '../lib/chip-label';
import { useRosterDialogs } from './dialogs/roster-dialogs-context';

interface EmptyCellButtonProps {
  staffId: string;
  name: string;
  day: string;
}

/**
 * The "+" in an empty roster cell: opens "ลงเวร" with this person and day already chosen. Renders
 * nothing for a session that may not change the roster.
 */
export function EmptyCellButton({ staffId, name, day }: EmptyCellButtonProps) {
  const open = useRosterDialogs();

  if (!open) return null;

  return (
    <button
      type="button"
      aria-label={schedulePage.addHere(name, dayLabel(day))}
      className="flex h-7 w-full items-center justify-center rounded-md text-base text-ink-300 hover:bg-brand-50 hover:text-brand-700 focus-visible:outline-2 focus-visible:outline-brand-500"
      onClick={() => open({ kind: 'addShift', staffId, day })}
    >
      <span aria-hidden="true">+</span>
    </button>
  );
}
