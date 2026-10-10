'use client';

import { useRouter } from 'next/navigation';
import { useCallback, useState } from 'react';
import { useCooldown } from '@/features/authentication/hooks/use-cooldown';
import { ReferredNote } from '@/features/referrals/components/guest/referred-note';
import { clinicDay } from '@/lib/format/datetime';
import { bookAction } from '../../actions/booking-actions';
import { startGuestBookingAction, verifyGuestBookingAction } from '../../actions/guest-booking-actions';
import { bookingPage } from '../../content/booking-page';
import { guestBookingPage } from '../../content/guest-booking-page';
import { suggestedTimes } from '../../lib/slot-times';
import type {
  BookingDetails,
  BookingOptions,
  GuestBookingStarted,
  GuestVerified,
  PickedTime,
  TimePreselect,
} from '../../types/booking.types';
import { BookingSteps } from '../booking-steps';
import { DetailsStep } from '../details-step';
import { TimeStep, type PickRefusal } from '../time-step';
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
 * Nothing is held while the visitor browses times. Sending the phone holds the picked time for ten
 * minutes and texts a code; the code signs them in as a customer (new or returning, the backend
 * decides); the last step books through the same `POST /portal/appointments` as the portal, with an
 * idempotency key per proved hold, so a double tap books once.
 */
export function GuestBookingWizard({
  options,
  initial,
  start,
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

  const [day, setDay] = useState<string | undefined>(start?.day);
  const [refusal, setRefusal] = useState<PickRefusal | undefined>(start?.notice ? { message: start.notice, suggested: [] } : undefined);
  const [picked, setPicked] = useState<PickedTime | undefined>(start?.startsAt ? { startsAt: start.startsAt } : undefined);

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

  function pick(startsAt: string, staffId?: string) {
    setPicked({ startsAt, staffId });
    setDay(clinicDay(startsAt));
    setRefusal(undefined);
    setPhoneError(undefined);
    setStep('phone');
  }

  async function sendCode(number: string, resend = false) {
    if (!picked) return;
    setSending(true);
    setPhoneError(undefined);

    try {
      const result = await startGuestBookingAction({
        phone: number,
        serviceId: details.serviceId,
        branchId: details.branchId,
        startsAt: picked.startsAt,
        staffId: picked.staffId,
      });

      if (result.ok) {
        setPhone(number);
        setStarted(result.data);
        setExpired(false);
        setCodeErrors({});
        cooldown.start();
        setStep('code');
      } else if (result.error.code === 'slot_unavailable') {
        // Taken while the visitor was typing: back to the times, with what the backend offered.
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
  const branch = options.branches.length > 1 ? options.branches.find((item) => item.id === details.branchId) : undefined;
  const heldBy = started?.hold.staff_id ?? picked?.staffId;
  const summary = picked && {
    service: service?.name,
    startsAt: started?.hold.starts_at ?? picked.startsAt,
    vet: heldBy ? (options.vets[heldBy] ?? bookingPage.vetFallback) : bookingPage.vetFallback,
    branch: branch?.name,
  };
  const portalBooking = `/portal/book?${new URLSearchParams({ service: details.serviceId, branch: details.branchId })}`;

  return (
    <div className="space-y-6" data-booking-flow="guest" data-booking-step={step}>
      <BookingSteps current={STEPS.indexOf(step)} labels={guestBookingPage.steps} />

      {referral && step !== 'pet' && <ReferredNote />}

      {step === 'details' && (
        <DetailsStep
          options={options}
          value={details}
          onChange={setDetails}
          askPet={false}
          askType={false}
          onNext={() => {
            setRefusal(undefined);
            setStep('time');
          }}
        />
      )}

      {step === 'time' && (
        <TimeStep
          serviceId={details.serviceId}
          branchId={details.branchId}
          vetNames={options.vets}
          onPick={pick}
          holding={false}
          refusal={refusal}
          onBack={() => setStep('details')}
          initialDay={day}
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
    </div>
  );
}
