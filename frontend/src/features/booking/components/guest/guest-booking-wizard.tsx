'use client';

import { useRouter } from 'next/navigation';
import { useCallback, useState } from 'react';
import { useCooldown } from '@/features/authentication/hooks/use-cooldown';
import { ReferredNote } from '@/features/referrals/components/guest/referred-note';
import { bookAction } from '../../actions/booking-actions';
import { startGuestBookingAction, verifyGuestBookingAction } from '../../actions/guest-booking-actions';
import { bookingFrame } from '../../content/booking-frame';
import { bookingPage } from '../../content/booking-page';
import { guestBookingPage } from '../../content/guest-booking-page';
import { forgetWeekSlots } from '../../hooks/use-week-slots';
import { detailsReady } from '../../lib/details-ready';
import { suggestedTimes } from '../../lib/slot-times';
import type { BookingDetails, BookingOptions, GuestBookingStarted, GuestVerified, TimeChoice, TimePreselect } from '../../types/booking.types';
import { DetailsStep } from '../details-step';
import { BookingFrame } from '../frame/booking-frame';
import { BookingProgress } from '../frame/booking-progress';
import { StepNextButton } from '../frame/step-next-button';
import { BookingSummary } from '../summary/booking-summary';
import { HoldNoteCard } from '../summary/hold-note-card';
import { QuestionsCard } from '../summary/questions-card';
import { BookingTimeStep } from '../time/booking-time-step';
import type { PickRefusal } from '../time-step';
import { GuestCodeStep, type GuestCodeErrors, type GuestCodeForm } from './guest-code-step';
import { GuestPetStep, type PetChoice } from './guest-pet-step';
import { GuestPhoneStep } from './guest-phone-step';

/**
 * Each step is its own named state, and the wrapper carries it as `data-booking-step`, so how far
 * visitors get can be measured later without a tracker on the page.
 */
const STEPS = ['details', 'time', 'phone', 'code', 'pet'] as const;
type GuestStep = (typeof STEPS)[number];

/** Where a finished booking lands: the owner's appointments, with the new one at the top. */
const BOOKED_PATH = '/portal/appointments';

/** A backend refusal by field, for the code step. */
const CODE_FIELDS = ['code', 'display_name', 'accept_privacy_notice'] as const;

/** The type every booking that names none gets. Customers' own type list needs a session. */
const GUEST_TYPE = 'regular';

/**
 * Booking without signing up first (docs/contract-extensions.md § 27): details, a time, the phone,
 * the code, then which pet.
 *
 * Drawn in the booking frame: the progress bar, the step's card, and the summary card beside it,
 * which fills in as the visitor chooses. Picking a time only marks it; "next" moves on with it.
 *
 * Nothing is held while the visitor browses times. Sending the phone holds the picked time for ten
 * minutes and texts a code; the code signs them in as a customer (new or returning, the backend
 * decides); the last step books through the same `POST /portal/appointments` as the portal, with an
 * idempotency key per proved hold, so a double tap books once.
 */
export function GuestBookingWizard({
  options,
  initial,
  start,
  vet,
  referralCode,
}: {
  options: BookingOptions;
  initial: BookingDetails;
  /**
   * A time the address asked for (`?date=&time=`), already checked on the server. A free one opens
   * the form on the phone step with it picked; one taken since opens that day's times with a note.
   */
  start?: TimePreselect;
  /**
   * A vet the address asked for (`?vet=`, from the team page), already checked on the server. The
   * times open on that vet's, and a time from the address is held with them.
   */
  vet?: string;
  /**
   * A friend's referral code from `?ref=`, already checked on the server (docs/contract-extensions.md
   * § 39). Sent with the phone's proof; kept from the first render, so the page re-rendering inside
   * an action cannot drop it.
   */
  referralCode?: string;
}) {
  const router = useRouter();
  const cooldown = useCooldown(60);
  const [referral] = useState(referralCode);
  const [step, setStep] = useState<GuestStep>(start?.startsAt ? 'phone' : start ? 'time' : 'details');
  const [details, setDetails] = useState(initial);

  const [time, setTime] = useState<TimeChoice>({ day: start?.day ?? '', vet: vet ?? '', startsAt: start?.startsAt });
  const [refusal, setRefusal] = useState<PickRefusal | undefined>(start?.notice ? { message: start.notice, suggested: [] } : undefined);

  const [phone, setPhone] = useState('');
  const [started, setStarted] = useState<GuestBookingStarted>();
  const [sending, setSending] = useState(false);
  const [phoneError, setPhoneError] = useState<string>();
  const [expired, setExpired] = useState(false);

  const [verifying, setVerifying] = useState(false);
  const [codeErrors, setCodeErrors] = useState<GuestCodeErrors>({});
  const [verified, setVerified] = useState<{ value: GuestVerified; key: string }>();

  const [booking, setBooking] = useState(false);
  const [bookError, setBookError] = useState<{ message: string; pickAgain: boolean }>();

  const onExpire = useCallback(() => setExpired(true), []);

  /** On from the times with the one picked. Nothing is held until the phone is sent. */
  function leaveTime() {
    if (!time.startsAt) return;
    setRefusal(undefined);
    setPhoneError(undefined);
    setStep('phone');
  }

  /** A change of service or branch drops the time picked: it was free for the old one. */
  function changeDetails(next: BookingDetails) {
    if (next.serviceId !== details.serviceId || next.branchId !== details.branchId) {
      setTime((current) => ({ ...current, startsAt: undefined }));
    }
    setDetails(next);
  }

  function leaveDetails() {
    setRefusal(undefined);
    setStep('time');
  }

  async function sendCode(number: string, resend = false) {
    if (!time.startsAt) return;
    setSending(true);
    setPhoneError(undefined);

    try {
      const result = await startGuestBookingAction({
        phone: number,
        serviceId: details.serviceId,
        branchId: details.branchId,
        startsAt: time.startsAt,
        staffId: time.vet || undefined,
      });

      if (result.ok) {
        setPhone(number);
        setStarted(result.data);
        setExpired(false);
        setCodeErrors({});
        cooldown.start();
        setStep('code');
      } else if (result.error.code === 'slot_unavailable') {
        // Taken while the visitor was typing: back to the times, read afresh, with what the backend offered.
        forgetWeekSlots();
        setTime((current) => ({ ...current, startsAt: undefined }));
        setRefusal({ message: result.error.message, suggested: suggestedTimes(result.error.details) });
        setStep('time');
      } else if (resend) {
        setCodeErrors({ general: result.error.message });
      } else {
        setPhoneError(result.error.message);
      }
    } catch {
      if (resend) setCodeErrors({ general: bookingPage.unreachable });
      else setPhoneError(bookingPage.unreachable);
    } finally {
      setSending(false);
    }
  }

  async function verify(form: GuestCodeForm) {
    if (!started) return;
    setVerifying(true);
    setCodeErrors({});

    try {
      const result = await verifyGuestBookingAction({ holdId: started.hold.id, phone, ...form, referralCode: referral });

      if (result.ok) {
        setVerified({ value: result.data, key: crypto.randomUUID() });
        setStep('pet');
      } else if (result.error.code === 'hold_expired') {
        setExpired(true);
      } else {
        const field = CODE_FIELDS.find((name) => name === result.error.field);
        setCodeErrors(field ? { [field]: result.error.message } : { general: result.error.message });
      }
    } catch {
      setCodeErrors({ general: bookingPage.unreachable });
    } finally {
      setVerifying(false);
    }
  }

  async function book(choice: PetChoice) {
    if (!verified) return;
    setBooking(true);
    setBookError(undefined);

    try {
      const result = await bookAction({
        ...choice,
        hold: verified.value.hold,
        type: GUEST_TYPE,
        reason: details.reason.trim() || undefined,
        idempotencyKey: verified.key,
      });

      if (result.ok) {
        router.push(BOOKED_PATH);
        return;
      }

      // A refusal is a settled answer for that key, so the next try needs a new one.
      setVerified({ value: verified.value, key: crypto.randomUUID() });
      setBookError({
        message: result.error.message,
        pickAgain: result.error.code === 'hold_expired' || result.error.code === 'slot_unavailable',
      });
    } catch {
      setBookError({ message: bookingPage.unreachable, pickAgain: false });
    }

    setBooking(false);
  }

  const service = options.servicesByBranch[details.branchId]?.find((item) => item.id === details.serviceId);
  const branch = options.branches.find((item) => item.id === details.branchId);
  // Once a hold is made (the code and pet steps), it is what is kept: its time and its vet.
  const hold = (step === 'code' || step === 'pet') && started ? started.hold : undefined;
  const startsAt = hold?.starts_at ?? time.startsAt;
  const heldBy = hold?.staff_id ?? (time.vet || undefined);
  const vetName = heldBy ? (options.vets[heldBy] ?? bookingPage.vetFallback) : undefined;
  const summary = startsAt && {
    service: service?.name,
    startsAt,
    vet: vetName ?? bookingPage.vetFallback,
    branch: options.branches.length > 1 ? branch?.name : undefined,
  };
  const portalBooking = `/portal/book?${new URLSearchParams({ service: details.serviceId, branch: details.branchId, ...(vet ? { vet } : {}) })}`;
  // Once the number is proved the visitor is signed in and the hold is theirs: nothing behind can change.
  const canEdit = step !== 'pet';

  function goTo(index: number) {
    const target = STEPS[index]!;
    if (target === 'details') setRefusal(undefined);
    setStep(target);
  }

  const action =
    step === 'details' ? (
      <StepNextButton onClick={leaveDetails} disabled={!detailsReady(options, details, false, false)} arrow={false} block>
        {bookingFrame.next.time}
      </StepNextButton>
    ) : step === 'time' ? (
      <StepNextButton onClick={leaveTime} disabled={!time.startsAt} arrow={false} block>
        {bookingFrame.next.phone}
      </StepNextButton>
    ) : undefined;

  return (
    <BookingFrame
      flow="guest"
      step={step}
      progress={<BookingProgress labels={guestBookingPage.steps} current={STEPS.indexOf(step)} onEdit={canEdit ? goTo : undefined} />}
      note={referral && step !== 'pet' ? <ReferredNote /> : undefined}
      aside={
        <>
          <BookingSummary
            askPet={false}
            values={{ service: service?.name, startsAt, day: time.day || undefined, vet: vetName, branch: branch?.name }}
            onChangeService={canEdit && step !== 'details' ? () => goTo(0) : undefined}
            action={action}
          />
          {(step === 'details' || step === 'time') && <HoldNoteCard>{bookingFrame.holdNote.guest}</HoldNoteCard>}
          <QuestionsCard />
        </>
      }
    >
      {step === 'details' && (
        <DetailsStep options={options} value={details} onChange={changeDetails} askPet={false} askType={false} onNext={leaveDetails} />
      )}

      {step === 'time' && (
        <BookingTimeStep
          serviceId={details.serviceId}
          branchId={details.branchId}
          vetNames={options.vets}
          value={time}
          onChange={setTime}
          initialVet={vet}
          refusal={refusal}
          holding={false}
          onBack={() => goTo(0)}
          onNext={leaveTime}
          nextLabel={bookingFrame.next.phone}
        />
      )}

      {step === 'phone' && summary && (
        <GuestPhoneStep
          summary={summary}
          defaultPhone={phone}
          onSubmit={(number) => sendCode(number)}
          sending={sending}
          error={phoneError}
          onBack={() => setStep('time')}
        />
      )}

      {step === 'code' && started && (
        <GuestCodeStep
          phone={phone}
          codeExpiresAt={started.code_expires_at}
          holdExpiresAt={started.hold.expires_at}
          expired={expired}
          onExpire={onExpire}
          onSubmit={verify}
          submitting={verifying}
          errors={codeErrors}
          resendIn={cooldown.remaining}
          resending={sending}
          onResend={() => sendCode(phone, true)}
          onChangePhone={() => setStep('phone')}
          onPickAgain={() => setStep('time')}
        />
      )}

      {step === 'pet' && verified && summary && (
        <GuestPetStep
          summary={summary}
          pets={verified.value.pets}
          onBook={book}
          submitting={booking}
          error={bookError?.message}
          pickAgainHref={bookError?.pickAgain ? portalBooking : undefined}
        />
      )}
    </BookingFrame>
  );
}
