'use client';

import { useActionState } from 'react';
import { Alert } from '@/components/ui/alert';
import { requestPasswordReset, type ForgotPasswordState } from '../../actions/password-reset';
import { forgotPasswordPage } from '../../content/password-reset-page';
import { SignInField } from '../sign-in/sign-in-field';
import { SignInSubmit } from '../sign-in/sign-in-submit';
import { ResetLinkSent } from './reset-link-sent';

/**
 * Asks for a reset link by email or phone. Once asked, the form gives way to the same neutral
 * confirmation every time. `onAgain` brings a fresh form back (the parent remounts this one).
 */
export function ForgotPasswordForm({ onAgain }: { onAgain: () => void }) {
  const [state, submit, pending] = useActionState(requestPasswordReset, { status: 'idle' } satisfies ForgotPasswordState);
  const text = forgotPasswordPage;

  if (state.status === 'sent') {
    return <ResetLinkSent onAgain={onAgain} />;
  }

  return (
    <form action={submit} className="mt-10 space-y-5">
      {state.error && <Alert>{state.error}</Alert>}

      <SignInField
        id="forgot-identifier"
        name="identifier"
        label={text.identifierLabel}
        autoComplete="username"
        defaultValue={state.identifier}
        maxLength={256}
        required
        autoFocus
      />

      <SignInSubmit disabled={pending}>{pending ? text.submitting : text.submit}</SignInSubmit>
    </form>
  );
}
