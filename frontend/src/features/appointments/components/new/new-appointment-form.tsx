'use client';

import { useRouter } from 'next/navigation';
import { useEffect, useState, useTransition, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { loadSlotsAction } from '@/features/booking/actions/booking-actions';
import { clinicInstant } from '@/features/booking/lib/booking-dates';
import { slotTimes, suggestedTimes, type SlotTime } from '@/features/booking/lib/slot-times';
import type { Slot } from '@/features/booking/types/booking.types';
import type { PetSummary } from '@/features/pets/types/pet.types';
import type { PublicService } from '@/features/service-catalog/types/service.types';
import { createRecurringSeriesAction } from '../../actions/advanced-booking-actions';
import { createAppointmentAction } from '../../actions/calendar-actions';
import { repeatText } from '../../content/advanced-booking';
import { newAppointment } from '../../content/appointment-calendar';
import { useConflictCheck, type ConflictQuery } from '../../hooks/use-conflict-check';
import { appointmentPaths } from '../../lib/appointment-paths';
import { defaultRepeat, expand, toRrule, type RepeatRule } from '../../lib/recurrence';
import { refusalText } from '../../lib/refusal';
import { seriesConflicts } from '../../lib/series-conflicts';
import type { SeriesConflict } from '../../types/advanced-booking.types';
import type { VetOption } from '../../types/calendar.types';
import { PetPicker } from './pet-picker';
import { RepeatSection } from './repeat-section';
import { SingleConflictCheck } from './single-conflict-check';
import { SlotPicker } from './slot-picker';

interface NewAppointmentFormProps {
  branchId: string;
  services: PublicService[];
  vets: VetOption[];
  initialDay: string;
  /** `appointment:read`, which the conflict check needs. */
  canCheck: boolean;
}

/**
 * Books on a customer's behalf, once or as a course. The free times come from the same availability
 * the booking page reads; the counter may also type a time, because staff are not held to the
 * online notice period. Either way the backend checks the vet is really free and answers with other
 * times if not. A course is booked whole or not at all, and a refusal is shown against each date.
 */
export function NewAppointmentForm({ branchId, services, vets, initialDay, canCheck }: NewAppointmentFormProps) {
  const text = newAppointment;
  const router = useRouter();

  const [pet, setPet] = useState<PetSummary | null>(null);
  const [serviceId, setServiceId] = useState(services[0]?.id ?? '');
  const [day, setDay] = useState(initialDay);
  const [vetId, setVetId] = useState('');
  const [slots, setSlots] = useState<Slot[] | null>(null);
  const [chosen, setChosen] = useState<string>();
  const [manualTime, setManualTime] = useState('');
  const [reason, setReason] = useState('');
  const [repeat, setRepeat] = useState<RepeatRule | null>(null);
  const [error, setError] = useState<string>();
  const [suggested, setSuggested] = useState<string[]>([]);
  const [refused, setRefused] = useState<{ key: string; dates: Map<number, SeriesConflict> }>();
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  const [loadingSlots, startLoading] = useTransition();
  const [pending, startSubmit] = useTransition();

  useEffect(() => {
    setChosen(undefined);
    if (!serviceId || !day) {
      setSlots(null);
      return;
    }

    startLoading(async () => {
      const result = await loadSlotsAction({ serviceId, branchId, date: day });
      setSlots(result.ok ? result.data.slots : []);
    });
  }, [serviceId, day, branchId]);

  const times: SlotTime[] | null = slots ? slotTimes(slots, vetId || undefined) : null;
  const chosenSlot = times?.find((slot) => slot.startsAt === chosen);
  const startsAt = manualTime ? clinicInstant(day, manualTime) : chosen;

  // A typed time keeps the vet the counter picked. A chosen slot names who is free at it.
  const staffId = vetId || (manualTime ? undefined : chosenSlot?.staffIds[0]);

  const expansion = repeat && startsAt ? expand(startsAt, repeat) : null;
  const starts = repeat ? (expansion && !expansion.problem ? expansion.starts : []) : startsAt ? [startsAt] : [];
  const query: ConflictQuery | null =
    serviceId && starts.length > 0 ? { starts, serviceId, branchId, vetId: staffId, petId: pet?.id } : null;
  const check = useConflictCheck(query);
  const queryKey = JSON.stringify(query);
  const refusals = refused?.key === queryKey ? refused.dates : new Map<number, SeriesConflict>();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!pet || !serviceId || !startsAt) {
      setError(text.missing);
      return;
    }
    if (expansion?.problem) {
      setError(repeatText[expansion.problem]);
      return;
    }

    setError(undefined);
    setSuggested([]);
    setRefused(undefined);

    const common = { petId: pet.id, serviceId, branchId, staffId, reason: reason.trim() || undefined, idempotencyKey };

    startSubmit(async () => {
      try {
        const result = repeat
          ? await createRecurringSeriesAction({ ...common, firstStartsAt: startsAt, rrule: toRrule(repeat) })
          : await createAppointmentAction({ ...common, startsAt });

        if (result.ok) {
          router.push(appointmentPaths.calendar(day));
          return;
        }

        // Nothing was booked, so the next try is a new request with a key of its own.
        setIdempotencyKey(crypto.randomUUID());
        setError(refusalText(result.error));
        setSuggested(repeat ? [] : suggestedTimes(result.error.details));
        if (repeat) setRefused({ key: queryKey, dates: seriesConflicts(result.error.details) });
      } catch {
        setError(text.unreachable);
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-5">
      {error && <Alert>{error}</Alert>}

      <PetPicker pet={pet} onPick={setPet} />

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.service.label} htmlFor="book-service" hint={text.servicesNote}>
          <Select id="book-service" value={serviceId} onChange={(event) => setServiceId(event.target.value)}>
            {services.length === 0 && <option value="">{text.service.none}</option>}
            {services.map((service) => (
              <option key={service.id} value={service.id}>
                {service.name} ({text.service.minutes(service.duration_minutes)})
              </option>
            ))}
          </Select>
        </Field>

        <Field label={text.date} htmlFor="book-day">
          <Input id="book-day" type="date" value={day} required onChange={(event) => setDay(event.target.value)} />
        </Field>
      </div>

      <Field label={text.vet.label} htmlFor="book-vet">
        <Select id="book-vet" value={vetId} onChange={(event) => { setVetId(event.target.value); setChosen(undefined); }}>
          <option value="">{text.vet.any}</option>
          {vets.map((vet) => (
            <option key={vet.id} value={vet.id}>
              {vet.name}
            </option>
          ))}
        </Select>
      </Field>

      <fieldset className="space-y-3">
        <legend className="text-sm font-medium text-ink-700">{text.slots.label}</legend>
        <SlotPicker
          times={times}
          loading={loadingSlots}
          chosen={manualTime ? undefined : chosen}
          onChoose={(value) => {
            setChosen(value);
            setManualTime('');
          }}
        />

        <Field label={text.slots.manual} htmlFor="book-time" hint={text.slots.manualHint}>
          <Input id="book-time" type="time" step={300} value={manualTime} className="max-w-40" onChange={(event) => setManualTime(event.target.value)} />
        </Field>

        {suggested.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {suggested.map((option) => (
              <Button key={option} variant="secondary" size="sm" onClick={() => setManualTime(option)}>
                {option}
              </Button>
            ))}
          </div>
        )}
      </fieldset>

      <Field label={text.reason.label} htmlFor="book-reason">
        <Input id="book-reason" value={reason} placeholder={text.reason.placeholder} maxLength={1000} onChange={(event) => setReason(event.target.value)} />
      </Field>

      <RepeatSection
        rule={repeat}
        firstDay={day}
        expansion={expansion}
        refusals={refusals}
        hasVet={Boolean(staffId)}
        canCheck={canCheck}
        check={check}
        onToggle={(on) => setRepeat(on ? defaultRepeat() : null)}
        onChange={setRepeat}
      />

      {!repeat && canCheck && startsAt && <SingleConflictCheck startsAt={startsAt} hasVet={Boolean(staffId)} check={check} />}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? text.submitting : expansion && !expansion.problem ? repeatText.submit(expansion.starts.length) : text.submit}
        </Button>
      </div>
    </form>
  );
}
