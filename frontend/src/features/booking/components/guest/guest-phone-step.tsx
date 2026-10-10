'use client';

import { useId, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { SignInField } from '@/features/authentication/components/sign-in/sign-in-field';
import { guestBookingPage } from '../../content/guest-booking-page';
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
    <form onSubmit={submit} className="space-y-5" noValidate>
      <div className="space-y-1">
        <h2 className="text-base font-semibold text-ink-900">{text.title}</h2>
        <p className="text-sm text-site-muted">{text.lead}</p>
      </div>

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

      <div className="flex flex-col gap-3 sm:flex-row">
        <button
          type="submit"
          disabled={sending}
          className="h-12 flex-1 rounded-xl bg-site-orange text-sm font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {sending ? text.submitting : text.submit}
        </button>
        <button
          type="button"
          onClick={onBack}
          disabled={sending}
          className="h-12 rounded-xl border border-ink-200 px-5 text-sm font-semibold text-ink-700 hover:bg-ink-50"
        >
          {text.back}
        </button>
      </div>
    </form>
  );
}
