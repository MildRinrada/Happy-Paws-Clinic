'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { labPage } from '../../content/lab-page';
import type { ResultDraft } from '../../lib/result-drafts';

interface ResultRowProps {
  idPrefix: string;
  index: number;
  draft: ResultDraft;
  error?: string;
  onChange: (draft: ResultDraft) => void;
  onRemove?: () => void;
}

const label = 'block text-xs font-medium text-ink-600';

/** One measured value: what, how much, in what unit, and the range printed beside it. */
export function ResultRow({ idPrefix, index, draft, error, onChange, onRemove }: ResultRowProps) {
  const text = labPage.resultForm;
  const id = (field: string) => `${idPrefix}-${index}-${field}`;
  const set = (field: keyof ResultDraft) => (event: { target: { value: string } }) => onChange({ ...draft, [field]: event.target.value });

  return (
    <fieldset className="space-y-2 rounded-lg border border-ink-100 p-3">
      <legend className="px-1 text-xs text-ink-500">{text.row(index + 1)}</legend>
      <div className="grid gap-2 sm:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)_5rem_5.5rem_5.5rem]">
        <div className="space-y-1">
          <label htmlFor={id('analyte')} className={label}>{text.analyte}</label>
          <Input id={id('analyte')} value={draft.analyte} maxLength={100} placeholder={text.analytePlaceholder} onChange={set('analyte')} />
        </div>
        <div className="space-y-1">
          <label htmlFor={id('value')} className={label}>{text.value}</label>
          <Input id={id('value')} value={draft.value} maxLength={500} placeholder={text.valuePlaceholder} onChange={set('value')} />
        </div>
        <div className="space-y-1">
          <label htmlFor={id('unit')} className={label}>{text.unit}</label>
          <Input id={id('unit')} value={draft.unit} maxLength={30} onChange={set('unit')} />
        </div>
        <div className="space-y-1">
          <label htmlFor={id('low')} className={label}>{text.low}</label>
          <Input id={id('low')} inputMode="decimal" value={draft.low} onChange={set('low')} />
        </div>
        <div className="space-y-1">
          <label htmlFor={id('high')} className={label}>{text.high}</label>
          <Input id={id('high')} inputMode="decimal" value={draft.high} onChange={set('high')} />
        </div>
      </div>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <label className="flex items-center gap-2 text-sm text-ink-700">
          <input
            type="checkbox"
            checked={draft.abnormal}
            onChange={(event) => onChange({ ...draft, abnormal: event.target.checked })}
            className="size-4 accent-brand-600"
          />
          {text.abnormal}
        </label>
        {onRemove && (
          <Button variant="ghost" size="sm" onClick={onRemove}>
            {text.removeRow}
          </Button>
        )}
      </div>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </fieldset>
  );
}
