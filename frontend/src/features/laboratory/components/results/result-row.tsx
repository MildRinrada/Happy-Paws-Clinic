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

/** The grid every row and the header share: name, value, unit, range, abnormal, remove. */
export const resultGrid =
  'grid grid-cols-2 gap-2 sm:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)_5.5rem_minmax(0,1.3fr)_4.5rem_4.5rem] sm:items-center';

const small = 'h-9';

/**
 * One measured value on one line. A row filled from a panel keeps its name, unit and range out of
 * the Tab order (still editable by click or tap), so Tab goes from value to value down a blood
 * count. Each box has its own label for a screen reader, naming the value it belongs to.
 */
export function ResultRow({ idPrefix, index, draft, error, onChange, onRemove }: ResultRowProps) {
  const text = labPage.resultForm;
  const id = (field: string) => `${idPrefix}-${index}-${field}`;
  const label = (field: string) => text.rowLabel(field, draft.analyte, index + 1);
  const skip = draft.panel ? -1 : undefined;
  const set = (field: keyof ResultDraft) => (event: { target: { value: string } }) => onChange({ ...draft, [field]: event.target.value });

  return (
    <div role="group" aria-label={draft.analyte || text.row(index + 1)} className="rounded-lg border border-ink-100 p-2 sm:border-0 sm:p-0">
      <div className={resultGrid}>
        <div>
          <label htmlFor={id('analyte')} className="sr-only">{label(text.analyte)}</label>
          <Input
            id={id('analyte')}
            className={`${small} ${draft.panel ? 'border-transparent bg-ink-50 font-medium' : ''}`}
            value={draft.analyte}
            maxLength={100}
            placeholder={text.analytePlaceholder}
            tabIndex={skip}
            onChange={set('analyte')}
          />
        </div>
        <div>
          <label htmlFor={id('value')} className="sr-only">{label(text.value)}</label>
          <Input
            id={id('value')}
            className={`${small} tabular-nums`}
            value={draft.value}
            maxLength={500}
            placeholder={text.valuePlaceholder}
            aria-invalid={Boolean(error)}
            onChange={set('value')}
          />
        </div>
        <div>
          <label htmlFor={id('unit')} className="sr-only">{label(text.unit)}</label>
          <Input id={id('unit')} className={small} value={draft.unit} maxLength={30} tabIndex={skip} onChange={set('unit')} />
        </div>
        {draft.rangeKind === 'text' ? (
          <div>
            <label htmlFor={id('range-text')} className="sr-only">{label(text.rangeText)}</label>
            <Input id={id('range-text')} className={small} value={draft.rangeText} maxLength={200} tabIndex={skip} onChange={set('rangeText')} />
          </div>
        ) : (
          <div className="flex items-center gap-1">
            <label htmlFor={id('low')} className="sr-only">{label(text.low)}</label>
            <Input id={id('low')} className={`${small} tabular-nums`} inputMode="decimal" value={draft.low} tabIndex={skip} onChange={set('low')} />
            <span aria-hidden className="text-ink-400">-</span>
            <label htmlFor={id('high')} className="sr-only">{label(text.high)}</label>
            <Input id={id('high')} className={`${small} tabular-nums`} inputMode="decimal" value={draft.high} tabIndex={skip} onChange={set('high')} />
          </div>
        )}
        <label className="flex items-center gap-2 text-sm text-ink-700 sm:justify-center">
          <input
            type="checkbox"
            checked={draft.abnormal}
            tabIndex={skip}
            onChange={(event) => onChange({ ...draft, abnormal: event.target.checked })}
            className="size-4 accent-brand-600"
          />
          <span className="sm:sr-only">{text.abnormal}</span>
        </label>
        <div className="text-right">
          {onRemove && (
            <Button variant="ghost" size="sm" tabIndex={skip} aria-label={label(text.removeRow)} onClick={onRemove}>
              {text.removeRow}
            </Button>
          )}
        </div>
      </div>
      {error && (
        <p role="alert" className="mt-1 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
