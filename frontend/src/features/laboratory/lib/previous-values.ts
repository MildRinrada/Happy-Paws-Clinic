import type { LabRequest, LabResult } from '../types/lab.types';

/** The part of a past request the "ครั้งก่อน" column reads. */
export type PastRequest = Pick<LabRequest, 'id' | 'requested_at'> & {
  results: Pick<LabResult, 'analyte' | 'value_numeric' | 'value_text' | 'unit' | 'voided'>[];
};

/** The same value from an earlier request, as the column shows it. */
export interface PreviousValue {
  /** "2.1", "negative". */
  text: string;
  numeric?: number;
  unit?: string;
}

/** By analyte, lower case. */
export type PreviousValues = Record<string, PreviousValue>;

export const analyteKey = (analyte: string) => analyte.trim().toLowerCase();

/** A value as a cell shows it: the number as the lab sent it, or the text. */
export const valueText = (value: Pick<LabResult, 'value_numeric' | 'value_text'>) =>
  value.value_numeric !== undefined ? String(value.value_numeric) : value.value_text ?? '';

/**
 * For each value on a request, the same analyte from the pet's most recent earlier request that
 * measured it. Voided values are not results and are skipped on both sides. Requests are ordered
 * by when they were sent, so a result entered late still compares with the visit before it.
 */
export function previousValuesFor(request: PastRequest, history: PastRequest[]): PreviousValues {
  const sent = Date.parse(request.requested_at);
  const earlier = history
    .filter((past) => past.id !== request.id && Date.parse(past.requested_at) < sent)
    .sort((first, second) => Date.parse(second.requested_at) - Date.parse(first.requested_at));

  const found: PreviousValues = {};

  for (const result of request.results) {
    const key = analyteKey(result.analyte);
    if (result.voided || found[key]) continue;

    for (const past of earlier) {
      const match = past.results.find((value) => !value.voided && analyteKey(value.analyte) === key);
      if (match) {
        found[key] = { text: valueText(match), numeric: match.value_numeric, unit: match.unit };
        break;
      }
    }
  }

  return found;
}

/** Up, down or the same, when both are numbers in the same unit; nothing to say otherwise. */
export function trendOf(result: Pick<LabResult, 'value_numeric' | 'unit'>, previous: PreviousValue): 'up' | 'down' | 'same' | undefined {
  if (result.value_numeric === undefined || previous.numeric === undefined) return undefined;
  if ((result.unit ?? '') !== (previous.unit ?? '')) return undefined;
  if (result.value_numeric > previous.numeric) return 'up';
  if (result.value_numeric < previous.numeric) return 'down';

  return 'same';
}
