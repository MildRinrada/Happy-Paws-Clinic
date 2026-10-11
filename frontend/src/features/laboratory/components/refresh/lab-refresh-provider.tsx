'use client';

import { useCallback, useMemo, useState, type ReactNode } from 'react';
import { AutoRefresh } from '@/features/analytics/components/today/auto-refresh';
import { labTiming } from '../../content/lab-timing';
import { RefreshHoldContext } from '../../lib/refresh-hold';

/**
 * Reads the worklist again every minute while it is on screen, the dashboard's way.
 *
 * A refresh re-renders the rows on the server and keeps everything on the client: a row someone
 * opened stays open, by its request id, and a form keeps what was typed. The one thing a refresh
 * can still do is take a row away (another member read the results, or entered them), and a row
 * that goes takes its form with it. So the refresh waits while any form has a typed value or a
 * dialog is open (`useRefreshHold`), and goes on at the next tick once they are done.
 */
export function LabRefreshProvider({ children }: { children: ReactNode }) {
  const [held, setHeld] = useState<ReadonlySet<string>>(() => new Set());

  const hold = useCallback((key: string, on: boolean) => {
    setHeld((current) => {
      if (on === current.has(key)) return current;
      const next = new Set(current);
      if (on) next.add(key);
      else next.delete(key);
      return next;
    });
  }, []);

  const value = useMemo(() => ({ hold }), [hold]);

  return (
    <RefreshHoldContext.Provider value={value}>
      <AutoRefresh seconds={labTiming.refreshSeconds} paused={held.size > 0} />
      {children}
    </RefreshHoldContext.Provider>
  );
}
