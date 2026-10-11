'use client';

import { useId, useState, type FormEvent } from 'react';
import { Field, Input } from '@/components/ui/field';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replaceCancellationPolicyAction } from '../../actions/appointment-settings-actions';
import { appointmentSettingsText } from '../../content/appointment-settings';
import { draftFrom, toPolicyInput, type PolicyDraft, type PolicyField } from '../../lib/policy-draft';
import type { CancellationPolicy } from '../../types/appointment-settings.types';

const FIELDS: { field: PolicyField; api: string; label: 'cutoffHours' | 'fee' | 'freeAllowance' | 'windowDays' }[] = [
  { field: 'cutoffHours', api: 'cutoff_hours', label: 'cutoffHours' },
  { field: 'feeBaht', api: 'fee_satang', label: 'fee' },
  { field: 'freeAllowance', api: 'free_allowance', label: 'freeAllowance' },
  { field: 'windowDays', api: 'window_days', label: 'windowDays' },
];

const FORM_ID = 'cancellation-policy-form';

/**
 * The four values, written together by the settings save bar. The form starts from the stored policy, and the version it was
 * read at goes out as `If-Match`, so a colleague's change in between is refused rather than silently
 * replaced. After a save it shows what the backend answered with, and that version is sent next.
 */
export function CancellationPolicyForm({ policy }: { policy: CancellationPolicy }) {
  const text = appointmentSettingsText.policy;
  const id = useId();
  const [draft, setDraft] = useState<PolicyDraft>(() => draftFrom(policy));
  const [version, setVersion] = useState(policy.version);
  const [bad, setBad] = useState<PolicyField[]>([]);
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(appointmentSettingsText.messages);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: text.heading,
    value: draft,
    pending,
    failed: Boolean(message) || Object.keys(errors).length > 0 || bad.length > 0,
    onReset: (stored) => {
      clear();
      setBad([]);
      setDraft(stored);
    },
  });

  function set(field: PolicyField, value: string) {
    touch();
    setDraft((current) => ({ ...current, [field]: value }));
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const { input, bad: wrong } = toPolicyInput(draft);
    setBad(wrong);
    if (!input) return;

    submit(
      () => replaceCancellationPolicyAction({ version, values: input }),
      (stored) => {
        const next = draftFrom(stored);
        setVersion(stored.version);
        setDraft(next);
        changes.markSaved(next);
      },
    );
  }

  return (
    <form id={FORM_ID} onSubmit={save} noValidate className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        {FIELDS.map(({ field, api, label }) => (
          <Field
            key={field}
            label={text[label]}
            htmlFor={`${id}-${field}`}
            hint={text[`${label}Hint`]}
            error={bad.includes(field) ? (field === 'feeBaht' ? appointmentSettingsText.amount : appointmentSettingsText.wholeNumber) : errors[api]}
          >
            <Input
              id={`${id}-${field}`}
              inputMode={field === 'feeBaht' ? 'decimal' : 'numeric'}
              value={draft[field]}
              className="h-9 max-w-40 tabular-nums"
              aria-invalid={bad.includes(field) || Boolean(errors[api])}
              onChange={(event) => set(field, event.target.value)}
            />
          </Field>
        ))}
      </div>

      {saved && <p role="status" className="rounded-lg border border-emerald-200 bg-emerald-50 px-3.5 py-2.5 text-sm text-emerald-900">{text.stored}</p>}

      <SectionFooter pending={pending} message={message} inBar={changes.inBar} submitLabel={text.submit} />
    </form>
  );
}
