'use client';

import { useState, type FormEvent } from 'react';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { ONE_TIME_CODE_MAX_LENGTH, ONE_TIME_CODE_PATTERN, oneTimeCodeDigits } from '@/lib/auth/one-time-code';
import { confirmTwoFactorAction } from '../../actions/my-account-actions';
import { twoFactorSettings } from '../../content/two-factor-settings';
import type { AuthenticatorSecret } from '../../types/account.types';
import { AuthenticatorQr } from './authenticator-qr';

interface EnrolCodeStepProps {
  setup: AuthenticatorSecret;
  onEnabled: (recoveryCodes: string[]) => void;
  onCancel: () => void;
}

/**
 * Step 2: scan, then prove the app works with one code. Until this succeeds the old factor (if any)
 * stays in force, so cancelling here changes nothing.
 */
export function EnrolCodeStep({ setup, onEnabled, onCancel }: EnrolCodeStepProps) {
  const text = twoFactorSettings.code;
  const [code, setCode] = useState('');
  const [invalid, setInvalid] = useState(false);
  const { errors, message, pending, submit, touch } = useFormSubmit(formMessages);
  const error = invalid ? text.invalid : errors.code;

  function confirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    // Spaces are how the app shows the code ("162 544"); only the digits are checked and sent.
    const wellFormed = /^\d{6,8}$/.test(oneTimeCodeDigits(code));
    setInvalid(!wellFormed);
    if (!wellFormed) return;

    submit(() => confirmTwoFactorAction({ code }), (result) => onEnabled(result.recoveryCodes));
  }

  return (
    <div className="space-y-5">
      <AuthenticatorQr setup={setup} />

      <form onSubmit={confirm} className="space-y-4" noValidate>
        <Field label={text.label} htmlFor="tfa-code" hint={text.hint} error={error}>
          <Input
            id="tfa-code"
            value={code}
            inputMode="numeric"
            autoComplete="one-time-code"
            pattern={ONE_TIME_CODE_PATTERN}
            maxLength={ONE_TIME_CODE_MAX_LENGTH}
            required
            aria-invalid={Boolean(error)}
            className="max-w-56 text-center font-mono text-lg tracking-[0.3em]"
            onChange={(event) => {
              touch();
              setInvalid(false);
              setCode(event.target.value);
            }}
          />
        </Field>

        <FormFooter pending={pending} message={message} onCancel={onCancel} submitLabel={text.submit} pendingLabel={text.submitting} />
      </form>
    </div>
  );
}
