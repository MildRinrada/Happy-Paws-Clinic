'use client';

import { useEffect, useRef, useState } from 'react';
import { searchConsoleAction } from '@/features/staff-shell/actions/search-action';
import type { ConsoleSearchResults } from '@/features/staff-shell/types/staff-shell.types';

export type SearchStatus = 'idle' | 'short' | 'loading' | 'done' | 'failed';

/** A short pause in typing means the person has stopped; only then is the API asked. */
const DEBOUNCE_MS = 250;

/** Fewer letters than this match half the register, so nothing is asked. */
const MIN_LENGTH = 2;

/**
 * The search behind the top bar's box: asked through a server action once typing pauses, so the
 * staff token stays on the server. An answer that arrives after a newer question was asked is
 * dropped, so a slow reply can never replace the results for what is in the box now.
 */
export function useConsoleSearch(query: string, withPets: boolean) {
  const [results, setResults] = useState<ConsoleSearchResults | null>(null);
  const [status, setStatus] = useState<SearchStatus>('idle');
  const latest = useRef(0);

  useEffect(() => {
    const text = query.trim();
    const ticket = ++latest.current;

    if (text.length < MIN_LENGTH) {
      setResults(null);
      setStatus(text.length === 0 ? 'idle' : 'short');
      return;
    }

    setStatus('loading');

    const timer = window.setTimeout(() => {
      searchConsoleAction({ q: text, pets: withPets })
        .then((result) => {
          if (ticket !== latest.current) return;
          setResults(result.ok ? result.data : null);
          setStatus(result.ok ? 'done' : 'failed');
        })
        .catch(() => {
          if (ticket !== latest.current) return;
          setResults(null);
          setStatus('failed');
        });
    }, DEBOUNCE_MS);

    return () => window.clearTimeout(timer);
  }, [query, withPets]);

  return { results, status };
}
