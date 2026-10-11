import type { Shift } from '../types/schedule.types';
import { clinicDate } from './clinic-clock';
import { touchesDay } from './roster-week';

/** The longest gap between two blocks still read as a break inside one shift rather than two shifts. */
const LONGEST_BREAK_MS = 3 * 60 * 60 * 1000;

/**
 * What one chip on the roster stands for: a single block, or the blocks of one shift with the
 * breaks between them. The backend stores a shift with a break as two blocks.
 */
export interface ShiftChip {
  /** The first block's id, steady across renders. */
  key: string;
  startsAt: string;
  endsAt: string;
  /** The gaps between the blocks, in order. Empty for a single block. */
  breaks: { startsAt: string; endsAt: string }[];
  /** The blocks themselves, earliest first: what editing and removing act on. */
  blocks: Shift[];
}

/** True when `next` reads as the same shift as `previous` after a break: same branch, same day, a short gap. */
function continues(previous: Shift, next: Shift): boolean {
  const gap = Date.parse(next.starts_at) - Date.parse(previous.ends_at);

  return (
    gap > 0 &&
    gap <= LONGEST_BREAK_MS &&
    previous.branch_id === next.branch_id &&
    clinicDate(previous.starts_at) === clinicDate(next.starts_at)
  );
}

/**
 * One person's chips for one clinic day. Blocks that clearly belong together, as a shift saved
 * with a break comes back, become one chip that names the break; anything else stays apart.
 */
export function shiftChips(shifts: Shift[], day: string): ShiftChip[] {
  const blocks = shifts
    .filter((shift) => touchesDay(shift.starts_at, shift.ends_at, day))
    .sort((a, b) => Date.parse(a.starts_at) - Date.parse(b.starts_at));
  const chips: ShiftChip[] = [];

  for (const block of blocks) {
    const last = chips.at(-1);
    const previous = last?.blocks.at(-1);

    if (last && previous && continues(previous, block)) {
      last.breaks.push({ startsAt: previous.ends_at, endsAt: block.starts_at });
      last.blocks.push(block);
      last.endsAt = block.ends_at;
    } else {
      chips.push({ key: block.id, startsAt: block.starts_at, endsAt: block.ends_at, breaks: [], blocks: [block] });
    }
  }

  return chips;
}
