'use client';

import { useId, useState, type FormEvent } from 'react';
import { Field, Input } from '@/components/ui/field';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
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

const FORM_ID = 'loyalty-settings-form';

/**
 * The points programme, written as a whole by the settings save bar. Starts from the stored values; the version read goes
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
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(formMessages);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: text.heading,
    value: { draft, referral },
    pending,
    failed: Boolean(message) || Object.keys(errors).length > 0 || bad.length > 0 || badReferral.length > 0,
    onReset: (stored) => {
      clear();
      setBad([]);
      setBadReferral([]);
      setDraft(stored.draft);
      setReferral(stored.referral);
    },
  });

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
        const next = { draft: draftFromSettings(stored), referral: referralDraftFrom(stored) };
        setVersion(stored.version);
        setDraft(next.draft);
        setReferral(next.referral);
        changes.markSaved(next);
      },
    );
  }

  return (
    <form id={FORM_ID} onSubmit={save} noValidate className="space-y-4">
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

      <SectionFooter pending={pending} message={message} inBar={changes.inBar} submitLabel={text.submit} />
    </form>
  );
}
