'use client';

import { useId } from 'react';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input } from '@/components/ui/field';
import { referralSettingsText } from '../../content/referral-settings';
import type { ReferralDraft, ReferralNumberField } from '../../lib/referral-settings-draft';

const NUMBERS: { field: ReferralNumberField; api: string; label: 'referrerPoints' | 'friendPoints' | 'monthlyCap'; hint?: 'monthlyCapHint' }[] = [
  { field: 'referrerPoints', api: 'referral_referrer_points', label: 'referrerPoints' },
  { field: 'friendPoints', api: 'referral_friend_points', label: 'friendPoints' },
  { field: 'monthlyCap', api: 'referral_monthly_cap', label: 'monthlyCap', hint: 'monthlyCapHint' },
];

interface Props {
  draft: ReferralDraft;
  /** Fields that did not parse here. */
  bad: ReferralNumberField[];
  /** Refusals from the backend, by API field name. */
  errors: Record<string, string | undefined>;
  onChange: (patch: Partial<ReferralDraft>) => void;
}

/** Refer a friend inside the points programme: the switch, the points each side gets, and the monthly cap. */
export function ReferralSettingsFields({ draft, bad, errors, onChange }: Props) {
  const text = referralSettingsText;
  const id = useId();

  return (
    <fieldset className="space-y-4 border-t border-ink-100 pt-4">
      <legend className="text-sm font-semibold text-ink-900">{text.legend}</legend>
      <p className="text-sm text-ink-600">{text.intro}</p>

      <Checkbox
        id={`${id}-enabled`}
        checked={draft.enabled}
        label={text.enabled}
        hint={text.enabledHint}
        onChange={(event) => onChange({ enabled: event.target.checked })}
      />

      <div className="grid gap-4 sm:grid-cols-3">
        {NUMBERS.map(({ field, api, label, hint }) => (
          <Field
            key={field}
            label={text[label]}
            htmlFor={`${id}-${field}`}
            hint={hint ? text[hint] : undefined}
            error={(bad.includes(field) ? (field === 'monthlyCap' ? text.capRange : text.pointsRange) : undefined) ?? errors[api]}
          >
            <Input
              id={`${id}-${field}`}
              inputMode="numeric"
              value={draft[field]}
              className="h-9 max-w-32 tabular-nums"
              aria-invalid={bad.includes(field) || Boolean(errors[api])}
              onChange={(event) => onChange({ [field]: event.target.value })}
            />
          </Field>
        ))}
      </div>
    </fieldset>
  );
}
