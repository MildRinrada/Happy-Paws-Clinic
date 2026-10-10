'use client';

import { useCallback, useState } from 'react';
import { clinicDay } from '@/lib/format/datetime';
import { bookAction, holdSlotAction } from '../actions/booking-actions';
import { bookingPage } from '../content/booking-page';
import { suggestedTimes } from '../lib/slot-times';
import type { BookingDetails, BookingOptions, SlotHold, TimePreselect } from '../types/booking.types';
import { BookingDone } from './booking-done';
import { BookingSteps } from './booking-steps';
import { ConfirmStep } from './confirm-step';
import { DetailsStep } from './details-step';
import { PresetTimeNote } from './preset-time-note';
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
 *
 * `start` is a time the address asked for (`?date=&time=`), checked on the server by the same
 * `preselectTime` as `/book`. The form still opens on the details, because the owner has to say which
 * pet; leaving them with the service and branch unchanged then holds a free time at once (on to
 * confirming), or opens that day's times, with a note when the time was taken. Changing the service
 * or branch drops it.
 */
export function BookingWizard({
  options,
  initial,
  start,
}: {
  options: BookingOptions;
  initial: BookingDetails;
  start?: TimePreselect;
}) {
  const [step, setStep] = useState(DETAILS);
  const [details, setDetails] = useState(initial);
  // Used once, on the first "next" from the details.
  const [preset, setPreset] = useState(start);

  const [holding, setHolding] = useState(false);
  // The clinic day of the last time picked, so coming back to the times (a refusal on confirm, or
  // "back") reopens that day rather than today, and the suggested times land on the right day.
  const [day, setDay] = useState<string>();
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
    setDay(clinicDay(startsAt));

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

  // Only for what it was checked against: the service and branch the page opened on.
  const kept = preset && details.serviceId === initial.serviceId && details.branchId === initial.branchId ? preset : undefined;

  function leaveDetails() {
    setRefusal(undefined);
    setPreset(undefined);

    if (kept) {
      setDay(kept.day);
      if (kept.notice) setRefusal({ message: kept.notice, suggested: [] });
    }

    setStep(TIME);

    if (kept?.startsAt) void pick(kept.startsAt);
  }

  const pet = options.pets.find((item) => item.id === details.petId);
  const service = options.servicesByBranch[details.branchId]?.find((item) => item.id === details.serviceId);
  const branch = options.branches.length > 1 ? options.branches.find((item) => item.id === details.branchId) : undefined;

  return (
    <div className="space-y-6">
      <BookingSteps current={step} />

      {step === DETAILS && kept?.startsAt && <PresetTimeNote startsAt={kept.startsAt} />}

      {step === DETAILS && (
        <DetailsStep
          options={options}
          value={details}
          onChange={setDetails}
          onNext={leaveDetails}
          nextLabel={kept?.startsAt ? bookingPage.preset.next : undefined}
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
          initialDay={day}
        />
      )}

      {step === CONFIRM && hold && (
        <ConfirmStep
          hold={hold.value}
          summary={{
            pet: pet?.name,
            service: service?.name,
            type: options.types.find((type) => type.code === details.type)?.name ?? details.type,
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
