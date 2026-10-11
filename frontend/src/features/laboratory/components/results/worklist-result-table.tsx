'use client';

import { useMemo } from 'react';
import { usePetHistory } from '../../lib/pet-history';
import { previousValuesFor, type PreviousValues } from '../../lib/previous-values';
import { useRowExpanded } from '../../lib/row-expanded';
import type { LabRequest } from '../../types/lab.types';
import { LabResultTable } from '../lab-result-table';

const none: PreviousValues = {};

/**
 * The result table on the worklist, with "ครั้งก่อน" filled from the pet's history once the row is
 * open. The history is read once per pet (`PetHistoryProvider`), so a closed row costs nothing.
 */
export function WorklistResultTable({ request, canCorrect }: { request: LabRequest; canCorrect: boolean }) {
  const expanded = useRowExpanded();
  const history = usePetHistory(request.pet_id, expanded && request.results.length > 0);
  const previous = useMemo(() => (history ? previousValuesFor(request, history) : none), [history, request]);

  return (
    <LabResultTable
      results={request.results}
      previous={previous}
      correction={canCorrect ? { labRequestId: request.id, version: request.version } : undefined}
    />
  );
}
