import type { NextFreeSlot, NextFreeTimes } from '../types/next-free-time.types';

/**
 * Each branch's earliest time still ahead of `now`, earliest branch first. A branch with nothing
 * left is left out; an empty answer means nothing is free today or tomorrow anywhere.
 *
 * The times were read up to a minute ago, so this runs every time the block is drawn, on the
 * server and again in the browser, and a time already past is never offered.
 */
export function pickNextFree(data: NextFreeTimes, now: number): NextFreeSlot[] {
  return data.branches
    .flatMap((branch) => {
      const next = branch.times.find((time) => Date.parse(time) > now);
      return next ? [{ branchId: branch.branchId, branchName: branch.branchName, startsAt: next }] : [];
    })
    .sort((a, b) => Date.parse(a.startsAt) - Date.parse(b.startsAt));
}
