'use client';

import { useState } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { undoDeath } from '../../content/undo-death';
import type { CancelledVisit } from '../../types/pet.types';
import { CancelledVisitsList } from './cancelled-visits-list';
import { UndoDeathForm } from './undo-death-form';

/**
 * On a closed record: take the death back, behind a confirmation that says what will and will not
 * happen. Stays mounted while the page reloads as alive, so the visits to rebook stay on screen.
 */
export function UndoDeathCard({ petId, deceased }: { petId: string; deceased: boolean }) {
  const text = undoDeath;
  const [open, setOpen] = useState(false);
  const [cancelled, setCancelled] = useState<CancelledVisit[]>();

  if (cancelled !== undefined) {
    return (
      <div className="space-y-3">
        <Alert tone="success">{text.done}</Alert>
        <CancelledVisitsList visits={cancelled} />
      </div>
    );
  }

  if (!deceased) return null;

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody>
        {open ? (
          <UndoDeathForm petId={petId} onCancel={() => setOpen(false)} onDone={setCancelled} />
        ) : (
          <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
            {text.open}
          </Button>
        )}
      </CardBody>
    </Card>
  );
}
