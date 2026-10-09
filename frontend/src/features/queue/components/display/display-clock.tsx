'use client';

import { useEffect, useState } from 'react';
import { formatTime } from '@/lib/format/datetime';

/**
 * The wall clock in the screen's corner, in clinic time. Drawn only after mounting: the server's
 * minute and the TV's could differ, and a clock is not worth a hydration mismatch.
 */
export function DisplayClock() {
  const [now, setNow] = useState<string>();

  useEffect(() => {
    const tick = () => setNow(formatTime(new Date().toISOString()));

    tick();
    const timer = setInterval(tick, 10_000);

    return () => clearInterval(timer);
  }, []);

  return <span className="tabular-nums">{now}</span>;
}
