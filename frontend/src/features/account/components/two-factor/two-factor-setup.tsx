'use client';

import { useState } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { twoFactorSettings } from '../../content/two-factor-settings';
import type { AuthenticatorSecret } from '../../types/account.types';
import { EnrolCodeStep } from './enrol-code-step';
import { EnrolPasswordStep } from './enrol-password-step';
import { RecoveryCodes } from './recovery-codes';

type Step =
  | { name: 'idle' }
  | { name: 'password' }
  | { name: 'scan'; setup: AuthenticatorSecret }
  | { name: 'codes'; codes: string[] }
  | { name: 'done' };

/**
 * The two calls of `POST /me/2fa/enroll` as a short wizard: password, then scan and confirm, then
 * the recovery codes. The secret and the codes live only in this component's state; leaving the
 * page drops them, which for the codes is the point.
 */
export function TwoFactorSetup({ enabled }: { enabled: boolean }) {
  const text = twoFactorSettings;
  const [step, setStep] = useState<Step>({ name: 'idle' });
  const reset = () => setStep({ name: 'idle' });

  switch (step.name) {
    case 'password':
      return <EnrolPasswordStep onReady={(setup) => setStep({ name: 'scan', setup })} onCancel={reset} />;

    case 'scan':
      return (
        <EnrolCodeStep setup={step.setup} onEnabled={(codes) => setStep({ name: 'codes', codes })} onCancel={reset} />
      );

    case 'codes':
      return <RecoveryCodes codes={step.codes} onDone={() => setStep({ name: 'done' })} />;

    case 'done':
      return <Alert tone="success">{text.enabled}</Alert>;

    default:
      return (
        <div className="space-y-3">
          <p className="text-sm text-ink-600">{enabled ? text.intro.enabled : text.intro.disabled}</p>
          <Button variant={enabled ? 'secondary' : 'primary'} onClick={() => setStep({ name: 'password' })}>
            {enabled ? text.restart : text.start}
          </Button>
        </div>
      );
  }
}
