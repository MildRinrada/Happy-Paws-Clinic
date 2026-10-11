'use client';

import { createContext, useContext } from 'react';
import type { ShiftChip } from '../../lib/shift-chips';

/** What a click on the roster asks for. Day and person come along when the click knows them. */
export type RosterDialogRequest =
  | { kind: 'addShift'; staffId?: string; day?: string }
  | { kind: 'timeOff' }
  | { kind: 'editShift'; staffId: string; day: string; chip: ShiftChip }
  | { kind: 'copyWeek' };

export const RosterDialogsContext = createContext<((request: RosterDialogRequest) => void) | null>(null);

/**
 * Opens one of the roster's dialogs, or `null` when there is no provider: a session without
 * `schedule:write` renders the roster without one, so nothing on it offers to change anything.
 */
export function useRosterDialogs() {
  return useContext(RosterDialogsContext);
}
