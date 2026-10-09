'use client';

import { useId, useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { configureLabAction } from '../actions/integration-actions';
import { labSettingsPage, providerShared } from '../content/provider-settings';
import { emptySecret, secretChange } from '../lib/secret-change';
import type { LabSettings } from '../types/integration.types';
import { SecretInput } from './secret-input';

/** The outside lab: which one, the clinic's account there, its API address, and the write-only key. */
export function LabSettingsForm({ settings: initial }: { settings: LabSettings }) {
  const text = labSettingsPage;
  const id = useId();
  const [settings, setSettings] = useState(initial);
  const [provider, setProvider] = useState(initial.provider ?? '');
  const [accountId, setAccountId] = useState(initial.account_id ?? '');
  const [baseUrl, setBaseUrl] = useState(initial.base_url ?? '');
  const [enabled, setEnabled] = useState(initial.enabled);
  const [apiKey, setApiKey] = useState(emptySecret);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () =>
        configureLabAction({
          version: settings.version,
          values: { provider, enabled, account_id: accountId, base_url: baseUrl, api_key: secretChange(apiKey) },
        }),
      (stored) => {
        setSettings(stored);
        setApiKey(emptySecret);
      },
    );
  }

  const change = <T,>(set: (value: T) => void) => (value: T) => {
    touch();
    set(value);
  };

  return (
    <form onSubmit={save}>
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

          <FormFooter pending={pending} message={message} saved={saved} />
        </CardBody>
      </Card>
    </form>
  );
}
