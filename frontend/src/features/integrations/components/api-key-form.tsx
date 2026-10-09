'use client';

import { useId, useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Input, Select } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createApiKeyAction } from '../actions/integration-actions';
import { apiKeySettingsPage } from '../content/api-key-settings';
import type { CreatedApiKey } from '../types/integration.types';
import { CreatedApiKeyCard } from './created-api-key-card';

interface ApiKeyFormProps {
  /** Scope to label, already narrowed to what this session may give a key. */
  scopeOptions: Record<string, string>;
  /** Branch id to name, the branches this session works at. */
  branchOptions: Record<string, string>;
}

/** A new API key. Once made, the key is shown in place of the form, this once. */
export function ApiKeyForm({ scopeOptions, branchOptions }: ApiKeyFormProps) {
  const text = apiKeySettingsPage.form;
  const id = useId();
  const [name, setName] = useState('');
  const [scopes, setScopes] = useState<string[]>([]);
  const [branches, setBranches] = useState<string[]>([]);
  const [days, setDays] = useState(90);
  const [created, setCreated] = useState<CreatedApiKey>();
  const { errors, message, pending, submit, touch } = useFormSubmit(formMessages);

  if (created) {
    return <CreatedApiKeyCard apiKey={created} onDone={() => setCreated(undefined)} />;
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => createApiKeyAction({ name, scopes, branch_ids: branches, expires_in_days: days }), (key) => {
      setCreated(key);
      setName('');
      setScopes([]);
      setBranches([]);
    });
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
            <Field label={text.name} htmlFor={`${id}-name`} hint={text.nameHint} error={errors.name}>
              <Input id={`${id}-name`} maxLength={100} value={name} onChange={(event) => change(setName)(event.target.value)} />
            </Field>
            <Field label={text.expiry} htmlFor={`${id}-expiry`} hint={text.expiryHint} error={errors.expires_at}>
              <Select id={`${id}-expiry`} value={days} onChange={(event) => change(setDays)(Number(event.target.value))}>
                {apiKeySettingsPage.expiryOptions.map((option) => (
                  <option key={option.days} value={option.days}>
                    {option.label}
                  </option>
                ))}
              </Select>
            </Field>
          </div>

          {Object.keys(scopeOptions).length === 0 ? (
            <p className="text-sm text-ink-600">{text.noScopes}</p>
          ) : (
            <CheckboxGroup
              legend={text.scopes}
              hint={text.scopesHint}
              error={errors.scopes}
              options={scopeOptions}
              selected={scopes}
              onChange={change(setScopes)}
            />
          )}

          {Object.keys(branchOptions).length > 1 && (
            <CheckboxGroup
              legend={text.branches}
              hint={text.branchesHint}
              error={errors.branch_ids}
              options={branchOptions}
              selected={branches}
              onChange={change(setBranches)}
            />
          )}

          <FormFooter pending={pending} message={message} submitLabel={text.submit} pendingLabel={text.pending} />
        </CardBody>
      </Card>
    </form>
  );
}
