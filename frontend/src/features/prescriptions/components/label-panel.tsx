'use client';

import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { prescriptionsPage } from '../content/prescriptions-page';
import type { DispenseResult } from '../types/prescription.types';
import { DispenseOutcome } from './dispense-outcome';
import { DrugLabel } from './drug-label';

/**
 * What a dispense hands back: the label to print, and what it did to stock and to the bill. Only
 * the label prints; the notes and buttons are `no-print`.
 */
export function LabelPanel({ result, onClose }: { result: DispenseResult; onClose: () => void }) {
  const text = prescriptionsPage;

  return (
    <div className="space-y-2">
      <div className="no-print space-y-2">
        <Alert tone="success">{text.dispense.done}</Alert>
        <DispenseOutcome result={result} />
      </div>

      <DrugLabel label={result.label} />

      <div className="no-print flex gap-2">
        <Button size="sm" onClick={() => window.print()}>
          {text.label.print}
        </Button>
        <Button size="sm" variant="ghost" onClick={onClose}>
          {text.label.close}
        </Button>
      </div>
    </div>
  );
}
