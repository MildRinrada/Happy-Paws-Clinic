'use client';

import { useState, useTransition } from 'react';
import { fieldErrors } from '@/lib/actions/field-errors';
import { setNewPassword } from '../../actions/password-reset';
import { resetPasswordPage } from '../../content/password-reset-page';
import { useLinkToken } from '../../hooks/use-link-token';
import { NewPasswordForm, type NewPasswordErrors } from './new-password-form';
import { PasswordResetDone } from './password-reset-done';
import { ResetLinkInvalid } from './reset-link-invalid';

type Step = 'form' | 'done' | 'invalid';

/**
 * Where a reset link lands: read the token from the fragment, ask for the new password, send both
 * to the server action. The token never goes into the page's address or any request but that one.
 *
 * Whether the link is still good is only known once it is used: the backend has no "check this
 * token" call, and asking would not be cheaper than trying. So the form shows first, and a dead
 * link turns into the "ask for a new one" message after the attempt.
 */
export function ResetPasswordFlow() {
  const token = useLinkToken();
  const [step, setStep] = useState<Step>('form');
  const [errors, setErrors] = useState<NewPasswordErrors>({});
  const [pending, startTransition] = useTransition();

  if (token === undefined) {
    return <p className="mt-10 text-[15px] text-site-muted">{resetPasswordPage.reading}</p>;
  }

  if (token === null || step === 'invalid') {
    return <ResetLinkInvalid />;
  }

  if (step === 'done') {
    return <PasswordResetDone />;
  }

  function save(linkToken: string, password: string) {
    setErrors({});
    startTransition(async () => {
      try {
        const result = await setNewPassword({ token: linkToken, password });

        if (result.ok) {
          setStep('done');
          return;
        }

        const perField = fieldErrors(result.error);

        if (perField.token) {
          setStep('invalid');
        } else if (perField.new_password) {
          setErrors({ password: perField.new_password });
        } else {
          setErrors({ general: result.error.message });
        }
      } catch {
        setErrors({ general: resetPasswordPage.errors.unreachable });
      }
    });
  }

  return <NewPasswordForm onSubmit={(password) => save(token, password)} submitting={pending} errors={errors} />;
}
