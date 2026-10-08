'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { createAutomation } from '../api/createAutomation';
import { simulateAutomation } from '../api/simulateAutomation';
import type { Automation, Simulation } from '../types/automation.types';

/**
 * Creating a rule (`automation:manage`) and previewing one. The backend checks the scope, the
 * branch and every field again; these schemas only keep a malformed call from leaving the page.
 */

const optionalText = z.string().trim().max(200).optional();

const createInput = z.object({
  name: z.string().trim().min(1).max(120),
  trigger: z.object({
    type: z.enum(['vaccine_due', 'appointment_upcoming', 'treatment_plan_session']),
    offset_days: z.number().int().min(-30).max(90).optional(),
    offset_hours: z.number().int().min(1).max(168).optional(),
    send_time: z.string().regex(/^\d{2}:\d{2}$/).optional(),
  }),
  condition: z.object({
    branch_id: z.string().max(40).optional(),
    species: optionalText,
    vaccine_name: optionalText,
    unconfirmed_only: z.boolean().optional(),
  }),
  channel: z.enum(['auto', 'line', 'sms', 'email']),
  enabled: z.boolean(),
});

/** Empty text means "no condition", so it is left out rather than sent as a blank. */
const present = (value: string | undefined) => (value && value.trim() !== '' ? value.trim() : undefined);

export async function createAutomationAction(values: z.input<typeof createInput>): Promise<ActionResult<Automation>> {
  const input = createInput.parse(values);

  const result = await runAction(() =>
    createAutomation({
      name: input.name,
      trigger: input.trigger,
      condition: {
        branch_id: input.condition.branch_id,
        species: present(input.condition.species),
        vaccine_name: present(input.condition.vaccine_name),
        unconfirmed_only: input.condition.unconfirmed_only || undefined,
      },
      action: { type: 'send_notification', channel: input.channel },
      enabled: input.enabled,
    }),
  );

  revalidatePath('/notifications/automations');
  revalidatePath('/notifications/reminders');
  return result;
}

const simulateInput = z.object({ id: z.string().min(1).max(40), days: z.number().int().min(1).max(30) });

export async function simulateAutomationAction(values: z.infer<typeof simulateInput>): Promise<ActionResult<Simulation>> {
  const { id, days } = simulateInput.parse(values);

  return runAction(() => simulateAutomation(id, days));
}
