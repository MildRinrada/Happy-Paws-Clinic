'use client';

import { useState } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { recordDeath } from '../../content/record-death';
import { RecordDeathForm } from './record-death-form';

/**
 * Closing a record, behind a button and a confirmation. Stays mounted while the page reloads as
 * deceased, so the answer (how many visits were cancelled) is still on screen afterwards.
 */
export function RecordDeathCard({ petId, deceased }: { petId: string; deceased: boolean }) {
  const text = recordDeath;
  const [open, setOpen] = useState(false);
  const [cancelled, setCancelled] = useState<number>();

  if (cancelled !== undefined) return <Alert tone="success">{text.done(cancelled)}</Alert>;
  if (deceased) return null;

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody>
        {open ? (
          <RecordDeathForm petId={petId} onCancel={() => setOpen(false)} onDone={setCancelled} />
        ) : (
          <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
            {text.open}
          </Button>
        )}
      </CardBody>
    </Card>
  );
}
