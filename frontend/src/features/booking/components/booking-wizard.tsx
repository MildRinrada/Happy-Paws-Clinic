'use client';

import { useCallback, useState } from 'react';
import { bookAction, holdSlotAction } from '../actions/booking-actions';
import { bookingPage } from '../content/booking-page';
import { suggestedTimes } from '../lib/slot-times';
import type { BookingDetails, BookingOptions, SlotHold } from '../types/booking.types';
import { BookingDone } from './booking-done';
import { BookingSteps } from './booking-steps';
import { ConfirmStep } from './confirm-step';
import { DetailsStep } from './details-step';
import { TimeStep, type PickRefusal } from './time-step';

const DETAILS = 0;
const TIME = 1;
const CONFIRM = 2;
const DONE = 3;

/**
 * The booking form from start to finish: details, a time, then confirming.
 *
 * Picking a time holds it at once (`POST /booking/holds`), so nobody else can take it while the
 * owner reads the summary. The idempotency key belongs to that hold: a double tap on "ยืนยัน"
 * books once, and choosing a new time starts a new key.
 */
export function BookingWizard({ options, initial }: { options: BookingOptions; initial: BookingDetails }) {
  const [step, setStep] = useState(DETAILS);
  const [details, setDetails] = useState(initial);

  const [holding, setHolding] = useState(false);
  const [refusal, setRefusal] = useState<PickRefusal>();
  const [hold, setHold] = useState<{ value: SlotHold; key: string }>();
  const [expired, setExpired] = useState(false);

  const [submitting, setSubmitting] = useState(false);
  const [confirmError, setConfirmError] = useState<string>();
  const [bookedAt, setBookedAt] = useState<string>();

  const onExpire = useCallback(() => setExpired(true), []);

  async function pick(startsAt: string, staffId?: string) {
    setHolding(true);
    setRefusal(undefined);

    try {
      const result = await holdSlotAction({ serviceId: details.serviceId, branchId: details.branchId, startsAt, staffId });

      if (result.ok) {
        setHold({ value: result.data, key: crypto.randomUUID() });
        setExpired(false);
        setConfirmError(undefined);
        setStep(CONFIRM);
      } else {
        setRefusal({ message: result.error.message, suggested: suggestedTimes(result.error.details) });
      }
    } catch {
      setRefusal({ message: bookingPage.unreachable, suggested: [] });
    } finally {
      setHolding(false);
    }
  }

  async function confirm() {
    if (!hold) return;
    setSubmitting(true);
    setConfirmError(undefined);

    try {
      const result = await bookAction({
        petId: details.petId,
        hold: hold.value,
        type: details.type,
        reason: details.reason.trim() || undefined,
        idempotencyKey: hold.key,
      });

      if (result.ok) {
        setBookedAt(result.data.starts_at);
        setStep(DONE);
      } else if (result.error.code === 'hold_expired') {
        setExpired(true);
      } else if (result.error.code === 'slot_unavailable') {
        // Taken after all: back to the times, with the alternatives the backend offered.
        setRefusal({ message: result.error.message, suggested: suggestedTimes(result.error.details) });
        setStep(TIME);
      } else {
        setConfirmError(result.error.message);
      }
    } catch {
      setConfirmError(bookingPage.unreachable);
    } finally {
      setSubmitting(false);
    }
  }

  if (step === DONE && bookedAt) {
    return <BookingDone startsAt={bookedAt} />;
  }

  const pet = options.pets.find((item) => item.id === details.petId);
  const service = options.servicesByBranch[details.branchId]?.find((item) => item.id === details.serviceId);
  const branch = options.branches.length > 1 ? options.branches.find((item) => item.id === details.branchId) : undefined;

  return (
    <div className="space-y-6">
      <BookingSteps current={step} />

      {step === DETAILS && (
        <DetailsStep
          options={options}
          value={details}
          onChange={setDetails}
          onNext={() => {
            setRefusal(undefined);
            setStep(TIME);
          }}
        />
      )}

      {step === TIME && (
        <TimeStep
          serviceId={details.serviceId}
          branchId={details.branchId}
          vetNames={options.vets}
          onPick={pick}
          holding={holding}
          refusal={refusal}
          onBack={() => setStep(DETAILS)}
        />
      )}

      {step === CONFIRM && hold && (
        <ConfirmStep
          hold={hold.value}
          summary={{
            pet: pet?.name,
            service: service?.name,
            type: bookingPage.details.types[details.type].label,
            vet: options.vets[hold.value.staff_id] ?? bookingPage.vetFallback,
            branch: branch?.name,
            reason: details.reason.trim() || undefined,
          }}
          expired={expired}
          onExpire={onExpire}
          submitting={submitting}
          error={confirmError}
          onConfirm={confirm}
          onBack={() => {
            setRefusal(undefined);
            setStep(TIME);
          }}
        />
      )}
    </div>
  );
}
