import { createContext, useContext, useEffect, useState } from 'react';
import type { PastRequest } from './previous-values';

/** One read per pet for the whole visit to the page, shared by every row of that pet. */
export interface PetHistoryCache {
  load: (petId: string) => Promise<PastRequest[] | null>;
}

export const PetHistoryContext = createContext<PetHistoryCache | null>(null);

/**
 * A pet's lab history, read the first time `enabled` is true and kept after that: a row asks only
 * once it is open and has results, and the cache survives the auto refresh, so the column costs
 * one read per pet on screen. `undefined` while loading or without a cache; `null` when it failed.
 */
export function usePetHistory(petId: string, enabled: boolean): PastRequest[] | null | undefined {
  const cache = useContext(PetHistoryContext);
  const [history, setHistory] = useState<PastRequest[] | null>();

  useEffect(() => {
    if (!cache || !enabled || history !== undefined) return;

    let live = true;
    cache.load(petId).then((loaded) => {
      if (live) setHistory(loaded);
    });

    return () => {
      live = false;
    };
  }, [cache, enabled, history, petId]);

  return history;
}
