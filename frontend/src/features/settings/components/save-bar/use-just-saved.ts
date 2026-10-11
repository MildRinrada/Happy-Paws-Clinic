'use client';

import { useEffect, useRef, useState } from 'react';

const SHOWN_MS = 3000;

/**
 * True for a few seconds after the bar's forms finish saving with nothing left unsaved, so the bar
 * can say so before it goes. Starts the moment `pending` turns false.
 */
export function useJustSaved(pending: boolean, settled: boolean) {
  const [shown, setShown] = useState(false);
  const wasPending = useRef(false);

  useEffect(() => {
    if (wasPending.current && !pending && settled) setShown(true);
    wasPending.current = pending;
  }, [pending, settled]);

  useEffect(() => {
    if (!shown) return;
    const timer = window.setTimeout(() => setShown(false), SHOWN_MS);
    return () => window.clearTimeout(timer);
  }, [shown]);

  return shown && settled;
}
