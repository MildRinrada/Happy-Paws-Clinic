'use client';

import { useEffect, useState, type Dispatch, type SetStateAction } from 'react';
import { Alert } from '@/components/ui/alert';
import { clinicDay } from '@/lib/format/datetime';
import { bookingFrame } from '../../content/booking-frame';
import { bookingPage } from '../../content/booking-page';
import { useWeekSlots } from '../../hooks/use-week-slots';
import { addDays, clinicInstant, daysBetween, WEEK_DAYS, weekStartFor } from '../../lib/booking-dates';
import { dayStatus } from '../../lib/day-status';
import { slotTimes, vetsIn } from '../../lib/slot-times';
import type { TimeChoice } from '../../types/booking.types';
import { StepActions } from '../frame/step-actions';
import { StepBackButton } from '../frame/step-back-button';
import { StepCard } from '../frame/step-card';
import { StepNextButton } from '../frame/step-next-button';
import { SuggestedTimes } from '../suggested-times';
import type { PickRefusal } from '../time-step';
import { DayChips } from './day-chips';
import { TimeGrid } from './time-grid';
import { VetSelect } from './vet-select';
import { WeekNav } from './week-nav';

/**
 * How many weeks past this one the chips page through. How far ahead the clinic books is the
 * backend's rule; days past it simply come back with no times, and say "เต็ม".
 */
const WEEKS_AHEAD = 8;

interface BookingTimeStepProps {
  serviceId: string;
  branchId: string;
  vetNames: Record<string, string>;
  /** Kept by the form, so the summary card can show the day and time as they are picked. */
  value: TimeChoice;
  onChange: Dispatch<SetStateAction<TimeChoice>>;
  /**
   * The vet the address asked for (`?vet=`, already checked on the server). They stay chosen from
   * day to day; a vet picked from a day's list goes back to "any" on another day.
   */
  initialVet?: string;
  refusal?: PickRefusal;
  /** The portal holds the time when "next" is pressed; nothing can be changed meanwhile. */
  holding: boolean;
  onBack: () => void;
  onNext: () => void;
  nextLabel: string;
}

/**
 * The day and time step of `/book` and `/portal/book`: seven day chips that say which days have a
 * free time, the chosen day's free times, and the vet. Picking a time only marks it; "next" moves
 * on with it. Seven days are read at once when the step opens (`useWeekSlots`), so moving between
 * the chips needs no further call.
 */
export function BookingTimeStep({
  serviceId,
  branchId,
  vetNames,
  value,
  onChange,
  initialVet,
  refusal,
  holding,
  onBack,
  onNext,
  nextLabel,
}: BookingTimeStepProps) {
  const text = bookingFrame.time;
  const [today] = useState(() => clinicDay(new Date()));
  const [weekStart, setWeekStart] = useState(() => weekStartFor(value.day || today, today));
  const week = useWeekSlots(serviceId, branchId, weekStart);
  const days = Array.from({ length: WEEK_DAYS }, (_, index) => addDays(weekStart, index));

  // No day chosen yet (the step opened plainly, or the visitor turned the week): once the week is
  // read, settle on its first day with a free time, or its first day when none has one.
  useEffect(() => {
    if (value.day || week.status !== 'ready') return;
    const row = Array.from({ length: WEEK_DAYS }, (_, index) => addDays(weekStart, index));
    const first = row.find((day) => dayStatus(week, day, value.vet) === 'free') ?? row[0]!;
    onChange((current) => (current.day ? current : { ...current, day: first }));
  }, [value.day, value.vet, week, weekStart, onChange]);

  const daySlots = week.status === 'ready' && value.day ? week.days[value.day] : undefined;
  const slots = daySlots?.ok ? daySlots.slots : [];
  const vets = vetsIn(slots);
  // The chosen vet stays in the list on a day they have no free time, so the list shows who it is.
  const vetChoices = value.vet && !vets.includes(value.vet) ? [value.vet, ...vets] : vets;
  const times = slotTimes(slots, value.vet || undefined);
  const loading = week.status === 'loading' || (week.status === 'ready' && !value.day);

  /** A vet picked from a day's list goes back to "any" on another day; the one from the link stays. */
  const keptVet = (vet: string) => (vet === initialVet ? vet : '');

  function selectDay(day: string) {
    onChange((current) => ({ day, vet: keptVet(current.vet), startsAt: undefined }));
  }

  function selectVet(vet: string) {
    const free = slotTimes(slots, vet || undefined);
    onChange((current) => ({
      ...current,
      vet,
      startsAt: free.some((time) => current.startsAt && Date.parse(time.startsAt) === Date.parse(current.startsAt)) ? current.startsAt : undefined,
    }));
  }

  function turnWeek(weeks: number) {
    setWeekStart(addDays(weekStart, weeks * WEEK_DAYS));
    onChange((current) => ({ day: '', vet: keptVet(current.vet), startsAt: undefined }));
  }

  return (
    <StepCard title={text.title}>
      <div className="space-y-7">
        <section className="space-y-3">
          <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
            <h3 className="text-[15px] font-semibold text-site-forest">{text.day}</h3>
            <WeekNav
              canGoBack={weekStart > today}
              canGoOn={daysBetween(today, weekStart) < WEEKS_AHEAD * WEEK_DAYS}
              onBack={() => turnWeek(-1)}
              onOn={() => turnWeek(1)}
            />
          </div>
          <DayChips
            days={days}
            today={today}
            value={value.day}
            statusOf={(day) => dayStatus(week, day, value.vet)}
            onSelect={selectDay}
          />
        </section>

        <section className="space-y-3">
          <h3 className="text-[15px] font-semibold text-site-forest">{text.slots}</h3>

          {refusal && (
            <div className="space-y-2">
              <Alert>{refusal.message}</Alert>
              {value.day && (
                <SuggestedTimes
                  times={refusal.suggested}
                  disabled={holding}
                  onPick={(time) => onChange((current) => ({ ...current, startsAt: clinicInstant(current.day, time) }))}
                />
              )}
            </div>
          )}

          {loading && <p className="text-sm text-site-muted">{bookingPage.time.loading}</p>}
          {week.status === 'error' && <Alert>{week.message}</Alert>}
          {daySlots && !daySlots.ok && <Alert>{daySlots.message}</Alert>}
          {daySlots?.ok && times.length === 0 && (
            <p className="text-sm text-site-muted">{value.vet && slots.length > 0 ? bookingPage.time.noneForVet : bookingPage.time.none}</p>
          )}
          {times.length > 0 && (
            <TimeGrid
              times={times}
              value={value.startsAt}
              disabled={holding}
              onSelect={(time) => onChange((current) => ({ ...current, startsAt: time.startsAt }))}
            />
          )}
        </section>

        <VetSelect vetIds={vetChoices} names={vetNames} value={value.vet} onChange={selectVet} />
      </div>

      <StepActions
        back={
          <StepBackButton onClick={onBack} disabled={holding}>
            {text.back}
          </StepBackButton>
        }
        next={
          <StepNextButton onClick={onNext} disabled={!value.startsAt || holding}>
            {nextLabel}
          </StepNextButton>
        }
      />
    </StepCard>
  );
}
