import { invitationPage } from '../../content/invitation-page';
import type { AuthenticatorSetup as Setup } from '../../types/invitation.types';
import { CopySecretButton } from './copy-secret-button';
import { StepHeading } from './step-heading';

/** Step 1: the QR code to scan, and the same secret as text for anyone who cannot scan. */
export function AuthenticatorSetup({ setup }: { setup: Setup }) {
  const text = invitationPage.setup;

  return (
    <section className="space-y-4">
      <StepHeading label={text.stepLabel} title={text.title} />

      <p className="text-sm text-ink-600">
        {text.accountLabel} <span className="font-medium text-ink-900">{setup.email}</span>
      </p>

      {/* A data URL drawn on the server, so next/image has nothing to optimise. */}
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        src={setup.qrDataUrl}
        alt={text.qrAlt}
        width={224}
        height={224}
        className="mx-auto rounded-lg border border-ink-200 bg-white p-2"
      />

      <div className="space-y-1.5">
        <p className="text-sm text-ink-600">{text.manualLabel}</p>
        <div className="flex items-center gap-2">
          <code className="min-w-0 flex-1 break-all rounded-lg bg-ink-50 px-3 py-2 font-mono text-sm text-ink-900">
            {setup.secret}
          </code>
          <CopySecretButton secret={setup.secret} />
        </div>
      </div>
    </section>
  );
}
