'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { saveConsentsAction } from '../../actions/privacy-actions';
import { portalAccount } from '../../content/portal-account';
import type { ConsentPurpose, ConsentState } from '../../types/privacy.types';
import { FormFooter, type FormStatus } from './form-footer';
import { ToggleRow } from './toggle-row';

const ORDER: ConsentPurpose[] = ['service_messages', 'marketing', 'medical_record_sharing', 'extended_data_processing'];

/**
 * One switch per purpose. Only the switches the owner actually moved are sent, so a default they
 * never touched stays a default and is not turned into a recorded decision.
 */
export function ConsentsForm({ consents }: { consents: ConsentState[] }) {
  const initial = Object.fromEntries(consents.map((item) => [item.purpose, item.granted])) as Record<ConsentPurpose, boolean>;
  const [values, setValues] = useState(initial);
  const [baseline, setBaseline] = useState(initial);
  const [status, setStatus] = useState<FormStatus>({ kind: 'idle' });
  const [pending, startTransition] = useTransition();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus({ kind: 'idle' });

    const changed = Object.fromEntries(ORDER.filter((purpose) => values[purpose] !== baseline[purpose]).map((purpose) => [purpose, values[purpose]]));

    if (Object.keys(changed).length === 0) {
      setStatus({ kind: 'saved' });
      return;
    }

    startTransition(async () => {
      try {
        const result = await saveConsentsAction({ consents: changed });

        if (result.ok) {
          setBaseline(values);
          setStatus({ kind: 'saved' });
        } else {
          setStatus({ kind: 'error', message: result.error.message });
        }
      } catch {
        setStatus({ kind: 'error', message: portalAccount.unreachable });
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-2">
      <div className="divide-y divide-ink-100">
        {ORDER.filter((purpose) => purpose in values).map((purpose) => (
          <ToggleRow
            key={purpose}
            id={`consent-${purpose}`}
            label={portalAccount.consents.purposes[purpose].label}
            hint={portalAccount.consents.purposes[purpose].hint}
            checked={values[purpose]}
            onChange={(checked) => setValues({ ...values, [purpose]: checked })}
          />
        ))}
      </div>
      <FormFooter pending={pending} status={status} />
    </form>
  );
}
