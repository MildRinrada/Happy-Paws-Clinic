'use client';

import { useState, useTransition } from 'react';
import { beginInvitationSetup, completeInvitation } from '../../actions/accept-invitation';
import { invitationPage } from '../../content/invitation-page';
import { useInvitationToken } from '../../hooks/use-invitation-token';
import { placeAcceptError } from '../../lib/invitation-errors';
import type { AuthenticatorSetup as Setup } from '../../types/invitation.types';
import { AuthenticatorSetup } from './authenticator-setup';
import { InvitationAccepted } from './invitation-accepted';
import { InvitationIntro } from './invitation-intro';
import { InvitationLinkInvalid } from './invitation-link-invalid';
import { SetPasswordForm, type PasswordFormErrors } from './set-password-form';

type Step =
  | { kind: 'intro' }
  | { kind: 'setup'; setup: Setup }
  | { kind: 'accepted'; email: string }
  | { kind: 'invalid' };

/**
 * The whole accept-invitation flow: intro, authenticator setup with password, done.
 *
 * Setup starts on a button press rather than on load. Each start asks the backend for the secret,
 * and a page that did it by itself would do it again on every reload, leaving the invitee unsure
 * which QR code is the live one.
 */
export function InvitationFlow({ invitationId }: { invitationId: string }) {
  const token = useInvitationToken();
  const [step, setStep] = useState<Step>({ kind: 'intro' });
  const [errors, setErrors] = useState<PasswordFormErrors>({});
  const [pending, startTransition] = useTransition();

  if (token === undefined) {
    return <p className="text-center text-sm text-ink-500">{invitationPage.reading}</p>;
  }

  if (token === null || step.kind === 'invalid') {
    return <InvitationLinkInvalid />;
  }

  function start(linkToken: string) {
    setErrors({});
    startTransition(async () => {
      try {
        const result = await beginInvitationSetup({ invitationId, token: linkToken });

        if (result.ok) {
          setStep({ kind: 'setup', setup: result.data });
          return;
        }

        const placed = placeAcceptError(result.error);
        if (placed === 'invalid') setStep({ kind: 'invalid' });
        else setErrors(placed);
      } catch {
        setErrors({ general: invitationPage.errors.unreachable });
      }
    });
  }

  function complete(linkToken: string, password: string, code: string) {
    setErrors({});
    startTransition(async () => {
      try {
        const result = await completeInvitation({ invitationId, token: linkToken, password, code });

        if (result.ok) {
          setStep({ kind: 'accepted', email: result.data.email });
          return;
        }

        const placed = placeAcceptError(result.error);
        if (placed === 'invalid') setStep({ kind: 'invalid' });
        else setErrors(placed);
      } catch {
        setErrors({ general: invitationPage.errors.unreachable });
      }
    });
  }

  if (step.kind === 'accepted') {
    return <InvitationAccepted email={step.email} />;
  }

  if (step.kind === 'intro') {
    return <InvitationIntro onStart={() => start(token)} starting={pending} error={errors.general} />;
  }

  return (
    <div className="space-y-8">
      <AuthenticatorSetup setup={step.setup} />
      <hr className="border-ink-200" />
      <SetPasswordForm
        onSubmit={(password, code) => complete(token, password, code)}
        submitting={pending}
        errors={errors}
      />
    </div>
  );
}
