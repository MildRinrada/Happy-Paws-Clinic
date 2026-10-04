'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { FormStatus } from '@/components/ui/form-status';
import type { Customer } from '@/features/customers/types/customer.types';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { addCarerAction } from '../../actions/carer-actions';
import { carers } from '../../content/carers';
import { petForms } from '../../content/pet-forms';
import { CustomerPicker } from './customer-picker';

/**
 * Links another customer to the pet: they may then book for it and bring it in. Whether they may
 * also read its medical history is a separate choice, on by default as the backend has it.
 */
export function AddCarerForm({ petId, linkedIds }: { petId: string; linkedIds: string[] }) {
  const text = carers;
  const [open, setOpen] = useState(false);
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [mayViewHistory, setMayViewHistory] = useState(true);
  const [missing, setMissing] = useState(false);
  const { errors, message, pending, submit, touch } = useFormSubmit(petForms);

  function close() {
    setOpen(false);
    setCustomer(null);
    setMayViewHistory(true);
    setMissing(false);
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!customer) {
      setMissing(true);
      return;
    }

    submit(() => addCarerAction({ petId, customerId: customer.id, mayViewHistory }), close);
  }

  if (!open) {
    return (
      <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
        {text.add}
      </Button>
    );
  }

  return (
    <form onSubmit={save} className="space-y-3 rounded-lg border border-ink-200 bg-ink-50/60 p-4">
      <CustomerPicker
        picked={customer}
        linkedIds={linkedIds}
        error={missing ? text.pickFirst : errors.customer_id}
        onPick={(next) => {
          touch();
          setMissing(false);
          setCustomer(next);
        }}
      />

      <Checkbox
        label={text.mayViewHistory}
        hint={text.mayViewHistoryHint}
        checked={mayViewHistory}
        onChange={(event) => setMayViewHistory(event.target.checked)}
      />

      <div className="flex flex-wrap items-center justify-end gap-2">
        <FormStatus message={message} savedText="" />
        <Button variant="ghost" size="sm" onClick={close} disabled={pending}>
          {petForms.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? text.submitting : text.submit}
        </Button>
      </div>
    </form>
  );
}
