'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { appointmentDetail } from '../../content/appointment-calendar';

interface CancelFormProps {
  pending: boolean;
  onCancel: (reason: string) => void;
  onBack: () => void;
}

/** Asks why before cancelling. The reason is kept on the appointment. */
export function CancelForm({ pending, onCancel, onBack }: CancelFormProps) {
  const text = appointmentDetail.cancelForm;
  const [reason, setReason] = useState('');

  return (
    <form
      className="space-y-3"
      onSubmit={(event) => {
        event.preventDefault();
        onCancel(reason);
      }}
    >
      <Field label={text.reason} htmlFor="cancel-reason">
        <Input
          id="cancel-reason"
          value={reason}
          placeholder={text.placeholder}
          maxLength={500}
          autoFocus
          onChange={(event) => setReason(event.target.value)}
        />
      </Field>
      <div className="flex justify-end gap-2">
        <Button variant="ghost" size="sm" onClick={onBack} disabled={pending}>
          {text.back}
        </Button>
        <Button type="submit" variant="danger" size="sm" disabled={pending}>
          {pending ? appointmentDetail.actions.working : text.submit}
        </Button>
      </div>
    </form>
  );
}
