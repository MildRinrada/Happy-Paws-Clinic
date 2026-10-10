'use client';

import { useRouter } from 'next/navigation';
import { useCallback, useEffect } from 'react';
import { queueDisplay } from '../../content/queue-display';
import { useQueueStream, type StreamState } from '../../hooks/use-queue-stream';

/** A full re-read once a minute even when nothing streams in: the safety net under the stream. */
const SAFETY_REFRESH_MS = 60_000;

const dots: Record<StreamState, string> = {
  connecting: 'bg-ink-400',
  live: 'bg-emerald-400',
  reconnecting: 'bg-amber-400',
};

/**
 * Keeps the waiting-room screen current, and keeps it on.
 *
 * - Every stream event re-runs the page on the server (`router.refresh()`), exactly as the board does.
 * - A re-read every minute catches what a silently stalled stream would miss, rolls the screen
 *   over to the new day at midnight, and keeps the session in use so it slides forward rather
 *   than lapsing. If the session has ended, that re-read is what takes the screen to sign-in
 *   instead of leaving yesterday's numbers up.
 * - A screen wake lock, where the browser has one, stops the TV from dimming. It is released by
 *   the browser when the tab is hidden, so it is asked for again when the tab comes back.
 */
export function DisplayLive({ branchId, streamPath }: { branchId: string; streamPath?: string }) {
  const router = useRouter();
  const refresh = useCallback(() => router.refresh(), [router]);
  const state = useQueueStream(branchId, refresh, streamPath);

  useEffect(() => {
    const timer = setInterval(refresh, SAFETY_REFRESH_MS);
    return () => clearInterval(timer);
  }, [refresh]);

  useEffect(() => {
    let lock: WakeLockSentinel | undefined;

    const acquire = async () => {
      if (document.visibilityState !== 'visible' || !('wakeLock' in navigator)) {
        return;
      }
      try {
        lock = await navigator.wakeLock.request('screen');
      } catch {
        // Refused (battery saver, or no permission). The screen still works; it may just dim.
      }
    };

    void acquire();
    document.addEventListener('visibilitychange', acquire);

    return () => {
      document.removeEventListener('visibilitychange', acquire);
      void lock?.release();
    };
  }, []);

  return (
    <p role="status" className="flex items-center gap-[1vh] text-[clamp(0.875rem,2vh,1.5rem)] text-ink-300">
      <span aria-hidden className={`size-[1.4vh] min-h-2 min-w-2 rounded-full ${dots[state]}`} />
      {queueDisplay.status[state]}
    </p>
  );
}
