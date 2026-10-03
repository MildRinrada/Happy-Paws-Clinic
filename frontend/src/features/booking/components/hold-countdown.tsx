'use client';

import { useEffect, useState } from 'react';

/**
 * Minutes and seconds until the hold lapses, ticking. Calls `onExpire` once when it reaches zero,
 * so the form can stop the owner confirming a time that is no longer kept for them.
 */
export function HoldCountdown({ expiresAt, onExpire }: { expiresAt: string; onExpire: () => void }) {
  const deadline = Date.parse(expiresAt);
  const [now, setNow] = useState(() => Date.now());
  const left = Math.max(0, Math.floor((deadline - now) / 1000));

  useEffect(() => {
    if (left === 0) {
      onExpire();
      return;
    }
    const timer = window.setTimeout(() => setNow(Date.now()), 1000);
    return () => window.clearTimeout(timer);
  }, [left, onExpire]);

  const minutes = Math.floor(left / 60);
  const seconds = String(left % 60).padStart(2, '0');

  return (
    <span className="font-mono font-semibold tabular-nums" aria-live="off">
      {minutes}:{seconds}
    </span>
  );
}
