'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { editPet } from '../../content/edit-pet';
import type { PetSummary } from '../../types/pet.types';
import { EditPetForm } from './edit-pet-form';

/** Staff's correction of the identity card, behind a button (docs/contract-extensions.md § 28). */
export function EditPetCard({ pet }: { pet: PetSummary }) {
  const text = editPet;
  const [open, setOpen] = useState(false);

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody>
        {open ? (
          <EditPetForm pet={pet} onClose={() => setOpen(false)} />
        ) : (
          <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
            {text.open}
          </Button>
        )}
      </CardBody>
    </Card>
  );
}
