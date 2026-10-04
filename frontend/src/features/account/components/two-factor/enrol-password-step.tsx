'use client';

import { useState, type FormEvent } from 'react';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { beginTwoFactorAction } from '../../actions/my-account-actions';
import { twoFactorSettings } from '../../content/two-factor-settings';
import type { AuthenticatorSecret } from '../../types/account.types';

interface EnrolPasswordStepProps {
  onReady: (secret: AuthenticatorSecret) => void;
  onCancel: () => void;
}

/** Step 1: the password again, so a borrowed signed-in browser alone cannot plant a new factor. */
export function EnrolPasswordStep({ onReady, onCancel }: EnrolPasswordStepProps) {
  const text = twoFactorSettings.password;
  const [password, setPassword] = useState('');
  const { errors, message, pending, submit, touch } = useFormSubmit(formMessages);

  function start(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => beginTwoFactorAction({ current_password: password }), onReady);
  }

  return (
    <form onSubmit={start} className="space-y-4" noValidate>
      <Field label={text.label} htmlFor="tfa-password" hint={text.hint} error={errors.current_password}>
        <Input
          id="tfa-password"
          type="password"
          value={password}
          maxLength={256}
          required
          autoComplete="current-password"
          aria-invalid={Boolean(errors.current_password)}
          onChange={(event) => {
            touch();
            setPassword(event.target.value);
          }}
        />
      </Field>

      <FormFooter pending={pending} message={message} onCancel={onCancel} submitLabel={text.submit} pendingLabel={text.submitting} />
    </form>
  );
}
