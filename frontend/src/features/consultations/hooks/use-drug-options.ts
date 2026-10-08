'use client';

import { useEffect, useRef, useState } from 'react';
import { searchDrugOptionsAction } from '@/features/prescriptions/actions/drug-option-actions';
import type { DrugOption } from '@/features/prescriptions/types/drug-option.types';

const DEBOUNCE_MS = 250;

export type DrugSearchState = 'idle' | 'searching' | 'done' | 'failed';

/**
 * Searches the clinic's medicines as the vet types, a short pause after the last key. Only the
 * newest search may answer: a slow reply for "amo" never overwrites the one for "amox".
 */
export function useDrugOptions(branchId: string) {
  const [options, setOptions] = useState<DrugOption[]>([]);
  const [state, setState] = useState<DrugSearchState>('idle');
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined);
  const latest = useRef(0);

  useEffect(() => () => clearTimeout(timer.current), []);

  function search(text: string) {
    clearTimeout(timer.current);
    const ticket = ++latest.current;
    const q = text.trim();

    if (q === '') {
      setOptions([]);
      setState('idle');
      return;
    }

    setState('searching');
    timer.current = setTimeout(async () => {
      try {
        const result = await searchDrugOptionsAction({ q, branchId });
        if (ticket !== latest.current) return;
        setOptions(result.ok ? result.data.data : []);
        setState(result.ok ? 'done' : 'failed');
      } catch {
        if (ticket !== latest.current) return;
        setOptions([]);
        setState('failed');
      }
    }, DEBOUNCE_MS);
  }

  function clear() {
    clearTimeout(timer.current);
    latest.current++;
    setOptions([]);
    setState('idle');
  }

  return { options, state, search, clear };
}
