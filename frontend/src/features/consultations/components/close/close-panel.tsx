'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { clinicInstant } from '@/features/booking/lib/booking-dates';
import { closeConsultationAction } from '../../actions/consultation-actions';
import { consultationPage } from '../../content/consultation-page';
import { usePartSubmit } from '../../hooks/use-part-submit';
import { CLOSED_PARAMS } from '../../lib/closed-params';

const NOTE_FIELDS = ['chief_complaint', 'subjective', 'objective', 'assessment', 'plan'];

/** Where on the page a missing field is filled in. */
const anchorFor = (field: string) => `#${NOTE_FIELDS.includes(field) ? 'notes' : field}`;

interface Closed {
  invoice?: { id: string; number: string };
  followUpAt?: string;
}

interface ClosePanelProps {
  consultationId: string;
  /** The services a follow-up can be booked for. */
  services: { id: string; name: string }[];
  /**
   * The visit was booked, so a follow-up can repeat its service. A walk-in was not, and its
   * follow-up has to name one: the backend refuses it otherwise.
   */
  fromAppointment: boolean;
}

/**
 * Signs and closes. Asks once more before signing, because a signed record cannot be edited, only
 * amended. If the clinic requires something still empty, the refusal lists it with links to the
 * sections; a follow-up visit can be booked in the same step.
 */
export function ClosePanel({ consultationId, services, fromAppointment }: ClosePanelProps) {
  const text = consultationPage.close;
  const [followUp, setFollowUp] = useState({ date: '', time: '', serviceId: fromAppointment ? '' : (services[0]?.id ?? '') });
  const [confirming, setConfirming] = useState(false);
  const [missing, setMissing] = useState<string[]>([]);
  const [key, setKey] = useState(() => crypto.randomUUID());
  const { errors, message, pending, submit } = usePartSubmit();
  const router = useRouter();

  // Any refusal that names a field (a follow-up time already taken, a service the follow-up needs)
  // is shown here as one line: the close is a single decision, not a form of many fields.
  const refusal = message ?? Object.values(errors)[0];

  // The page re-renders as a closed case, without this panel, so what the close produced travels
  // in the address for the page to show.
  function closedWith(result: Closed) {
    const params = new URLSearchParams();
    if (result.invoice) {
      params.set(CLOSED_PARAMS.invoiceId, result.invoice.id);
      params.set(CLOSED_PARAMS.invoiceNumber, result.invoice.number);
    }
    if (result.followUpAt) params.set(CLOSED_PARAMS.followUpAt, result.followUpAt);
    params.set(CLOSED_PARAMS.closed, '1');
    router.replace(`?${params}`);
  }

  function close() {
    setMissing([]);
    const wantsFollowUp = Boolean(followUp.date && followUp.time);

    submit(
      () =>
        closeConsultationAction({
          consultationId,
          followUpAt: wantsFollowUp ? clinicInstant(followUp.date, followUp.time) : undefined,
          followUpServiceId: wantsFollowUp && followUp.serviceId ? followUp.serviceId : undefined,
          idempotencyKey: key,
        }),
      (data) => closedWith(data),
      (refused) => {
        setConfirming(false);
        setKey(crypto.randomUUID());
        const fields = refused.error.details?.missing_fields;
        if (refused.error.code !== 'incomplete_consultation' || !Array.isArray(fields)) return false;
        setMissing(fields.filter((field): field is string => typeof field === 'string'));
        return true;
      },
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-ink-600">{text.hint}</p>

      {missing.length > 0 && (
        <div role="alert" className="rounded-lg border border-amber-300 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
          <span className="font-semibold">{text.missing}</span>{' '}
          {missing.map((field, index) => (
            <span key={field}>
              {index > 0 && ', '}
              <a href={anchorFor(field)} className="underline">
                {text.fieldNames[field] ?? field}
              </a>
            </span>
          ))}
        </div>
      )}
      {refusal && <p role="alert" className="text-sm text-red-700">{refusal}</p>}

      <fieldset className="space-y-2">
        <legend className="text-sm font-medium text-ink-700">{text.followUp}</legend>
        <div className="flex flex-wrap gap-3">
          <Field label={text.followUpDate} htmlFor="follow-up-date">
            <Input id="follow-up-date" type="date" value={followUp.date} className="max-w-44" onChange={(event) => setFollowUp((current) => ({ ...current, date: event.target.value }))} />
          </Field>
          <Field label={text.followUpTime} htmlFor="follow-up-time">
            <Input id="follow-up-time" type="time" step={300} value={followUp.time} className="max-w-36" onChange={(event) => setFollowUp((current) => ({ ...current, time: event.target.value }))} />
          </Field>
        </div>
        <Field label={text.followUpService} htmlFor="follow-up-service">
          <Select id="follow-up-service" value={followUp.serviceId} onChange={(event) => setFollowUp((current) => ({ ...current, serviceId: event.target.value }))}>
            {fromAppointment && <option value="">{text.sameService}</option>}
            {services.map((service) => (
              <option key={service.id} value={service.id}>
                {service.name}
              </option>
            ))}
          </Select>
        </Field>
      </fieldset>

      {confirming ? (
        <div className="flex flex-wrap items-center justify-end gap-3">
          <p className="text-sm font-medium text-ink-800">{text.confirm}</p>
          <Button variant="ghost" onClick={() => setConfirming(false)} disabled={pending}>
            {consultationPage.cancel}
          </Button>
          <Button onClick={close} disabled={pending}>
            {pending ? text.working : text.submit}
          </Button>
        </div>
      ) : (
        <div className="flex justify-end">
          <Button onClick={() => setConfirming(true)}>{text.submit}</Button>
        </div>
      )}
    </div>
  );
}
