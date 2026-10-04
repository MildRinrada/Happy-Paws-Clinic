'use client';

import { useCallback, useEffect, useState } from 'react';

/**
 * A countdown in whole seconds, for a button that must wait before it can be pressed again.
 *
 * Counts against the clock rather than adding up ticks, so a phone that sleeps with the tab in the
 * background wakes up with the right number, not the one it fell asleep on.
 */
export function useCooldown(seconds: number): { remaining: number; start: () => void } {
  const [until, setUntil] = useState<number | null>(null);
  const [remaining, setRemaining] = useState(0);

  const start = useCallback(() => setUntil(Date.now() + seconds * 1000), [seconds]);

  useEffect(() => {
    if (until === null) {
      return;
    }

    const tick = () => {
      const left = Math.max(0, Math.ceil((until - Date.now()) / 1000));
      setRemaining(left);
      if (left === 0) setUntil(null);
    };

    tick();
    const timer = window.setInterval(tick, 1000);
    return () => window.clearInterval(timer);
  }, [until]);

  return { remaining, start };
}
