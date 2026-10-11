'use client';

import { useId, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { SignInField } from '@/features/authentication/components/sign-in/sign-in-field';
import { guestBookingPage } from '../../content/guest-booking-page';
import { StepActions } from '../frame/step-actions';
import { StepBackButton } from '../frame/step-back-button';
import { StepCard } from '../frame/step-card';
import { StepNextButton } from '../frame/step-next-button';
import { PickedTimeSummary, type PickedSummary } from './picked-time-summary';

interface GuestPhoneStepProps {
  summary: PickedSummary;
  /** The number typed before, kept when the visitor comes back to change it. */
  defaultPhone: string;
  onSubmit: (phone: string) => void;
  sending: boolean;
  /** A refusal about the number, or about asking too often, in the backend's words. */
  error?: string;
  onBack: () => void;
}

/** Step 3 of `/book`: the chosen time, and the phone the code goes to. Sending it holds the time. */
export function GuestPhoneStep({ summary, defaultPhone, onSubmit, sending, error, onBack }: GuestPhoneStepProps) {
  const text = guestBookingPage.phone;
  const phoneId = useId();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    onSubmit(String(new FormData(event.currentTarget).get('phone') ?? ''));
  }

  return (
    <StepCard title={text.title} lead={text.lead}>
      <form onSubmit={submit} className="space-y-5" noValidate>
        <PickedTimeSummary summary={summary} />

        {error && <Alert>{error}</Alert>}

        <SignInField
          id={phoneId}
          name="phone"
          type="tel"
          inputMode="tel"
          autoComplete="tel"
          label={text.label}
          hint={text.hint}
          placeholder={text.placeholder}
          defaultValue={defaultPhone}
          maxLength={32}
          required
          autoFocus
        />

        <StepActions
          back={
            <StepBackButton onClick={onBack} disabled={sending}>
              {text.back}
            </StepBackButton>
          }
          next={
            <StepNextButton type="submit" disabled={sending}>
              {sending ? text.submitting : text.submit}
            </StepNextButton>
          }
        />
      </form>
    </StepCard>
  );
}
