import type { DueBackReason } from '../types/due-back.types';

const REASONS: DueBackReason[] = ['vaccine_overdue', 'lapsed'];

type SearchParams = Record<string, string | string[] | undefined>;

/** `?reason=` of the due-back list; vaccines first when it is missing or unknown. */
export function readDueBackReason(params: SearchParams): DueBackReason {
  const value = Array.isArray(params.reason) ? params.reason[0] : params.reason;

  return REASONS.find((reason) => reason === value) ?? 'vaccine_overdue';
}

export const DUE_BACK_REASONS = REASONS;
