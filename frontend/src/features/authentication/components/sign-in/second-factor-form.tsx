'use client';

import { useActionState } from 'react';
import { Alert } from '@/components/ui/alert';
import { verifySecondFactor, type SignInState } from '../../actions/sign-in';
import { signInPage } from '../../content/sign-in-page';
import { SignInField } from './sign-in-field';
import { SignInSubmit } from './sign-in-submit';

type Challenge = Extract<SignInState, { step: 'second_factor' }>;

/**
 * Step two, for an account with an authenticator enrolled (every member of staff who touches
 * medical records). The challenge that links the steps lives in state the user cannot see or
 * edit, and expires in five minutes.
 */
export function SecondFactorForm({ challenge }: { challenge: Challenge }) {
  const [state, submitCode, verifying] = useActionState(verifySecondFactor, challenge);
  const error = state.step === 'second_factor' ? state.error : undefined;
  const text = signInPage.secondFactor;

  return (
    <form action={submitCode} className="mt-10 space-y-5">
      <div>
        <h2 className="text-lg font-semibold text-ink-900">{text.heading}</h2>
        <p className="mt-1 text-[15px] text-site-muted">
          {text.intro} <span className="font-medium text-ink-900">{challenge.identifier}</span>
        </p>
      </div>

      {error && <Alert>{error}</Alert>}

      <SignInField
        id="sign-in-code"
        name="code"
        label={text.codeLabel}
        hint={text.codeHint}
        inputMode="numeric"
        autoComplete="one-time-code"
        pattern="[0-9]{6,8}"
        maxLength={8}
        required
        autoFocus
        className="text-center font-mono text-lg tracking-[0.4em]"
      />

      <SignInSubmit disabled={verifying}>{verifying ? text.submitting : text.submit}</SignInSubmit>
    </form>
  );
}
