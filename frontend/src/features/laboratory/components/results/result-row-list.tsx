'use client';

import { Button } from '@/components/ui/button';
import { labPage } from '../../content/lab-page';
import { emptyResult, type ResultDraft } from '../../lib/result-drafts';
import { ResultRow } from './result-row';

interface ResultRowListProps {
  idPrefix: string;
  drafts: ResultDraft[];
  /** By row key. */
  errors: Record<string, string>;
  onChange: (drafts: ResultDraft[]) => void;
}

/** The rows of values being entered, with a button for one more. A request takes up to 100 at a time. */
export function ResultRowList({ idPrefix, drafts, errors, onChange }: ResultRowListProps) {
  return (
    <div className="space-y-2">
      {drafts.map((draft, index) => (
        <ResultRow
          key={draft.key}
          idPrefix={idPrefix}
          index={index}
          draft={draft}
          error={errors[draft.key]}
          onChange={(next) => onChange(drafts.map((row) => (row.key === draft.key ? next : row)))}
          onRemove={drafts.length > 1 ? () => onChange(drafts.filter((row) => row.key !== draft.key)) : undefined}
        />
      ))}
      {drafts.length < 100 && (
        <Button variant="secondary" size="sm" onClick={() => onChange([...drafts, emptyResult()])}>
          {labPage.resultForm.addRow}
        </Button>
      )}
    </div>
  );
}
