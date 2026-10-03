'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { fieldErrors } from '@/lib/actions/field-errors';
import { registerCustomerAction } from '../actions/customer-actions';
import { registerCustomer } from '../content/register-customer';
import { duplicatesIn } from '../lib/duplicate-customers';
import type { Customer } from '../types/customer.types';
import { DuplicateCustomerNotice } from './duplicate-customer-notice';

/**
 * Name, phone and an optional email. On success the action opens the new customer's page.
 *
 * A phone already on file stops the form with the matching records. Editing the number clears
 * the stop, since the matches were for the old one.
 */
export function RegisterCustomerForm() {
  const text = registerCustomer;
  const [displayName, setDisplayName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string>();
  const [duplicates, setDuplicates] = useState<Customer[]>([]);
  const [pending, startTransition] = useTransition();

  function save(allowDuplicatePhone: boolean) {
    setErrors({});
    setMessage(undefined);

    startTransition(async () => {
      try {
        const result = await registerCustomerAction({ displayName, phone, email, allowDuplicatePhone });

        // Success navigates away; only a refusal comes back here.
        if (result.ok) return;

        const matches = duplicatesIn(result.error);
        if (matches.length > 0) {
          setDuplicates(matches);
          return;
        }

        const byField = fieldErrors(result.error);
        setErrors(byField);
        if (Object.keys(byField).length === 0) setMessage(result.error.message);
      } catch {
        setMessage(text.unreachable);
      }
    });
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    save(false);
  }

  return (
    <form onSubmit={submit} className="space-y-4">
      {message && <Alert>{message}</Alert>}

      <Field label={text.fields.displayName} hint={text.fields.displayNameHint} htmlFor="customer-name" error={errors.display_name}>
        <Input
          id="customer-name"
          value={displayName}
          onChange={(event) => setDisplayName(event.target.value)}
          maxLength={200}
          required
          autoFocus
          aria-invalid={Boolean(errors.display_name)}
        />
      </Field>

      <Field label={text.fields.phone} hint={text.fields.phoneHint} htmlFor="customer-phone" error={errors.phone}>
        <Input
          id="customer-phone"
          type="tel"
          inputMode="tel"
          value={phone}
          onChange={(event) => {
            setPhone(event.target.value);
            setDuplicates([]);
          }}
          maxLength={32}
          required
          aria-invalid={Boolean(errors.phone)}
        />
      </Field>

      <Field label={text.fields.email} htmlFor="customer-email" error={errors.email}>
        <Input
          id="customer-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          maxLength={256}
          aria-invalid={Boolean(errors.email)}
        />
      </Field>

      {duplicates.length > 0 ? (
        <DuplicateCustomerNotice existing={duplicates} pending={pending} onCreateAnyway={() => save(true)} />
      ) : (
        <div className="flex justify-end pt-2">
          <Button type="submit" disabled={pending}>
            {pending ? text.submitting : text.submit}
          </Button>
        </div>
      )}
    </form>
  );
}
