'use client';

import { useEffect, useState, type ReactNode } from 'react';
import { refreshNavCountsAction } from '@/features/staff-shell/actions/nav-counts-action';
import type { NavCounts } from '@/features/staff-shell/types/staff-shell.types';
import { NavCountsContext } from './nav-counts-context';

/** The badges are read again this often while the console is on screen. */
const REFRESH_SECONDS = 60;

/**
 * Holds the menu's badges. The first counts arrive as a promise the server started while drawing
 * the page, so the frame is on screen before they are: the menu shows without badges for a moment
 * rather than the page waiting for three extra reads.
 *
 * After that the counts are read again once a minute, but only while the tab is visible: a console
 * left open in a background tab all day should not keep asking the API. Coming back to the tab
 * reads once straight away, then the timer starts over, as the overview's own refresh does.
 */
export function NavCountsProvider({ initial, children }: { initial: Promise<NavCounts>; children: ReactNode }) {
  const [counts, setCounts] = useState<NavCounts>({});

  // A new promise arrives whenever the layout is drawn again (a refresh, a branch change).
  useEffect(() => {
    let live = true;

    // The promise React hands over is a bare thenable whose `then` returns nothing to chain on.
    Promise.resolve(initial)
      .then((value) => live && setCounts(value))
      .catch(() => undefined);

    return () => {
      live = false;
    };
  }, [initial]);

  useEffect(() => {
    let timer: number | undefined;
    let live = true;

    const read = () => {
      refreshNavCountsAction()
        .then((value) => live && setCounts(value))
        // A failed refresh keeps the last counts: they are at most a minute old.
        .catch(() => undefined);
    };
    const start = () => {
      window.clearInterval(timer);
      timer = window.setInterval(read, REFRESH_SECONDS * 1000);
    };
    const stop = () => {
      window.clearInterval(timer);
      timer = undefined;
    };
    const onVisibility = () => {
      if (document.visibilityState === 'visible') {
        read();
        start();
      } else {
        stop();
      }
    };

    if (document.visibilityState === 'visible') start();
    document.addEventListener('visibilitychange', onVisibility);

    return () => {
      live = false;
      stop();
      document.removeEventListener('visibilitychange', onVisibility);
    };
  }, []);

  return <NavCountsContext.Provider value={counts}>{children}</NavCountsContext.Provider>;
}
