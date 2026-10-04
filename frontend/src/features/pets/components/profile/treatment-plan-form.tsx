'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input, Select, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import type { VetOption } from '@/features/appointments/types/calendar.types';
import type { PublicService } from '@/features/service-catalog/types/service.types';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { clinicDay } from '@/lib/format/datetime';
import { createTreatmentPlanAction } from '../../actions/treatment-plan-actions';
import { petForms } from '../../content/pet-forms';
import { treatmentPlan } from '../../content/treatment-plan';
import type { TreatmentPlan } from '../../types/pet.types';
import { TreatmentPlanResult } from './treatment-plan-result';

export interface TreatmentPlanFormProps {
  petId: string;
  /** The branch in view; the sessions are booked there. */
  branchId?: string;
  services: PublicService[];
  vets: VetOption[];
}

const initial = () => ({
  title: '',
  firstDate: clinicDay(new Date()),
  firstTime: '10:00',
  sessions: '3',
  intervalDays: '7',
  vetId: '',
  notes: '',
  book: true,
});

/**
 * A course of visits: how many, how far apart, from when. Booking all of them is the default and
 * goes through as one request, all or nothing, so it carries an idempotency key that is renewed
 * only once the outcome is known.
 */
export function TreatmentPlanForm({ petId, branchId, services, vets }: TreatmentPlanFormProps) {
  const text = treatmentPlan;
  const [open, setOpen] = useState(false);
  const [values, setValues] = useState(initial);
  const [serviceId, setServiceId] = useState(services[0]?.id ?? '');
  const [created, setCreated] = useState<TreatmentPlan>();
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  const { errors, message, pending, submit, touch } = useFormSubmit(petForms);

  const set = <K extends keyof ReturnType<typeof initial>>(field: K, value: ReturnType<typeof initial>[K]) => {
    touch();
    setValues((current) => ({ ...current, [field]: value }));
  };

  function reset() {
    setValues(initial());
    setCreated(undefined);
    setOpen(false);
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    submit(
      async () => {
        const result = await createTreatmentPlanAction({
          petId,
          serviceId,
          // The clinic is in Bangkok, which keeps one offset all year.
          firstStartsAt: `${values.firstDate}T${values.firstTime}:00+07:00`,
          title: values.title,
          sessions: Number(values.sessions),
          intervalDays: Number(values.intervalDays),
          staffId: values.vetId || undefined,
          branchId,
          notes: values.notes,
          book: values.book && Boolean(branchId),
          idempotencyKey,
        });

        // Booked or refused, the next request is a new one with a key of its own.
        setIdempotencyKey(crypto.randomUUID());
        return result;
      },
      (plan) => setCreated(plan),
    );
  }

  if (created) {
    return <TreatmentPlanResult plan={created} onAnother={reset} />;
  }

  if (!open) {
    return (
      <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
        {text.open}
      </Button>
    );
  }

  return (
    <form onSubmit={save} className="space-y-3 rounded-lg border border-ink-200 bg-ink-50/60 p-4">
      <Field label={text.planTitle} htmlFor="plan-title" error={errors.title}>
        <Input
          id="plan-title"
          value={values.title}
          placeholder={text.planTitlePlaceholder}
          maxLength={200}
          required
          autoFocus
          aria-invalid={Boolean(errors.title)}
          onChange={(event) => set('title', event.target.value)}
        />
      </Field>

      <Field label={text.service} htmlFor="plan-service" hint={text.servicesHint} error={errors.service_id}>
        <Select
          id="plan-service"
          value={serviceId}
          required
          aria-invalid={Boolean(errors.service_id)}
          onChange={(event) => { touch(); setServiceId(event.target.value); }}
        >
          {services.length === 0 && <option value="">{text.noServices}</option>}
          {services.map((service) => (
            <option key={service.id} value={service.id}>
              {service.name}
            </option>
          ))}
        </Select>
      </Field>

      <div className="grid gap-3 sm:grid-cols-2">
        <Field label={text.firstDate} htmlFor="plan-date" error={errors.first_starts_at}>
          <Input
            id="plan-date"
            type="date"
            value={values.firstDate}
            required
            aria-invalid={Boolean(errors.first_starts_at)}
            onChange={(event) => set('firstDate', event.target.value)}
          />
        </Field>
        <Field label={text.firstTime} htmlFor="plan-time">
          <Input id="plan-time" type="time" step={300} value={values.firstTime} required onChange={(event) => set('firstTime', event.target.value)} />
        </Field>
        <Field label={text.sessions} htmlFor="plan-sessions" hint={text.sessionsHint} error={errors.sessions ?? errors.rrule}>
          <Input
            id="plan-sessions"
            type="number"
            min={2}
            max={52}
            value={values.sessions}
            required
            aria-invalid={Boolean(errors.sessions ?? errors.rrule)}
            onChange={(event) => set('sessions', event.target.value)}
          />
        </Field>
        <Field label={text.intervalDays} htmlFor="plan-interval" error={errors.interval_days}>
          <Input
            id="plan-interval"
            type="number"
            min={1}
            max={365}
            value={values.intervalDays}
            required
            aria-invalid={Boolean(errors.interval_days)}
            onChange={(event) => set('intervalDays', event.target.value)}
          />
        </Field>
      </div>

      <Field label={text.vet} htmlFor="plan-vet" error={errors.staff_id}>
        <Select id="plan-vet" value={values.vetId} onChange={(event) => set('vetId', event.target.value)}>
          <option value="">{text.anyVet}</option>
          {vets.map((vet) => (
            <option key={vet.id} value={vet.id}>
              {vet.name}
            </option>
          ))}
        </Select>
      </Field>

      <Field label={text.notes} htmlFor="plan-notes" error={errors.notes}>
        <Textarea id="plan-notes" value={values.notes} maxLength={2000} rows={2} onChange={(event) => set('notes', event.target.value)} />
      </Field>

      <Checkbox
        label={text.book}
        hint={branchId ? text.bookHint : text.noBranch}
        checked={values.book && Boolean(branchId)}
        disabled={!branchId}
        onChange={(event) => set('book', event.target.checked)}
      />

      <div className="flex flex-wrap items-center justify-end gap-2">
        <FormStatus message={message} savedText="" />
        <Button variant="ghost" size="sm" onClick={reset} disabled={pending}>
          {petForms.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={pending || services.length === 0}>
          {pending ? text.submitting : text.submit}
        </Button>
      </div>
    </form>
  );
}
