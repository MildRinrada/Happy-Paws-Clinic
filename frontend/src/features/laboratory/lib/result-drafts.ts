import { labPage } from '../content/lab-page';
import type { LabResultInput } from '../types/lab.types';
import { parseNumber, splitResultValue } from './result-value';

/** One row of the results form, as typed. */
export interface ResultDraft {
  key: string;
  analyte: string;
  value: string;
  unit: string;
  low: string;
  high: string;
  abnormal: boolean;
}

export const emptyResult = (): ResultDraft => ({
  key: crypto.randomUUID(),
  analyte: '',
  value: '',
  unit: '',
  low: '',
  high: '',
  abnormal: false,
});

const isBlank = (draft: ResultDraft) => !draft.analyte.trim() && !draft.value.trim() && !draft.unit.trim() && !draft.low.trim() && !draft.high.trim();

export type DraftCheck =
  | { ok: true; results: LabResultInput[] }
  | { ok: false; rowErrors: Record<string, string>; message?: string };

/**
 * Turns the rows into what the API takes, or says which row is wrong. Untouched rows are skipped.
 * A row with no value is left for the backend to refuse, in its own words.
 */
export function checkResultDrafts(drafts: ResultDraft[]): DraftCheck {
  const text = labPage.resultForm;
  const filled = drafts.filter((draft) => !isBlank(draft));
  const rowErrors: Record<string, string> = {};

  if (filled.length === 0) return { ok: false, rowErrors, message: text.empty };

  const results = filled.map((draft): LabResultInput => {
    const low = parseNumber(draft.low);
    const high = parseNumber(draft.high);

    if (!draft.analyte.trim()) rowErrors[draft.key] = text.noAnalyte;
    else if ((draft.low.trim() && low === undefined) || (draft.high.trim() && high === undefined)) rowErrors[draft.key] = text.badRange;

    return {
      analyte: draft.analyte.trim(),
      ...splitResultValue(draft.value),
      unit: draft.unit.trim() || undefined,
      reference_low: low,
      reference_high: high,
      abnormal: draft.abnormal || undefined,
    };
  });

  return Object.keys(rowErrors).length > 0 ? { ok: false, rowErrors } : { ok: true, results };
}
