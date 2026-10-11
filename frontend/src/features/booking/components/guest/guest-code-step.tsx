'use client';

import Link from 'next/link';
import { useId, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { SignInField } from '@/features/authentication/components/sign-in/sign-in-field';
import { ONE_TIME_CODE_MAX_LENGTH } from '@/lib/auth/one-time-code';
import { formatTime } from '@/lib/format/datetime';
import { guestBookingPage } from '../../content/guest-booking-page';
import { StepActions } from '../frame/step-actions';
import { StepBackButton } from '../frame/step-back-button';
import { StepCard } from '../frame/step-card';
import { StepNextButton } from '../frame/step-next-button';
import { HoldCountdown } from '../hold-countdown';
import { ConsentCheckbox } from './consent-checkbox';

/** What the code step collects. The phone is already known. */
export interface GuestCodeForm {
  code: string;
  displayName: string;
  acceptPrivacy: boolean;
  marketingConsent: boolean;
}

/** Refusals by field, in the API's field names, plus one for the whole form. */
export type GuestCodeErrors = Partial<Record<'code' | 'display_name' | 'accept_privacy_notice' | 'general', string>>;

interface GuestCodeStepProps {
  phone: string;
  codeExpiresAt: string;
  holdExpiresAt: string;
  expired: boolean;
  onExpire: () => void;
  onSubmit: (form: GuestCodeForm) => void;
  submitting: boolean;
  errors: GuestCodeErrors;
  /** Seconds before another code may be asked for. */
  resendIn: number;
  resending: boolean;
  onResend: () => void;
  onChangePhone: () => void;
  onPickAgain: () => void;
}

/**
 * Step 4 of `/book`: the code from the SMS, a name, and the privacy notice. Asked of everyone the
 * same way, because nobody may be told whether the number has an account before the code is right.
 */
export function GuestCodeStep(props: GuestCodeStepProps) {
  const { phone, codeExpiresAt, holdExpiresAt, expired, onExpire, onSubmit, submitting, errors } = props;
  const text = guestBookingPage.code;
  const codeId = useId();
  const nameId = useId();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);

    onSubmit({
      code: String(form.get('code') ?? ''),
      displayName: String(form.get('display_name') ?? ''),
      acceptPrivacy: form.get('accept_privacy_notice') === 'on',
      marketingConsent: form.get('marketing_consent') === 'on',
    });
  }

  if (expired) {
    return (
      <StepCard title={text.title}>
        <div className="space-y-5">
          <Alert>{text.expired}</Alert>
          <StepNextButton onClick={props.onPickAgain} block>
            {guestBookingPage.phone.back}
          </StepNextButton>
        </div>
      </StepCard>
    );
  }

  return (
    <StepCard title={text.title} lead={`${text.sentTo(phone)} ${text.codeExpires(formatTime(codeExpiresAt))}`}>
      <form onSubmit={submit} className="space-y-5" noValidate>
        <p className="rounded-xl bg-site-soft px-4 py-3 text-sm text-ink-800">
          {text.held(formatTime(holdExpiresAt))} (<HoldCountdown expiresAt={holdExpiresAt} onExpire={onExpire} />)
        </p>

        {errors.general && <Alert>{errors.general}</Alert>}

        <div className="space-y-2">
          <SignInField
            id={codeId}
            name="code"
            label={text.codeLabel}
            error={errors.code}
            inputMode="numeric"
            autoComplete="one-time-code"
            maxLength={ONE_TIME_CODE_MAX_LENGTH}
            required
            autoFocus
            className="text-center font-mono text-lg tracking-[0.4em]"
          />
          <button
            type="button"
            onClick={props.onResend}
            disabled={props.resendIn > 0 || props.resending}
            className="text-sm font-bold text-site-forest underline underline-offset-2 hover:text-site-teal disabled:cursor-not-allowed disabled:font-normal disabled:text-site-muted disabled:no-underline"
          >
            {props.resending ? text.resending : props.resendIn > 0 ? text.resendIn(props.resendIn) : text.resend}
          </button>
        </div>

        <SignInField
          id={nameId}
          name="display_name"
          label={text.nameLabel}
          hint={text.nameHint}
          placeholder={text.namePlaceholder}
          error={errors.display_name}
          autoComplete="name"
          maxLength={200}
          required
        />

        <ConsentCheckbox name="accept_privacy_notice" error={errors.accept_privacy_notice}>
          {text.privacyBefore}{' '}
          <Link href="/privacy" target="_blank" className="font-semibold text-site-forest underline">
            {text.privacyLink}
          </Link>{' '}
          {text.privacyAfter}
        </ConsentCheckbox>

        <ConsentCheckbox name="marketing_consent">{text.marketing}</ConsentCheckbox>

        <StepActions
          back={
            <StepBackButton onClick={props.onChangePhone} disabled={submitting}>
              {text.changePhone}
            </StepBackButton>
          }
          next={
            <StepNextButton type="submit" disabled={submitting}>
              {submitting ? text.submitting : text.submit}
            </StepNextButton>
          }
        />
      </form>
    </StepCard>
  );
}
