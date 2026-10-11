import { labPage } from '../content/lab-page';
import type { LabResultInput } from '../types/lab.types';
import type { ResultPanelId } from '../types/result-panel.types';
import { parseNumber, splitResultValue } from './result-value';

/** One row of the results form, as typed. */
export interface ResultDraft {
  key: string;
  analyte: string;
  value: string;
  unit: string;
  low: string;
  high: string;
  /** A range in words ("negative"), for a value that is not a number. Used instead of low and high. */
  rangeText: string;
  /** Which range boxes the row shows: two numbers, or words. Fixed for the row, so typing never swaps them. */
  rangeKind: 'numbers' | 'text';
  abnormal: boolean;
  /** The panel the row was filled from. Its name, unit and range are skipped by Tab, not locked. */
  panel?: ResultPanelId;
}

export const emptyResult = (): ResultDraft => ({
  key: crypto.randomUUID(),
  analyte: '',
  value: '',
  unit: '',
  low: '',
  high: '',
  rangeText: '',
  rangeKind: 'numbers',
  abnormal: false,
});

/**
 * A row nobody has filled in. A row from a panel counts as blank until it has a value, so the
 * values the analyser did not report are simply left empty and not sent.
 */
export const isBlankDraft = (draft: ResultDraft) =>
  draft.panel
    ? !draft.value.trim()
    : !draft.analyte.trim() && !draft.value.trim() && !draft.unit.trim() && !draft.low.trim() && !draft.high.trim() && !draft.rangeText.trim();

/** True once anything has been typed: the refresh waits while it is (see `LabRefreshProvider`). */
export const hasTypedValues = (drafts: ResultDraft[]) => drafts.some((draft) => draft.value.trim() !== '');

export type DraftCheck =
  | { ok: true; results: LabResultInput[] }
  | { ok: false; rowErrors: Record<string, string>; message?: string };

/**
 * Turns the rows into what the API takes, or says which row is wrong. Blank rows are skipped.
 * A row with no value is left for the backend to refuse, in its own words.
 */
export function checkResultDrafts(drafts: ResultDraft[]): DraftCheck {
  const text = labPage.resultForm;
  const filled = drafts.filter((draft) => !isBlankDraft(draft));
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
      reference_text: draft.rangeText.trim() || undefined,
      abnormal: draft.abnormal || undefined,
    };
  });

  return Object.keys(rowErrors).length > 0 ? { ok: false, rowErrors } : { ok: true, results };
}
