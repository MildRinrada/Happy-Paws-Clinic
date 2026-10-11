'use client';

import { useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input } from '@/components/ui/field';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { configureLineAction } from '../actions/line-actions';
import { lineSettingsPage } from '../content/line-settings';
import { lineSettingsChanges } from '../lib/line-settings-changes';
import type { LineSettings } from '../types/line.types';
import { LineCredentialFields, type CredentialDraft } from './line-credential-fields';
import { LineTypeSwitches } from './line-type-switches';

const FORM_ID = 'line-settings-form';

/**
 * The LINE channel's credentials, the master switch, the broadcast quota and the per-type switches:
 * two cards, one form, saved together by the settings save bar.
 */
export function LineSettingsForm({ settings: initial }: { settings: LineSettings }) {
  const text = lineSettingsPage;
  const [settings, setSettings] = useState(initial);
  const [credentials, setCredentials] = useState<CredentialDraft>({
    channelId: initial.channel_id ?? '',
    channelSecret: '',
    channelAccessToken: '',
    clearStored: false,
  });
  const [enabled, setEnabled] = useState(initial.enabled);
  const [limit, setLimit] = useState(initial.broadcast_monthly_limit);
  const [types, setTypes] = useState(initial.notification_types);
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(formMessages);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: text.title,
    value: { credentials, enabled, limit, types },
    pending,
    failed: Boolean(message) || Object.keys(errors).length > 0,
    onReset: (stored) => {
      clear();
      setCredentials(stored.credentials);
      setEnabled(stored.enabled);
      setLimit(stored.limit);
      setTypes(stored.types);
    },
  });

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () => configureLineAction({ version: settings.version, values: lineSettingsChanges(settings, credentials, enabled, limit, types) }),
      (stored) => {
        const next = {
          credentials: { channelId: stored.channel_id ?? '', channelSecret: '', channelAccessToken: '', clearStored: false },
          enabled: stored.enabled,
          limit: stored.broadcast_monthly_limit,
          types: stored.notification_types,
        };
        setSettings(stored);
        setCredentials(next.credentials);
        setEnabled(next.enabled);
        setLimit(next.limit);
        setTypes(next.types);
        changes.markSaved(next);
      },
    );
  }

  return (
    <form id={FORM_ID} onSubmit={save} className="space-y-6">
      <Card>
        <CardHeader title={text.credentials.heading} />
        <CardBody>
          <LineCredentialFields
            draft={credentials}
            hasStored={settings.has_channel_secret || settings.has_channel_access_token}
            errors={errors}
            onChange={(draft) => {
              touch();
              setCredentials(draft);
            }}
          />
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={text.sending.heading} />
        <CardBody className="space-y-5">
          <Checkbox
            label={text.sending.enabled}
            hint={text.sending.enabledHint}
            checked={enabled}
            onChange={(event) => {
              touch();
              setEnabled(event.target.checked);
            }}
          />

          <div className="max-w-xs">
            <Field label={text.sending.quota} htmlFor="line-quota" hint={text.sending.quotaHint} error={errors.broadcast_monthly_limit}>
              <Input
                id="line-quota"
                type="number"
                min={0}
                max={1_000_000}
                value={limit}
                onChange={(event) => {
                  touch();
                  setLimit(Number(event.target.value));
                }}
              />
            </Field>
          </div>

          <LineTypeSwitches
            types={types}
            onChange={(key, on) => {
              touch();
              setTypes((current) => ({ ...current, [key]: on }));
            }}
          />
        </CardBody>
      </Card>

      <SectionFooter pending={pending} message={message} saved={saved} inBar={changes.inBar} />
    </form>
  );
}
