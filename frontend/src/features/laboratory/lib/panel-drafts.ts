import { resultPanels } from '../content/result-panels';
import type { LabTest } from '../types/lab.types';
import type { PanelSpecies, ResultPanelId } from '../types/result-panel.types';
import { emptyResult, isBlankDraft, type ResultDraft } from './result-drafts';

const normal = (value: string) => value.trim().toLowerCase();

/** The panels a request's tests name, by code or by name, in the form's order. */
export function panelsForTests(tests: LabTest[]): ResultPanelId[] {
  return resultPanels
    .filter((panel) =>
      tests.some((test) => (test.code && panel.codes.includes(test.code.trim().toUpperCase())) || panel.names.includes(normal(test.name))),
    )
    .map((panel) => panel.id);
}

/** A panel's rows for one species. A species without ranges gets names and units, ranges blank. */
export function panelDrafts(id: ResultPanelId, species: PanelSpecies | undefined): ResultDraft[] {
  const panel = resultPanels.find((item) => item.id === id);

  return (panel?.analytes ?? []).map((item) => {
    const range = species ? item.ranges[species] : undefined;

    return {
      ...emptyResult(),
      analyte: item.analyte,
      unit: item.unit,
      low: range && 'low' in range ? String(range.low) : '',
      high: range && 'high' in range ? String(range.high) : '',
      rangeText: range && 'text' in range ? range.text : '',
      rangeKind: Object.values(item.ranges).some((each) => 'text' in each) ? 'text' : 'numbers',
      panel: id,
    };
  });
}

/**
 * Turns a panel on or off. On adds its rows after the ones there, skipping a value already listed
 * and dropping untouched rows typed by hand; off takes back its rows that have no value yet, so
 * nothing typed is ever thrown away.
 */
export function togglePanel(drafts: ResultDraft[], id: ResultPanelId, on: boolean, species: PanelSpecies | undefined): ResultDraft[] {
  if (!on) {
    const left = drafts.filter((draft) => draft.panel !== id || draft.value.trim() !== '');
    return left.length > 0 ? left : [emptyResult()];
  }

  const kept = drafts.filter((draft) => draft.panel || !isBlankDraft(draft));
  const listed = new Set(kept.map((draft) => normal(draft.analyte)));
  const added = panelDrafts(id, species).filter((draft) => !listed.has(normal(draft.analyte)));

  return [...kept, ...added];
}

/** The rows a request's form opens with: its matched panels, or one empty row. */
export function startingDrafts(tests: LabTest[], species: PanelSpecies | undefined): ResultDraft[] {
  const rows = panelsForTests(tests).flatMap((id) => panelDrafts(id, species));
  return rows.length > 0 ? rows : [emptyResult()];
}
