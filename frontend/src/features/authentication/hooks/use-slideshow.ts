'use client';

import { useEffect, useState } from 'react';

/**
 * Which slide is showing, moving on by itself every `intervalMs`.
 *
 * It holds still while `paused` (the pointer or keyboard focus is on the slideshow, so text being
 * read does not slide away), and for anyone whose system asks for reduced motion. Choosing a slide
 * by hand restarts the wait, so the chosen slide gets its full time.
 */
export function useSlideshow(count: number, intervalMs: number, paused: boolean) {
  const [index, setIndex] = useState(0);
  const [reducedMotion, setReducedMotion] = useState(false);

  useEffect(() => {
    const query = window.matchMedia('(prefers-reduced-motion: reduce)');
    const update = () => setReducedMotion(query.matches);

    update();
    query.addEventListener('change', update);
    return () => query.removeEventListener('change', update);
  }, []);

  useEffect(() => {
    if (paused || reducedMotion || count < 2) {
      return;
    }

    const timer = window.setTimeout(() => setIndex((current) => (current + 1) % count), intervalMs);
    return () => window.clearTimeout(timer);
  }, [index, paused, reducedMotion, count, intervalMs]);

  return { index, goTo: setIndex };
}
