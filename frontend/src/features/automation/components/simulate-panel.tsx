'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Select } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { simulateAutomationAction } from '../actions/automation-actions';
import { automationsPage } from '../content/automations-page';
import type { Simulation } from '../types/automation.types';
import { SimulationResult } from './simulation-result';

/** "ดูว่าจะส่งถึงใครบ้างก่อนเปิดใช้จริง": runs the preview for one rule and shows who would get what. */
export function SimulatePanel({ automationId }: { automationId: string }) {
  const text = automationsPage.simulate;
  const [days, setDays] = useState(7);
  const [simulation, setSimulation] = useState<Simulation>();
  const { message, pending, submit } = useFormSubmit({ conflict: formMessages.conflict, unreachable: formMessages.unreachable });

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <label className="flex items-center gap-2 text-sm text-ink-700">
          {text.days}
          <Select value={days} onChange={(event) => setDays(Number(event.target.value))} className="h-8 w-24">
            {text.dayChoices.map((choice) => (
              <option key={choice} value={choice}>
                {text.dayLabel(choice)}
              </option>
            ))}
          </Select>
        </label>
        <Button variant="secondary" size="sm" disabled={pending} onClick={() => submit(() => simulateAutomationAction({ id: automationId, days }), setSimulation)}>
          {pending ? text.running : text.button}
        </Button>
        {simulation && (
          <Button variant="ghost" size="sm" onClick={() => setSimulation(undefined)}>
            {text.close}
          </Button>
        )}
        <FormStatus message={message} savedText="" />
      </div>
      {simulation && <SimulationResult simulation={simulation} />}
    </div>
  );
}
