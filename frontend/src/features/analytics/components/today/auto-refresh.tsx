'use client';

import { useRouter } from 'next/navigation';
import { useEffect, useRef } from 'react';

/**
 * Reads the page again every `seconds` while it is on screen, so the counter's numbers move
 * without anyone pressing reload. While the tab is hidden the timer stops: a dashboard left open
 * in a background tab all day should not keep asking the clinic's API. Coming back to the tab
 * reads once straight away, then the timer starts over.
 *
 * `router.refresh()` re-runs the server component and keeps everything on the client as it was,
 * including an open "รายละเอียดวันนี้" and the scroll position.
 *
 * `paused` skips the reads while it is true, without restarting the timer: the lab worklist sets it
 * while someone is typing results, so a row the refresh would take away never takes the typing
 * with it. The next tick after it clears reads as usual.
 */
export function AutoRefresh({ seconds, paused = false }: { seconds: number; paused?: boolean }) {
  const router = useRouter();
  const pausedRef = useRef(paused);

  useEffect(() => {
    pausedRef.current = paused;
  }, [paused]);

  useEffect(() => {
    let timer: number | undefined;
    const refresh = () => {
      if (!pausedRef.current) router.refresh();
    };

    const start = () => {
      window.clearInterval(timer);
      timer = window.setInterval(refresh, seconds * 1000);
    };
    const stop = () => {
      window.clearInterval(timer);
      timer = undefined;
    };
    const onVisibility = () => {
      if (document.visibilityState === 'visible') {
        refresh();
        start();
      } else {
        stop();
      }
    };

    if (document.visibilityState === 'visible') start();
    document.addEventListener('visibilitychange', onVisibility);

    return () => {
      stop();
      document.removeEventListener('visibilitychange', onVisibility);
    };
  }, [router, seconds]);

  return null;
}
