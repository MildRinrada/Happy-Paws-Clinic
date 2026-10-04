'use client';

import type { FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { registerPage } from '../../content/register-page';
import { SignInField } from '../sign-in/sign-in-field';
import { SignInSubmit } from '../sign-in/sign-in-submit';

interface PhoneStepFormProps {
  /** The number typed before, kept when the visitor comes back to change it. */
  defaultPhone: string;
  onSubmit: (phone: string) => void;
  sending: boolean;
  /** A refusal about the number, or about asking too often, in the backend's words. */
  error?: string;
}

/** Step one of sign-up: the phone number the code is sent to. */
export function PhoneStepForm({ defaultPhone, onSubmit, sending, error }: PhoneStepFormProps) {
  const text = registerPage.phone;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    onSubmit(String(new FormData(event.currentTarget).get('phone') ?? ''));
  }

  return (
    <form onSubmit={submit} className="mt-10 space-y-5" noValidate>
      {error && <Alert>{error}</Alert>}

      <SignInField
        id="register-phone"
        name="phone"
        type="tel"
        inputMode="tel"
        autoComplete="tel"
        label={text.label}
        hint={text.hint}
        placeholder={text.placeholder}
        defaultValue={defaultPhone}
        maxLength={32}
        required
        autoFocus
      />

      <SignInSubmit disabled={sending}>{sending ? text.submitting : text.submit}</SignInSubmit>
    </form>
  );
}
