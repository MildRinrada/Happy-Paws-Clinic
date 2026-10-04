'use client';

import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { ONE_TIME_CODE_MAX_LENGTH, ONE_TIME_CODE_PATTERN, oneTimeCodeDigits } from '@/lib/auth/one-time-code';
import { invitationPage } from '../../content/invitation-page';
import { StepHeading } from './step-heading';

export interface PasswordFormErrors {
  password?: string;
  code?: string;
  general?: string;
}

interface SetPasswordFormProps {
  onSubmit: (password: string, code: string) => void;
  submitting: boolean;
  /** Refusals from the backend, shown on the field they name. */
  errors: PasswordFormErrors;
}

/** Checks what can be checked without the backend, so a typo costs no round trip. */
function check(password: string, confirm: string, code: string): PasswordFormErrors {
  const text = invitationPage.password;

  if (password.length < 8) return { password: text.tooShort };
  if (password !== confirm) return { password: text.mismatch };
  if (!/^\d{6,8}$/.test(code)) return { code: text.badCode };
  return {};
}

/** Step 2: the password, typed twice, and a code that proves the authenticator was set up. */
export function SetPasswordForm({ onSubmit, submitting, errors }: SetPasswordFormProps) {
  const text = invitationPage.password;
  const [local, setLocal] = useState<PasswordFormErrors>({});
  const shown = { ...errors, ...local };

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const form = new FormData(event.currentTarget);
    const password = String(form.get('password') ?? '');
    const code = oneTimeCodeDigits(String(form.get('code') ?? ''));
    const problems = check(password, String(form.get('confirm') ?? ''), code);

    setLocal(problems);

    if (Object.keys(problems).length === 0) {
      onSubmit(password, code);
    }
  }

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      <StepHeading label={text.stepLabel} title={text.title} />

      {shown.general && <Alert>{shown.general}</Alert>}

      <Field label={text.passwordLabel} htmlFor="invite-password" hint={text.passwordHint} error={shown.password}>
        <Input
          id="invite-password"
          name="password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          maxLength={256}
          required
          aria-invalid={Boolean(shown.password)}
        />
      </Field>

      <Field label={text.confirmLabel} htmlFor="invite-confirm">
        <Input id="invite-confirm" name="confirm" type="password" autoComplete="new-password" required />
      </Field>

      <Field label={text.codeLabel} htmlFor="invite-code" hint={text.codeHint} error={shown.code}>
        <Input
          id="invite-code"
          name="code"
          inputMode="numeric"
          autoComplete="one-time-code"
          pattern={ONE_TIME_CODE_PATTERN}
          maxLength={ONE_TIME_CODE_MAX_LENGTH}
          required
          aria-invalid={Boolean(shown.code)}
          className="text-center font-mono text-lg tracking-[0.4em]"
        />
      </Field>

      <Button type="submit" className="w-full" disabled={submitting}>
        {submitting ? text.submitting : text.submit}
      </Button>
    </form>
  );
}
