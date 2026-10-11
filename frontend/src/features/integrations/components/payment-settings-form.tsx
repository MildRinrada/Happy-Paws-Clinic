'use client';

import { useId, useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input, Select } from '@/components/ui/field';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { configurePaymentAction } from '../actions/integration-actions';
import { paymentSettingsPage, providerShared } from '../content/provider-settings';
import { emptySecret, secretChange } from '../lib/secret-change';
import type { PaymentSettings } from '../types/integration.types';
import { SecretInput } from './secret-input';

const FORM_ID = 'payment-settings-form';

/** The payment provider: who, which mode, the public key, and the two write-only secrets. Saved by the save bar. */
export function PaymentSettingsForm({ settings: initial }: { settings: PaymentSettings }) {
  const text = paymentSettingsPage;
  const id = useId();
  const [settings, setSettings] = useState(initial);
  const [provider, setProvider] = useState(initial.provider ?? '');
  const [mode, setMode] = useState(initial.mode);
  const [publicKey, setPublicKey] = useState(initial.public_key ?? '');
  const [enabled, setEnabled] = useState(initial.enabled);
  const [secretKey, setSecretKey] = useState(emptySecret);
  const [webhookSecret, setWebhookSecret] = useState(emptySecret);
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(formMessages);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: text.heading,
    value: { provider, mode, publicKey, enabled, secretKey, webhookSecret },
    pending,
    failed: Boolean(message) || Object.keys(errors).length > 0,
    onReset: (stored) => {
      clear();
      setProvider(stored.provider);
      setMode(stored.mode);
      setPublicKey(stored.publicKey);
      setEnabled(stored.enabled);
      setSecretKey(stored.secretKey);
      setWebhookSecret(stored.webhookSecret);
    },
  });

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const sent = { provider, mode, publicKey, enabled, secretKey: emptySecret, webhookSecret: emptySecret };

    submit(
      () =>
        configurePaymentAction({
          version: settings.version,
          values: {
            provider,
            mode,
            enabled,
            public_key: publicKey,
            secret_key: secretChange(secretKey),
            webhook_secret: secretChange(webhookSecret),
          },
        }),
      (stored) => {
        setSettings(stored);
        setSecretKey(emptySecret);
        setWebhookSecret(emptySecret);
        // The secrets are write-only: once saved, the boxes are empty again and that is "saved".
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
          <div className="grid gap-4 sm:grid-cols-3">
            <Field label={text.provider} htmlFor={`${id}-provider`} hint={providerShared.providerHint} error={errors.provider}>
              <Input id={`${id}-provider`} className="font-mono" maxLength={30} value={provider} onChange={(event) => change(setProvider)(event.target.value)} />
            </Field>
            <Field label={text.mode} htmlFor={`${id}-mode`} error={errors.mode}>
              <Select id={`${id}-mode`} value={mode} onChange={(event) => change(setMode)(event.target.value as PaymentSettings['mode'])}>
                {Object.entries(text.modes).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </Select>
            </Field>
            <Field label={text.publicKey} htmlFor={`${id}-public`} error={errors.public_key}>
              <Input id={`${id}-public`} className="font-mono" maxLength={200} value={publicKey} onChange={(event) => change(setPublicKey)(event.target.value)} />
            </Field>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <SecretInput label={text.secretKey} stored={settings.has_secret_key} draft={secretKey} error={errors.secret_key} onChange={change(setSecretKey)} />
            <SecretInput label={text.webhookSecret} stored={settings.has_webhook_secret} draft={webhookSecret} error={errors.webhook_secret} onChange={change(setWebhookSecret)} />
          </div>

          <Checkbox label={text.enabled} hint={text.enabledHint} checked={enabled} onChange={(event) => change(setEnabled)(event.target.checked)} />

          <SectionFooter pending={pending} message={message} saved={saved} inBar={changes.inBar} />
        </CardBody>
      </Card>
    </form>
  );
}
