'use client';

import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input, Select } from '@/components/ui/field';
import { appointmentSettingsText } from '../../content/appointment-settings';
import type { TypeDraft, TypeProblem } from '../../lib/appointment-type-draft';
import type { QueuePriority } from '../../types/appointment-settings.types';

interface AppointmentTypeRowProps {
  type: TypeDraft;
  index: number;
  /** A prefix from `useId`, so the row's controls have ids of their own. */
  idPrefix: string;
  problem?: TypeProblem;
  disabled?: boolean;
  onChange: (type: TypeDraft) => void;
  onRemove: () => void;
}

const PRIORITIES: QueuePriority[] = ['normal', 'urgent', 'emergency'];

/** One appointment type: its code and name, and the four rules that come with it. */
export function AppointmentTypeRow({ type, index, idPrefix, problem, disabled, onChange, onRemove }: AppointmentTypeRowProps) {
  const text = appointmentSettingsText.types;
  const prefix = `${idPrefix}-${index}`;
  const set = <K extends keyof TypeDraft>(field: K, value: TypeDraft[K]) => onChange({ ...type, [field]: value });

  return (
    <li className={`rounded-lg border p-3 ${problem ? 'border-red-300 bg-red-50/40' : 'border-ink-200 bg-ink-50/40'}`}>
      <div className="mb-2 flex items-center justify-between gap-2">
        <p className="text-sm font-semibold text-ink-900">{text.row(index + 1)}</p>
        {type.locked ? (
          <span className="text-xs text-ink-500">{text.regularLocked}</span>
        ) : (
          <Button variant="ghost" size="sm" disabled={disabled} onClick={onRemove}>
            {text.remove}
          </Button>
        )}
      </div>

      <div className="grid gap-3 sm:grid-cols-[1fr_1.4fr_1fr_1fr_1.1fr]">
        <Field label={text.code} htmlFor={`${prefix}-code`}>
          <Input
            id={`${prefix}-code`}
            value={type.code}
            readOnly={type.locked}
            maxLength={30}
            disabled={disabled}
            className="h-9 font-mono"
            onChange={(event) => set('code', event.target.value.toLowerCase())}
          />
        </Field>
        <Field label={text.name} htmlFor={`${prefix}-name`}>
          <Input id={`${prefix}-name`} value={type.name} maxLength={60} disabled={disabled} className="h-9" onChange={(event) => set('name', event.target.value)} />
        </Field>
        <Field label={text.duration} htmlFor={`${prefix}-duration`}>
          <Input
            id={`${prefix}-duration`}
            inputMode="numeric"
            value={type.duration}
            placeholder={text.useDefault}
            disabled={disabled}
            className="h-9 tabular-nums"
            onChange={(event) => set('duration', event.target.value)}
          />
        </Field>
        <Field label={text.leadTime} htmlFor={`${prefix}-lead`}>
          <Input
            id={`${prefix}-lead`}
            inputMode="numeric"
            value={type.leadTime}
            placeholder={text.useDefault}
            disabled={disabled}
            className="h-9 tabular-nums"
            onChange={(event) => set('leadTime', event.target.value)}
          />
        </Field>
        <Field label={text.priority} htmlFor={`${prefix}-priority`}>
          <Select
            id={`${prefix}-priority`}
            value={type.priority}
            disabled={disabled}
            className="h-9"
            onChange={(event) => set('priority', event.target.value as QueuePriority)}
          >
            {PRIORITIES.map((priority) => (
              <option key={priority} value={priority}>
                {text.priorities[priority]}
              </option>
            ))}
          </Select>
        </Field>
      </div>

      <Checkbox
        className="mt-2"
        label={text.onlineBooking}
        checked={type.onlineBooking}
        disabled={disabled}
        onChange={(event) => set('onlineBooking', event.target.checked)}
      />

      {problem && (
        <p role="alert" className="mt-2 text-sm text-red-700">
          {text[problem]}
        </p>
      )}
    </li>
  );
}
