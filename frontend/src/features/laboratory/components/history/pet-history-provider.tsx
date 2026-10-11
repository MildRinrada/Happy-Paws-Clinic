'use client';

import { useMemo, useRef, type ReactNode } from 'react';
import { petLabHistoryAction } from '../../actions/history-action';
import { PetHistoryContext } from '../../lib/pet-history';
import type { PastRequest } from '../../lib/previous-values';

/**
 * Keeps each pet's lab history once it has been read, for the "ครั้งก่อน" column. Two rows of the
 * same pet share one read, and a read that failed is not retried until the page is opened again:
 * the column simply stays empty, the results beside it are what matters.
 */
export function PetHistoryProvider({ children }: { children: ReactNode }) {
  const reads = useRef(new Map<string, Promise<PastRequest[] | null>>());

  const cache = useMemo(
    () => ({
      load: (petId: string) => {
        let read = reads.current.get(petId);
        if (!read) {
          read = petLabHistoryAction({ petId })
            .then((result) => (result.ok ? result.data : null))
            .catch(() => null);
          reads.current.set(petId, read);
        }
        return read;
      },
    }),
    [],
  );

  return <PetHistoryContext.Provider value={cache}>{children}</PetHistoryContext.Provider>;
}
