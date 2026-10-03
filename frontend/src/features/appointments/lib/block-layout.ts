import type { CalendarItem } from '../types/calendar.types';
import { clinicMinutes } from './calendar-day';

export interface PlacedItem {
  item: CalendarItem;
  start: number;
  end: number;
  /** Side by side when two appointments overlap in one column: which slot, out of how many. */
  lane: number;
  lanes: number;
}

/**
 * Places one column's appointments on the grid. Overlaps happen (a walk-in squeezed in, a
 * cancelled booking under its replacement), so overlapping blocks share the width instead of
 * hiding each other. Each group of blocks that overlap one another shares one lane count.
 */
export function placeColumn(items: CalendarItem[]): PlacedItem[] {
  const sorted = items
    .map((item) => ({ item, start: clinicMinutes(item.appointment.starts_at), end: clinicMinutes(item.appointment.ends_at) }))
    .map((entry) => ({ ...entry, end: Math.max(entry.end, entry.start + 15) }))
    .sort((a, b) => a.start - b.start || a.end - b.end);

  const placed: PlacedItem[] = [];
  let group: PlacedItem[] = [];
  let groupEnd = -1;

  const closeGroup = () => {
    const lanes = Math.max(1, ...group.map((entry) => entry.lane + 1));
    for (const entry of group) entry.lanes = lanes;
    placed.push(...group);
    group = [];
  };

  for (const entry of sorted) {
    if (entry.start >= groupEnd && group.length > 0) closeGroup();

    const laneEnds = new Map<number, number>();
    for (const other of group) laneEnds.set(other.lane, Math.max(laneEnds.get(other.lane) ?? 0, other.end));

    let lane = 0;
    while ((laneEnds.get(lane) ?? -1) > entry.start) lane += 1;

    group.push({ ...entry, lane, lanes: 1 });
    groupEnd = Math.max(groupEnd, entry.end);
  }

  if (group.length > 0) closeGroup();

  return placed;
}
