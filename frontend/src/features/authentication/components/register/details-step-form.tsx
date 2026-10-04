'use client';

import type { FormEvent, ReactNode } from 'react';
import { Alert } from '@/components/ui/alert';
import { ONE_TIME_CODE_MAX_LENGTH } from '@/lib/auth/one-time-code';
import { registerPage } from '../../content/register-page';
import type { RegisterErrors, RegisterForm } from '../../lib/check-registration';
import { SignInField } from '../sign-in/sign-in-field';
import { SignInSubmit } from '../sign-in/sign-in-submit';
import { PrivacyNote } from './privacy-note';

interface DetailsStepFormProps {
  onSubmit: (form: RegisterForm) => void;
  submitting: boolean;
  /** Problems found in the browser or refused by the backend, one per field. */
  errors: RegisterErrors;
  /** The resend button, placed under the code field where the visitor looks when no code came. */
  resend: ReactNode;
}

/**
 * Step two of sign-up: the code from the SMS, a name, an optional email and a password.
 *
 * The inputs are left uncontrolled and the form is never reset, so a refusal from the backend
 * leaves everything typed in place except what needs fixing.
 */
export function DetailsStepForm({ onSubmit, submitting, errors, resend }: DetailsStepFormProps) {
  const text = registerPage.details;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const read = (name: string) => String(form.get(name) ?? '');

    onSubmit({
      code: read('code'),
      displayName: read('display_name'),
      email: read('email'),
      password: read('password'),
      confirm: read('confirm'),
    });
  }

  return (
    <form onSubmit={submit} className="space-y-5" noValidate>
      {errors.general && <Alert>{errors.general}</Alert>}

      <div className="space-y-2">
        <SignInField
          id="register-code"
          name="code"
          label={text.codeLabel}
          hint={text.codeHint}
          error={errors.code}
          inputMode="numeric"
          autoComplete="one-time-code"
          maxLength={ONE_TIME_CODE_MAX_LENGTH}
          required
          autoFocus
          className="text-center font-mono text-lg tracking-[0.4em]"
        />
        {resend}
      </div>

      <SignInField
        id="register-name"
        name="display_name"
        label={text.nameLabel}
        placeholder={text.namePlaceholder}
        error={errors.display_name}
        autoComplete="name"
        maxLength={200}
        required
      />

      <SignInField
        id="register-email"
        name="email"
        type="email"
        label={text.emailLabel}
        hint={text.emailHint}
        error={errors.email}
        autoComplete="email"
        maxLength={256}
      />

      <SignInField
        id="register-password"
        name="password"
        type="password"
        label={text.passwordLabel}
        hint={text.passwordHint}
        error={errors.password}
        autoComplete="new-password"
        maxLength={256}
        required
      />

      <SignInField
        id="register-confirm"
        name="confirm"
        type="password"
        label={text.confirmLabel}
        error={errors.confirm}
        autoComplete="new-password"
        maxLength={256}
        required
      />

      <SignInSubmit disabled={submitting}>{submitting ? text.submitting : text.submit}</SignInSubmit>
      <PrivacyNote />
    </form>
  );
}
