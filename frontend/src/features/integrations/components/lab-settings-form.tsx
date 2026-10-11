'use client';

import { useId, useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input } from '@/components/ui/field';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { configureLabAction } from '../actions/integration-actions';
import { labSettingsPage, providerShared } from '../content/provider-settings';
import { emptySecret, secretChange } from '../lib/secret-change';
import type { LabSettings } from '../types/integration.types';
import { SecretInput } from './secret-input';

const FORM_ID = 'lab-settings-form';

/**
 * The outside lab: which one, the clinic's account there, its API address, and the write-only key.
 * Saved by the save bar.
 */
export function LabSettingsForm({ settings: initial }: { settings: LabSettings }) {
  const text = labSettingsPage;
  const id = useId();
  const [settings, setSettings] = useState(initial);
  const [provider, setProvider] = useState(initial.provider ?? '');
  const [accountId, setAccountId] = useState(initial.account_id ?? '');
  const [baseUrl, setBaseUrl] = useState(initial.base_url ?? '');
  const [enabled, setEnabled] = useState(initial.enabled);
  const [apiKey, setApiKey] = useState(emptySecret);
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(formMessages);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: text.heading,
    value: { provider, accountId, baseUrl, enabled, apiKey },
    pending,
    failed: Boolean(message) || Object.keys(errors).length > 0,
    onReset: (stored) => {
      clear();
      setProvider(stored.provider);
      setAccountId(stored.accountId);
      setBaseUrl(stored.baseUrl);
      setEnabled(stored.enabled);
      setApiKey(stored.apiKey);
    },
  });

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const sent = { provider, accountId, baseUrl, enabled, apiKey: emptySecret };

    submit(
      () =>
        configureLabAction({
          version: settings.version,
          values: { provider, enabled, account_id: accountId, base_url: baseUrl, api_key: secretChange(apiKey) },
        }),
      (stored) => {
        setSettings(stored);
        setApiKey(emptySecret);
        // The key is write-only: once saved, the box is empty again and that is "saved".
        changes.markSaved(sent);
      },
    );
  }

  const change = <T,>(set: (value: T) => void) => (value: T) => {
    touch();
    set(value);
  };

  return (
    <form id={FORM_ID} onSubmit={save}>
      <Card>
        <CardHeader title={text.heading} />
        <CardBody className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={text.provider} htmlFor={`${id}-provider`} hint={providerShared.providerHint} error={errors.provider}>
              <Input id={`${id}-provider`} className="font-mono" maxLength={30} value={provider} onChange={(event) => change(setProvider)(event.target.value)} />
            </Field>
            <Field label={text.accountId} htmlFor={`${id}-account`} error={errors.account_id}>
              <Input id={`${id}-account`} maxLength={100} value={accountId} onChange={(event) => change(setAccountId)(event.target.value)} />
            </Field>
            <Field label={text.baseUrl} htmlFor={`${id}-url`} hint={text.baseUrlHint} error={errors.base_url}>
              <Input id={`${id}-url`} type="url" inputMode="url" placeholder="https://" maxLength={2000} value={baseUrl} onChange={(event) => change(setBaseUrl)(event.target.value)} />
            </Field>
            <SecretInput label={text.apiKey} stored={settings.has_api_key} draft={apiKey} error={errors.api_key} onChange={change(setApiKey)} />
          </div>

          <Checkbox label={text.enabled} hint={text.enabledHint} checked={enabled} onChange={(event) => change(setEnabled)(event.target.checked)} />

          <SectionFooter pending={pending} message={message} saved={saved} inBar={changes.inBar} />
        </CardBody>
      </Card>
    </form>
  );
}
