'use client';

import { useState, useTransition } from 'react';
import { checkConflictsAction } from '../actions/advanced-booking-actions';
import { refusalText } from '../lib/refusal';
import type { ConflictReport } from '../types/advanced-booking.types';

export interface ConflictQuery {
  starts: string[];
  serviceId: string;
  branchId: string;
  vetId?: string;
  petId?: string;
}

/** The same question asked twice gives the same key; any change to the form makes the answer old. */
function keyOf(query: ConflictQuery): string {
  return JSON.stringify([query.starts, query.serviceId, query.branchId, query.vetId ?? '', query.petId ?? '']);
}

/**
 * Asks the backend what would go wrong with some times, without booking them. The answer is kept
 * with the question it answered, so a form that has changed since shows no stale verdict.
 */
export function useConflictCheck(current: ConflictQuery | null) {
  const [answer, setAnswer] = useState<{ key: string; reports: Map<number, ConflictReport> }>();
  const [error, setError] = useState<{ key: string; message: string }>();
  const [checking, startCheck] = useTransition();
  const currentKey = current ? keyOf(current) : '';

  function check(unreachable: string) {
    if (!current) return;
    const query = current;
    const key = keyOf(query);

    setError(undefined);
    startCheck(async () => {
      try {
        const result = await checkConflictsAction(query);

        if (!result.ok) {
          setError({ key, message: refusalText(result.error) });
          return;
        }

        const reports = new Map<number, ConflictReport>();
        query.starts.forEach((start, at) => reports.set(Date.parse(start), result.data[at]!));
        setAnswer({ key, reports });
      } catch {
        setError({ key, message: unreachable });
      }
    });
  }

  const fresh = answer && answer.key === currentKey ? answer.reports : undefined;

  return {
    check,
    checking,
    /** The report for each start (by its time in ms), when the last check matches the form. */
    reports: fresh,
    /** A check was made, but the form has changed since. */
    stale: Boolean(answer) && !fresh,
    error: error?.key === currentKey ? error.message : undefined,
  };
}
