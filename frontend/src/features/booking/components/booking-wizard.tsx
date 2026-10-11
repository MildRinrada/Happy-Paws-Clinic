'use client';

import { useCallback, useState } from 'react';
import { clinicDay } from '@/lib/format/datetime';
import { bookAction, holdSlotAction } from '../actions/booking-actions';
import { bookingFrame } from '../content/booking-frame';
import { bookingPage } from '../content/booking-page';
import { forgetWeekSlots } from '../hooks/use-week-slots';
import { detailsReady } from '../lib/details-ready';
import { suggestedTimes } from '../lib/slot-times';
import type { BookingDetails, BookingOptions, SlotHold, TimeChoice, TimePreselect } from '../types/booking.types';
import { BookingDone } from './booking-done';
import { ConfirmStep } from './confirm-step';
import { DetailsStep } from './details-step';
import { BookingFrame } from './frame/booking-frame';
import { BookingProgress } from './frame/booking-progress';
import { StepNextButton } from './frame/step-next-button';
import { PresetTimeNote } from './preset-time-note';
import { BookingSummary } from './summary/booking-summary';
import { HoldNoteCard } from './summary/hold-note-card';
import { QuestionsCard } from './summary/questions-card';
import { BookingTimeStep } from './time/booking-time-step';
import type { PickRefusal } from './time-step';

const DETAILS = 0;
const TIME = 1;
const CONFIRM = 2;
const DONE = 3;

/** The step names carried as `data-booking-step`, as on `/book`. */
const STEP_NAMES = ['details', 'time', 'confirm', 'done'];

/**
 * The booking form from start to finish: details, a time, then confirming. Drawn in the same frame
 * as `/book`: the progress bar, the step's card, and the summary card beside it.
 *
 * Picking a time only marks it; "next" holds it (`POST /booking/holds`), so nobody else can take
 * it while the owner reads the summary. The idempotency key belongs to that hold: a double tap on
 * "ยืนยัน" books once, and choosing a new time starts a new key.
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
  vet,
}: {
  options: BookingOptions;
  initial: BookingDetails;
  start?: TimePreselect;
  /**
   * A vet the address asked for (`?vet=`, from the team page through `/book`), already checked on
   * the server. The times open on that vet's, and a time from the address is held with them.
   */
  vet?: string;
}) {
  const [step, setStep] = useState(DETAILS);
  const [details, setDetails] = useState(initial);
  // Used once, on the first "next" from the details.
  const [preset, setPreset] = useState(start);

  const [holding, setHolding] = useState(false);
  // The day, vet and time picked, kept here so the summary card shows them as they are chosen and
  // coming back to the times (a refusal on confirm, or "back") reopens the same day.
  const [time, setTime] = useState<TimeChoice>({ day: '', vet: vet ?? '' });
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
        forgetWeekSlots();
        setTime((current) => ({ ...current, day: clinicDay(startsAt), startsAt: undefined }));
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
        // Taken after all: back to the times, read afresh, with the alternatives the backend offered.
        forgetWeekSlots();
        setTime((current) => ({ ...current, startsAt: undefined }));
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
    return (
      <div className="mx-auto max-w-2xl rounded-[18px] bg-white p-6 sm:p-8" data-booking-flow="portal" data-booking-step="done">
        <BookingDone startsAt={bookedAt} />
      </div>
    );
  }

  // Only for what it was checked against: the service and branch the page opened on.
  const kept = preset && details.serviceId === initial.serviceId && details.branchId === initial.branchId ? preset : undefined;

  /** A change of service or branch drops the time picked: it was free for the old one. */
  function changeDetails(next: BookingDetails) {
    if (next.serviceId !== details.serviceId || next.branchId !== details.branchId) {
      setTime((current) => ({ ...current, startsAt: undefined }));
    }
    setDetails(next);
  }

  function leaveDetails() {
    setRefusal(undefined);
    setPreset(undefined);

    if (kept) {
      setTime((current) => ({ ...current, day: kept.day, startsAt: kept.startsAt }));
      if (kept.notice) setRefusal({ message: kept.notice, suggested: [] });
    }

    setStep(TIME);

    if (kept?.startsAt) void pick(kept.startsAt, vet);
  }

  function leaveTime() {
    if (time.startsAt) void pick(time.startsAt, time.vet || undefined);
  }

  const pet = options.pets.find((item) => item.id === details.petId);
  const service = options.servicesByBranch[details.branchId]?.find((item) => item.id === details.serviceId);
  const branch = options.branches.find((item) => item.id === details.branchId);
  const typeName = options.types.find((type) => type.code === details.type)?.name ?? details.type;
  const held = step === CONFIRM ? hold?.value : undefined;
  const vetId = held?.staff_id ?? (time.vet || undefined);
  const vetName = vetId ? (options.vets[vetId] ?? bookingPage.vetFallback) : undefined;
  const detailsNext = kept?.startsAt ? bookingPage.preset.next : bookingFrame.next.time;
  const timeNext = holding ? bookingPage.time.holding : bookingFrame.next.confirm;

  const action =
    step === DETAILS ? (
      <StepNextButton onClick={leaveDetails} disabled={!detailsReady(options, details, true, true)} arrow={false} block>
        {detailsNext}
      </StepNextButton>
    ) : step === TIME ? (
      <StepNextButton onClick={leaveTime} disabled={!time.startsAt || holding} arrow={false} block>
        {timeNext}
      </StepNextButton>
    ) : undefined;

  return (
    <BookingFrame
      flow="portal"
      step={STEP_NAMES[step]!}
      progress={<BookingProgress labels={bookingPage.steps} current={step} onEdit={holding || submitting ? undefined : setStep} />}
      note={step === DETAILS && kept?.startsAt ? <PresetTimeNote startsAt={kept.startsAt} /> : undefined}
      aside={
        <>
          <BookingSummary
            askPet
            values={{
              pet: pet?.name,
              service: service?.name,
              type: typeName,
              // The time the owner arrived with shows from the start, as the note above the details says.
              startsAt: held?.starts_at ?? time.startsAt ?? (step === DETAILS ? kept?.startsAt : undefined),
              day: time.day || undefined,
              vet: vetName,
              branch: branch?.name,
              reason: details.reason.trim() || undefined,
            }}
            onChangeService={step !== DETAILS && !holding && !submitting ? () => setStep(DETAILS) : undefined}
            action={action}
          />
          {step !== CONFIRM && <HoldNoteCard>{bookingFrame.holdNote.portal}</HoldNoteCard>}
          <QuestionsCard />
        </>
      }
    >
      {step === DETAILS && (
        <DetailsStep options={options} value={details} onChange={changeDetails} onNext={leaveDetails} nextLabel={detailsNext} />
      )}

      {step === TIME && (
        <BookingTimeStep
          serviceId={details.serviceId}
          branchId={details.branchId}
          vetNames={options.vets}
          value={time}
          onChange={setTime}
          initialVet={vet}
          refusal={refusal}
          holding={holding}
          onBack={() => setStep(DETAILS)}
          onNext={leaveTime}
          nextLabel={timeNext}
        />
      )}

      {step === CONFIRM && hold && (
        <ConfirmStep
          hold={hold.value}
          summary={{
            pet: pet?.name,
            service: service?.name,
            type: typeName,
            vet: options.vets[hold.value.staff_id] ?? bookingPage.vetFallback,
            branch: options.branches.length > 1 ? branch?.name : undefined,
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
    </BookingFrame>
  );
}
