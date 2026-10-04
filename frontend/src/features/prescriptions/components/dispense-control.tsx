'use client';

import { useState, useTransition } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { dispensePrescriptionAction } from '../actions/prescription-actions';
import { prescriptionsPage } from '../content/prescriptions-page';
import type { DispenseResult } from '../types/prescription.types';
import { LabelPanel } from './label-panel';

interface Props {
  prescriptionId: string;
  dispensed: boolean;
  canDispense: boolean;
  /** Asks the counter to check with the vet in the confirm step. Does not block: the backend does not. */
  duplicate: boolean;
}

/**
 * "จ่ายยา", in two presses: handing over is final, so the first press asks. The label is kept here
 * after the page re-reads, which is why the row this sits in keeps its key when the list reorders.
 */
export function DispenseControl({ prescriptionId, dispensed, canDispense, duplicate }: Props) {
  const text = prescriptionsPage.dispense;
  const [confirming, setConfirming] = useState(false);
  const [result, setResult] = useState<DispenseResult>();
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  if (result) {
    return <LabelPanel result={result} onClose={() => setResult(undefined)} />;
  }

  if (dispensed || !canDispense) return null;

  function dispense() {
    setError(undefined);
    startTransition(async () => {
      try {
        const outcome = await dispensePrescriptionAction({ prescriptionId });

        if (outcome.ok) {
          setConfirming(false);
          setResult(outcome.data);
        } else {
          setError(outcome.error.message);
        }
      } catch {
        setError(text.unreachable);
      }
    });
  }

  return (
    <div className="no-print space-y-2">
      {error && <Alert>{error}</Alert>}

      {confirming ? (
        <div className="space-y-2">
          {duplicate && <p className="text-sm font-medium text-red-800">{text.warnDuplicate}</p>}
          <div className="flex gap-2">
            <Button size="sm" disabled={pending} onClick={dispense}>
              {pending ? text.working : text.confirm}
            </Button>
            <Button size="sm" variant="ghost" disabled={pending} onClick={() => setConfirming(false)}>
              {text.cancel}
            </Button>
          </div>
        </div>
      ) : (
        <Button size="sm" onClick={() => setConfirming(true)}>
          {text.button}
        </Button>
      )}
    </div>
  );
}
