'use client';

import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { resetPasswordPage } from '../../content/password-reset-page';
import { SignInField } from '../sign-in/sign-in-field';
import { SignInSubmit } from '../sign-in/sign-in-submit';

export interface NewPasswordErrors {
  password?: string;
  confirm?: string;
  general?: string;
}

interface NewPasswordFormProps {
  onSubmit: (password: string) => void;
  submitting: boolean;
  /** Refusals from the backend, shown on the field they name. */
  errors: NewPasswordErrors;
}

/** Checks what can be checked without the backend, so a typo costs no round trip. */
function check(password: string, confirm: string): NewPasswordErrors {
  const text = resetPasswordPage.errors;

  if (password.length < 8) return { password: text.tooShort };
  if (password.length > 256) return { password: text.tooLong };
  if (password !== confirm) return { confirm: text.mismatch };
  return {};
}

/** The new password, typed twice. */
export function NewPasswordForm({ onSubmit, submitting, errors }: NewPasswordFormProps) {
  const text = resetPasswordPage;
  const [local, setLocal] = useState<NewPasswordErrors>({});
  const shown = { ...errors, ...local };

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const form = new FormData(event.currentTarget);
    const password = String(form.get('password') ?? '');
    const problems = check(password, String(form.get('confirm') ?? ''));

    setLocal(problems);

    if (Object.keys(problems).length === 0) {
      onSubmit(password);
    }
  }

  return (
    <form onSubmit={submit} className="mt-10 space-y-5" noValidate>
      {shown.general && <Alert>{shown.general}</Alert>}

      <SignInField
        id="reset-password"
        name="password"
        type="password"
        label={text.passwordLabel}
        hint={text.passwordHint}
        error={shown.password}
        autoComplete="new-password"
        maxLength={256}
        required
        autoFocus
      />

      <SignInField
        id="reset-confirm"
        name="confirm"
        type="password"
        label={text.confirmLabel}
        error={shown.confirm}
        autoComplete="new-password"
        maxLength={256}
        required
      />

      <p className="text-sm text-site-muted">{text.signOutNote}</p>

      <SignInSubmit disabled={submitting}>{submitting ? text.submitting : text.submit}</SignInSubmit>
    </form>
  );
}
