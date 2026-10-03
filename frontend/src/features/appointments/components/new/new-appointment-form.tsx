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
import { createAppointmentAction } from '../../actions/calendar-actions';
import { newAppointment } from '../../content/appointment-calendar';
import { appointmentPaths } from '../../lib/appointment-paths';
import { refusalText } from '../../lib/refusal';
import type { VetOption } from '../../types/calendar.types';
import { PetPicker } from './pet-picker';
import { SlotPicker } from './slot-picker';

interface NewAppointmentFormProps {
  branchId: string;
  services: PublicService[];
  vets: VetOption[];
  initialDay: string;
}

/**
 * Books on a customer's behalf. The free times come from the same availability the booking page
 * reads; the counter may also type a time, because staff are not held to the online notice
 * period. Either way the backend checks the vet is really free and answers with other times if not.
 */
export function NewAppointmentForm({ branchId, services, vets, initialDay }: NewAppointmentFormProps) {
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
  const [error, setError] = useState<string>();
  const [suggested, setSuggested] = useState<string[]>([]);
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

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!pet || !serviceId || !startsAt) {
      setError(text.missing);
      return;
    }

    setError(undefined);
    setSuggested([]);

    startSubmit(async () => {
      try {
        const result = await createAppointmentAction({
          petId: pet.id,
          serviceId,
          branchId,
          startsAt,
          staffId,
          reason: reason.trim() || undefined,
          idempotencyKey,
        });

        if (result.ok) {
          router.push(appointmentPaths.calendar(day));
          return;
        }

        // Nothing was booked, so the next try is a new request with a key of its own.
        setIdempotencyKey(crypto.randomUUID());
        setError(refusalText(result.error));
        setSuggested(suggestedTimes(result.error.details));
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

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? text.submitting : text.submit}
        </Button>
      </div>
    </form>
  );
}
