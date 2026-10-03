'use client';

import { useActionState } from 'react';
import { signIn, type SignInState } from '../../actions/sign-in';
import { CredentialsForm } from './credentials-form';
import { SecondFactorForm } from './second-factor-form';
import { SignUpPrompt } from './sign-up-prompt';

/**
 * The form column's moving part: credentials first, then a code if the account has a second
 * factor. Both steps share one piece of state, advanced by the server.
 *
 * `next` is the page the visitor was sent here from. It starts the state, and the server checks
 * it before sending anyone there.
 */
export function SignInForm({ next }: { next?: string }) {
  const [state, submit, pending] = useActionState(signIn, { step: 'credentials', next } satisfies SignInState);

  if (state.step === 'second_factor') {
    return <SecondFactorForm challenge={state} />;
  }

  return (
    <>
      <CredentialsForm action={submit} pending={pending} error={state.error} identifier={state.identifier} />
      <SignUpPrompt />
    </>
  );
}
