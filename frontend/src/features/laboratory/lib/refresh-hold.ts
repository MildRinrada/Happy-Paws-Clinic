import { createContext, useContext, useEffect, useId } from 'react';

/** Lets a form on the worklist ask the auto refresh to wait. */
export interface RefreshHold {
  hold: (key: string, held: boolean) => void;
}

export const RefreshHoldContext = createContext<RefreshHold | null>(null);

/**
 * Holds the worklist's auto refresh while `active`: a results form with something typed, an open
 * dialog. Outside the worklist (the consultation page has no auto refresh) it does nothing.
 */
export function useRefreshHold(active: boolean) {
  const context = useContext(RefreshHoldContext);
  const key = useId();

  useEffect(() => {
    if (!context) return;
    context.hold(key, active);
    return () => context.hold(key, false);
  }, [context, key, active]);
}
