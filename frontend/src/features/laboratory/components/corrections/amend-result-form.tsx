'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Textarea } from '@/components/ui/field';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { amendLabResultAction } from '../../actions/correction-actions';
import { labPage } from '../../content/lab-page';
import { valueText } from '../../lib/previous-values';
import { parseNumber, splitResultValue } from '../../lib/result-value';
import type { LabResult } from '../../types/lab.types';

interface AmendResultFormProps {
  labRequestId: string;
  version: number;
  result: LabResult;
  onDone: () => void;
}

const asText = (value?: number) => (value === undefined ? '' : String(value));

/**
 * The value as it should have been, starting from the value on file, and why. Every field is sent:
 * the correction replaces the whole value, and the value it replaces is kept (§ 52).
 */
export function AmendResultForm({ labRequestId, version, result, onDone }: AmendResultFormProps) {
  const text = labPage.correct;
  const id = (field: string) => `amend-${result.id}-${field}`;
  const [value, setValue] = useState(valueText(result));
  const [unit, setUnit] = useState(result.unit ?? '');
  const [low, setLow] = useState(asText(result.reference_low));
  const [high, setHigh] = useState(asText(result.reference_high));
  const [rangeText, setRangeText] = useState(result.reference_text ?? '');
  const [abnormal, setAbnormal] = useState(result.flag === 'abnormal');
  const [reason, setReason] = useState('');
  const [local, setLocal] = useState<Record<string, string>>({});
  const { errors, message, pending, submit } = useFormSubmit(labPage.messages);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const lowNumber = parseNumber(low);
    const highNumber = parseNumber(high);
    const problems: Record<string, string> = {};

    if (!value.trim()) problems.value = text.noValue;
    if (!reason.trim()) problems.reason = text.noReason;
    if ((low.trim() && lowNumber === undefined) || (high.trim() && highNumber === undefined)) problems.range = text.badRange;
    setLocal(problems);
    if (Object.keys(problems).length > 0) return;

    submit(
      () =>
        amendLabResultAction({
          labRequestId,
          resultId: result.id,
          version,
          correction: {
            reason,
            ...splitResultValue(value),
            unit,
            reference_low: lowNumber,
            reference_high: highNumber,
            reference_text: rangeText,
            abnormal: abnormal || undefined,
          },
        }),
      onDone,
    );
  }

  return (
    <form onSubmit={save} className="space-y-3">
      <p className="text-sm text-ink-600">{text.current(`${valueText(result)}${result.unit ? ` ${result.unit}` : ''}`)}</p>
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label={text.value} htmlFor={id('value')} error={local.value ?? errors.value ?? errors.value_text}>
          <Input id={id('value')} value={value} maxLength={500} onChange={(event) => setValue(event.target.value)} />
        </Field>
        <Field label={text.unit} htmlFor={id('unit')} error={errors.unit}>
          <Input id={id('unit')} value={unit} maxLength={30} onChange={(event) => setUnit(event.target.value)} />
        </Field>
        <Field label={text.low} htmlFor={id('low')} error={local.range}>
          <Input id={id('low')} inputMode="decimal" value={low} onChange={(event) => setLow(event.target.value)} />
        </Field>
        <Field label={text.high} htmlFor={id('high')}>
          <Input id={id('high')} inputMode="decimal" value={high} onChange={(event) => setHigh(event.target.value)} />
        </Field>
      </div>
      <Field label={text.rangeText} htmlFor={id('range-text')} error={errors.reference_text}>
        <Input id={id('range-text')} value={rangeText} maxLength={200} onChange={(event) => setRangeText(event.target.value)} />
      </Field>
      <label className="flex items-center gap-2 text-sm text-ink-700">
        <input type="checkbox" checked={abnormal} onChange={(event) => setAbnormal(event.target.checked)} className="size-4 accent-brand-600" />
        {text.abnormal}
      </label>
      <Field label={text.reason} htmlFor={id('reason')} error={local.reason ?? errors.reason}>
        <Textarea id={id('reason')} rows={2} value={reason} maxLength={500} placeholder={text.reasonPlaceholder} onChange={(event) => setReason(event.target.value)} />
      </Field>
      <p className="text-xs text-ink-500">{text.hint}</p>
      <div className="flex flex-wrap items-center justify-end gap-2">
        {message && (
          <p role="alert" className="text-sm text-red-700">
            {message}
          </p>
        )}
        <Button variant="ghost" size="sm" disabled={pending} onClick={onDone}>
          {text.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? text.working : text.amendSubmit}
        </Button>
      </div>
    </form>
  );
}
