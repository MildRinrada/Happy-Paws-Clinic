'use client';

import { useId, useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Input, Select } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createDisplayAction } from '../../actions/display-actions';
import { queueDisplays } from '../../content/queue-displays';
import type { DisplaySetup } from '../../types/queue.types';
import { CreatedDisplayCard } from './created-display-card';

/**
 * A new waiting-room screen for this branch. Once made, its setup code, link and QR code are shown
 * in place of the form, this once.
 */
export function DisplayCreateForm({ branchId }: { branchId: string }) {
  const text = queueDisplays.form;
  const id = useId();
  const [name, setName] = useState('');
  const [days, setDays] = useState(0);
  const [created, setCreated] = useState<DisplaySetup>();
  const { errors, message, pending, submit, touch } = useFormSubmit(formMessages);

  if (created) {
    return <CreatedDisplayCard setup={created} onDone={() => setCreated(undefined)} />;
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => createDisplayAction({ name, branchId, expiresInDays: days }), (setup) => {
      setCreated(setup);
      setName('');
    });
  }

  return (
    <form onSubmit={save}>
      <Card>
        <CardHeader title={text.heading} />
        <CardBody className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={text.name} htmlFor={`${id}-name`} hint={text.nameHint} error={errors.name}>
              <Input
                id={`${id}-name`}
                maxLength={80}
                value={name}
                onChange={(event) => {
                  touch();
                  setName(event.target.value);
                }}
              />
            </Field>
            <Field label={text.expiry} htmlFor={`${id}-expiry`} hint={text.expiryHint} error={errors.expires_at}>
              <Select
                id={`${id}-expiry`}
                value={days}
                onChange={(event) => {
                  touch();
                  setDays(Number(event.target.value));
                }}
              >
                {queueDisplays.expiryOptions.map((option) => (
                  <option key={option.days} value={option.days}>
                    {option.label}
                  </option>
                ))}
              </Select>
            </Field>
          </div>

          <FormFooter pending={pending} message={message} submitLabel={text.submit} pendingLabel={text.pending} />
        </CardBody>
      </Card>
    </form>
  );
}
