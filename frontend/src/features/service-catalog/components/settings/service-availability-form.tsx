'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateServiceAvailabilityAction } from '../../actions/service-actions';
import { serviceSettings } from '../../content/service-settings';
import type { BookingWindow, ManagedService } from '../../types/managed-service.types';
import { BookingWindowRow } from './booking-window-row';

interface ServiceAvailabilityFormProps {
  service: ManagedService;
  /** Branch id to name. */
  branches: Record<string, string>;
  onDone: () => void;
}

interface WindowRow extends BookingWindow {
  key: number;
}

let nextKey = 0;
const row = (window: BookingWindow): WindowRow => ({ ...window, key: (nextKey += 1) });

/** Online booking on or off, the branches that offer it, and when customers may book it themselves. */
export function ServiceAvailabilityForm({ service, branches, onDone }: ServiceAvailabilityFormProps) {
  const text = serviceSettings.availability;
  const [online, setOnline] = useState(service.online_booking_enabled);
  const [branchIds, setBranchIds] = useState(service.branch_ids);
  const [windows, setWindows] = useState(() => service.booking_windows.map(row));
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () =>
        updateServiceAvailabilityAction({
          id: service.id,
          version: service.version,
          values: {
            online_booking_enabled: online,
            branch_ids: branchIds,
            booking_windows: windows.map(({ day, from, to }) => ({ day, from, to })),
          },
        }),
      onDone,
    );
  }

  const windowError = (index: number) =>
    Object.entries(errors).find(([field]) => field.startsWith(`booking_windows[${index}]`))?.[1];

  return (
    <form onSubmit={save} className="space-y-5 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
      <Checkbox label={text.online} hint={text.onlineHint} checked={online} onChange={(event) => setOnline(event.target.checked)} />

      {Object.keys(branches).length > 1 && (
        <CheckboxGroup
          legend={text.branches}
          hint={text.branchesHint}
          error={errors.branch_ids}
          options={branches}
          selected={branchIds}
          onChange={setBranchIds}
        />
      )}

      <fieldset className="space-y-2">
        <legend className="text-sm font-medium text-ink-700">{text.windows}</legend>
        <p className="text-sm text-ink-500">{text.windowsHint}</p>
        <ul className="space-y-2">
          {windows.map((window, index) => (
            <BookingWindowRow
              key={window.key}
              window={window}
              error={windowError(index)}
              onChange={(next) => setWindows((current) => current.map((entry) => (entry.key === window.key ? { ...next, key: window.key } : entry)))}
              onRemove={() => setWindows((current) => current.filter((entry) => entry.key !== window.key))}
            />
          ))}
        </ul>
        <Button variant="secondary" size="sm" onClick={() => setWindows((current) => [...current, row({ day: 'monday', from: '09:00', to: '12:00' })])}>
          {text.addWindow}
        </Button>
      </fieldset>

      <FormFooter pending={pending} message={message} onCancel={onDone} />
    </form>
  );
}
