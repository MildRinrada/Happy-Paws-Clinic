'use client';

import { useId, useState, useTransition, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input, Select } from '@/components/ui/field';
import { addDays } from '@/features/booking/lib/booking-dates';
import type { PetSummary } from '@/features/pets/types/pet.types';
import type { PublicService } from '@/features/service-catalog/types/service.types';
import { fieldErrors } from '@/lib/actions/field-errors';
import { addToWaitingListAction } from '../../actions/advanced-booking-actions';
import { waitingListText } from '../../content/advanced-booking';
import type { WaitingListEntry } from '../../types/advanced-booking.types';
import type { VetOption } from '../../types/calendar.types';
import { PetPicker } from '../new/pet-picker';
import { WaitingListAdded } from './waiting-list-added';

interface WaitingListFormProps {
  branchId: string;
  services: PublicService[];
  vets: VetOption[];
  /** Today in clinic time, where the window starts by default. */
  today: string;
}

/** The backend's default window when no dates are sent: today and the next 14 days. */
const DEFAULT_WINDOW_DAYS = 14;

/**
 * Puts a pet on the waiting list for a service and a run of days. Whether the window is allowed and
 * whether the pet is already waiting are the backend's to say; its sentence is shown as it comes.
 */
export function WaitingListForm({ branchId, services, vets, today }: WaitingListFormProps) {
  const text = waitingListText;
  const id = useId();
  const [pet, setPet] = useState<PetSummary | null>(null);
  const [serviceId, setServiceId] = useState(services[0]?.id ?? '');
  const [earliest, setEarliest] = useState(today);
  const [latest, setLatest] = useState(() => addDays(today, DEFAULT_WINDOW_DAYS));
  const [vetId, setVetId] = useState('');
  const [thisBranch, setThisBranch] = useState(true);
  const [note, setNote] = useState('');
  const [error, setError] = useState<string>();
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [added, setAdded] = useState<{ entry: WaitingListEntry; petName: string }>();
  const [pending, startSubmit] = useTransition();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!pet || !serviceId) {
      setError(text.missing);
      return;
    }

    setError(undefined);
    setErrors({});

    startSubmit(async () => {
      try {
        const result = await addToWaitingListAction({
          petId: pet.id,
          serviceId,
          earliestOn: earliest || undefined,
          latestOn: latest || undefined,
          branchId: thisBranch ? branchId : undefined,
          staffId: vetId || undefined,
          note: note.trim() || undefined,
        });

        if (result.ok) {
          setAdded({ entry: result.data, petName: pet.name });
          return;
        }

        const byField = fieldErrors(result.error);
        setErrors(byField);
        setError(Object.keys(byField).length === 0 ? result.error.message : undefined);
      } catch {
        setError(text.unreachable);
      }
    });
  }

  if (added) {
    return (
      <WaitingListAdded
        entry={added.entry}
        petName={added.petName}
        onAnother={() => {
          setAdded(undefined);
          setPet(null);
          setNote('');
        }}
      />
    );
  }

  return (
    <form onSubmit={submit} noValidate className="space-y-5">
      {error && <Alert>{error}</Alert>}

      <PetPicker pet={pet} onPick={setPet} />

      <Field label={text.service} htmlFor={`${id}-service`} error={errors.service_id}>
        <Select id={`${id}-service`} value={serviceId} onChange={(event) => setServiceId(event.target.value)}>
          {services.length === 0 && <option value="">{text.noService}</option>}
          {services.map((service) => (
            <option key={service.id} value={service.id}>
              {service.name}
            </option>
          ))}
        </Select>
      </Field>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.earliest} htmlFor={`${id}-earliest`} error={errors.earliest_on}>
          <Input id={`${id}-earliest`} type="date" value={earliest} onChange={(event) => setEarliest(event.target.value)} />
        </Field>
        <Field label={text.latest} htmlFor={`${id}-latest`} hint={text.windowHint} error={errors.latest_on}>
          <Input id={`${id}-latest`} type="date" value={latest} onChange={(event) => setLatest(event.target.value)} />
        </Field>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.vet} htmlFor={`${id}-vet`}>
          <Select id={`${id}-vet`} value={vetId} onChange={(event) => setVetId(event.target.value)}>
            <option value="">{text.anyVet}</option>
            {vets.map((vet) => (
              <option key={vet.id} value={vet.id}>
                {vet.name}
              </option>
            ))}
          </Select>
        </Field>
        <div className="sm:pt-7">
          <Checkbox label={text.thisBranch} hint={text.thisBranchHint} checked={thisBranch} onChange={(event) => setThisBranch(event.target.checked)} />
        </div>
      </div>

      <Field label={text.note} htmlFor={`${id}-note`} error={errors.note}>
        <Input id={`${id}-note`} value={note} maxLength={500} placeholder={text.notePlaceholder} onChange={(event) => setNote(event.target.value)} />
      </Field>

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? text.submitting : text.submit}
        </Button>
      </div>
    </form>
  );
}
