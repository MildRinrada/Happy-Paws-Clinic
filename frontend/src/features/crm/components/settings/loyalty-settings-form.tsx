'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replaceLoyaltySettingsAction } from '../../actions/crm-manage-actions';
import { loyaltySettingsText } from '../../content/loyalty-settings';
import { draftFromSettings, toSettingsInput, type LoyaltySettingsDraft, type LoyaltySettingsField } from '../../lib/loyalty-settings-draft';
import { referralDraftFrom, toReferralInput, type ReferralDraft, type ReferralNumberField } from '../../lib/referral-settings-draft';
import type { LoyaltySettings } from '../../types/crm.types';
import { ReferralSettingsFields } from './referral-settings-fields';

const FIELDS: {
  field: LoyaltySettingsField;
  api: string;
  label: 'bahtPerPoint' | 'pointValue' | 'silver' | 'gold' | 'expiry';
  hint?: 'bahtPerPointHint' | 'pointValueHint' | 'expiryHint';
  money?: boolean;
}[] = [
  { field: 'bahtPerPoint', api: 'satang_per_point', label: 'bahtPerPoint', hint: 'bahtPerPointHint', money: true },
  { field: 'pointValueBaht', api: 'point_value_satang', label: 'pointValue', hint: 'pointValueHint', money: true },
  { field: 'silver', api: 'silver_min_points', label: 'silver' },
  { field: 'gold', api: 'gold_min_points', label: 'gold' },
  { field: 'months', api: 'points_expire_after_months', label: 'expiry', hint: 'expiryHint' },
];

/**
 * The points programme, written as a whole. Starts from the stored values; the version read goes
 * out as `If-Match`, so a colleague's change in between is refused, not silently replaced.
 */
export function LoyaltySettingsForm({ settings }: { settings: LoyaltySettings }) {
  const text = loyaltySettingsText;
  const id = useId();
  const [draft, setDraft] = useState<LoyaltySettingsDraft>(() => draftFromSettings(settings));
  const [version, setVersion] = useState(settings.version);
  const [bad, setBad] = useState<LoyaltySettingsField[]>([]);
  const [referral, setReferral] = useState<ReferralDraft>(() => referralDraftFrom(settings));
  const [badReferral, setBadReferral] = useState<ReferralNumberField[]>([]);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);

  function set(field: LoyaltySettingsField, value: string) {
    touch();
    setDraft((current) => ({ ...current, [field]: value }));
  }

  function setReferralPart(patch: Partial<ReferralDraft>) {
    touch();
    setReferral((current) => ({ ...current, ...patch }));
  }

  function badText(field: LoyaltySettingsField, money?: boolean) {
    if (!bad.includes(field)) return undefined;
    if (field === 'gold' && draft.gold.trim() !== '' && Number(draft.gold) > 0) return text.goldAboveSilver;
    return money ? text.amount : text.wholeNumber;
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const { input, bad: wrong } = toSettingsInput(draft);
    const { input: referralInput, bad: wrongReferral } = toReferralInput(referral);
    setBad(wrong);
    setBadReferral(wrongReferral);
    if (!input || !referralInput) return;

    submit(
      () => replaceLoyaltySettingsAction({ version, values: { ...input, ...referralInput } }),
      (stored) => {
        setVersion(stored.version);
        setDraft(draftFromSettings(stored));
        setReferral(referralDraftFrom(stored));
      },
    );
  }

  return (
    <form onSubmit={save} noValidate className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        {FIELDS.map(({ field, api, label, hint, money }) => (
          <Field
            key={field}
            label={text[label]}
            htmlFor={`${id}-${field}`}
            hint={hint ? text[hint] : undefined}
            error={badText(field, money) ?? errors[api]}
          >
            <Input
              id={`${id}-${field}`}
              inputMode={money ? 'decimal' : 'numeric'}
              value={draft[field]}
              className="h-9 max-w-40 tabular-nums"
              aria-invalid={bad.includes(field) || Boolean(errors[api])}
              onChange={(event) => set(field, event.target.value)}
            />
          </Field>
        ))}
      </div>

      <ReferralSettingsFields draft={referral} bad={badReferral} errors={errors} onChange={setReferralPart} />

      {saved && (
        <p role="status" className="rounded-lg border border-emerald-200 bg-emerald-50 px-3.5 py-2.5 text-sm text-emerald-900">
          {text.stored}
        </p>
      )}

      <div className="flex flex-wrap items-center justify-end gap-3 border-t border-ink-100 pt-4">
        <FormStatus message={message} savedText="" />
        <Button type="submit" disabled={pending}>
          {pending ? text.submitting : text.submit}
        </Button>
      </div>
    </form>
  );
}
