'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { customerDetail } from '@/features/customers/content/customer-detail';
import { registerPet } from '../content/register-pet';
import type { PetSummary } from '../types/pet.types';
import { RegisterPetForm } from './register-pet-form';

interface AddPetPanelProps {
  customerId: string;
  /** Open straight away, as it is right after the customer was registered. */
  startOpen: boolean;
}

/**
 * The "add a pet" button and the form it opens, at the foot of the pet list. After a save the
 * form closes and says which HN was issued, so the counter can write it on the card.
 */
export function AddPetPanel({ customerId, startOpen }: AddPetPanelProps) {
  const [open, setOpen] = useState(startOpen);
  const [saved, setSaved] = useState<PetSummary>();

  if (!open) {
    return (
      <div className="flex flex-wrap items-center justify-between gap-3 px-5 py-3">
        {saved ? (
          <p role="status" className="text-sm text-emerald-700">
            {registerPet.saved(saved.name, saved.hospital_number)}
          </p>
        ) : (
          <span />
        )}
        <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
          {customerDetail.pets.add}
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-3 bg-ink-50/60 px-5 py-4">
      <h3 className="font-display text-sm font-semibold text-ink-900">{registerPet.title}</h3>
      <RegisterPetForm
        customerId={customerId}
        onCancel={() => setOpen(false)}
        onSaved={(pet) => {
          setSaved(pet);
          setOpen(false);
        }}
      />
    </div>
  );
}
