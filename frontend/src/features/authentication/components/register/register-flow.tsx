'use client';

import { useState, useTransition } from 'react';
import { Alert } from '@/components/ui/alert';
import { requestRegisterCode, registerCustomer } from '../../actions/register';
import { registerPage } from '../../content/register-page';
import { useCooldown } from '../../hooks/use-cooldown';
import { checkRegistration, type RegisterErrors, type RegisterForm } from '../../lib/check-registration';
import { isValidPhone } from '../../lib/phone-number';
import { placeRegisterError } from '../../lib/place-register-error';
import { CodeSentNote } from './code-sent-note';
import { DetailsStepForm } from './details-step-form';
import { PhoneStepForm } from './phone-step-form';
import { ResendCodeButton } from './resend-code-button';
import { SignInPrompt } from './sign-in-prompt';

/** How long the resend button waits after each code. */
const RESEND_COOLDOWN_SECONDS = 60;

type Step = { kind: 'phone' } | { kind: 'details'; expiresAt: string };

/**
 * The whole sign-up: a phone number, then the code sent to it with the rest of the details.
 *
 * The number is kept here, not in the second form, because it is what the code was sent to and
 * the backend checks the pair. Changing it means going back and asking for a new code.
 *
 * Success never comes back to this component: the server action signs the new customer in and
 * redirects to the portal. Only refusals do.
 */
export function RegisterFlow() {
  const [step, setStep] = useState<Step>({ kind: 'phone' });
  const [phone, setPhone] = useState('');
  const [phoneError, setPhoneError] = useState<string>();
  const [errors, setErrors] = useState<RegisterErrors>({});
  const [resent, setResent] = useState(false);
  const [sending, startSending] = useTransition();
  const [registering, startRegistering] = useTransition();
  const cooldown = useCooldown(RESEND_COOLDOWN_SECONDS);

  /** Asks for a code. On the first step a refusal stays by the number; on the second it goes on top. */
  function sendCode(number: string) {
    const onDetails = step.kind === 'details';
    const refuse = (message: string) => (onDetails ? setErrors({ general: message }) : setPhoneError(message));

    setPhoneError(undefined);
    setResent(false);

    if (!isValidPhone(number)) {
      refuse(registerPage.errors.badPhone);
      return;
    }

    startSending(async () => {
      try {
        const result = await requestRegisterCode({ phone: number });

        if (!result.ok) {
          refuse(result.error.message);
          return;
        }

        setPhone(number);
        setErrors({});
        setResent(onDetails);
        setStep({ kind: 'details', expiresAt: result.data.expiresAt });
        cooldown.start();
      } catch {
        refuse(registerPage.errors.unreachable);
      }
    });
  }

  function register(form: RegisterForm) {
    const problems = checkRegistration(form);
    setErrors(problems);
    setResent(false);

    if (Object.keys(problems).length > 0) {
      return;
    }

    startRegistering(async () => {
      try {
        const result = await registerCustomer({
          phone,
          code: form.code,
          displayName: form.displayName,
          email: form.email,
          password: form.password,
        });

        // Success navigates away; only a refusal comes back here.
        if (!result || result.ok) return;

        setErrors(placeRegisterError(result.error));
      } catch {
        setErrors({ general: registerPage.errors.unreachable });
      }
    });
  }

  if (step.kind === 'phone') {
    return (
      <>
        <PhoneStepForm defaultPhone={phone} onSubmit={sendCode} sending={sending} error={phoneError} />
        <SignInPrompt />
      </>
    );
  }

  return (
    <>
      <div className="mt-10 space-y-5">
        <CodeSentNote
          phone={phone}
          expiresAt={step.expiresAt}
          onChangeNumber={() => {
            setErrors({});
            setResent(false);
            setStep({ kind: 'phone' });
          }}
        />

        {resent && <Alert tone="info">{registerPage.codeSent.resent}</Alert>}

        <DetailsStepForm
          onSubmit={register}
          submitting={registering}
          errors={errors}
          resend={
            <ResendCodeButton remaining={cooldown.remaining} sending={sending} onResend={() => sendCode(phone)} />
          }
        />
      </div>
      <SignInPrompt />
    </>
  );
}
