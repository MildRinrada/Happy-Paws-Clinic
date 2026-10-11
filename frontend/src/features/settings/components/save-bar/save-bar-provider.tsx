'use client';

import { useCallback, useMemo, useRef, useState, type ReactNode } from 'react';
import { saveBarText } from '../../content/save-bar';
import { useLeaveGuard } from '../../hooks/use-leave-guard';
import { sameValue } from '../../lib/same-value';
import { SaveBarActionsContext, SaveBarEntriesContext, SaveBarResetContext, type SaveBarEntry } from './save-bar-context';

/**
 * Holds what every form in the settings area has said about itself, for the one save bar at the
 * bottom of the page, and asks before the page is left while any of them has unsaved changes.
 */
export function SaveBarProvider({ children }: { children: ReactNode }) {
  const [entries, setEntries] = useState<Record<string, SaveBarEntry>>({});
  const resets = useRef(new Map<string, () => void>());

  const register = useCallback((entry: SaveBarEntry, reset: () => void) => {
    resets.current.set(entry.formId, reset);
    setEntries((current) => (sameValue(current[entry.formId], entry) ? current : { ...current, [entry.formId]: entry }));
  }, []);

  const unregister = useCallback((formId: string) => {
    resets.current.delete(formId);
    setEntries((current) => {
      if (!(formId in current)) return current;
      const rest = { ...current };
      delete rest[formId];
      return rest;
    });
  }, []);

  const list = useMemo(() => Object.values(entries), [entries]);
  const unsaved = list.some((entry) => entry.dirty);

  useLeaveGuard(unsaved, saveBarText.leave);

  const actions = useMemo(
    () => ({
      available: true,
      register,
      unregister,
      confirmLeave: () => !unsaved || window.confirm(saveBarText.leave),
    }),
    [register, unregister, unsaved],
  );

  const reset = useCallback((formIds: string[]) => formIds.forEach((formId) => resets.current.get(formId)?.()), []);

  return (
    <SaveBarActionsContext.Provider value={actions}>
      <SaveBarResetContext.Provider value={reset}>
        <SaveBarEntriesContext.Provider value={list}>{children}</SaveBarEntriesContext.Provider>
      </SaveBarResetContext.Provider>
    </SaveBarActionsContext.Provider>
  );
}
