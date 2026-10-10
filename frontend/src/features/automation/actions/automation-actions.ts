'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { createAutomation } from '../api/createAutomation';
import { deleteAutomation } from '../api/deleteAutomation';
import { simulateAutomation } from '../api/simulateAutomation';
import { updateAutomation } from '../api/updateAutomation';
import type { Automation, Simulation } from '../types/automation.types';

/**
 * Creating, editing, switching, deleting (`automation:manage`) and previewing a rule. The backend checks the scope, the
 * branch and every field again; these schemas only keep a malformed call from leaving the page.
 */

const optionalText = z.string().trim().max(200).optional();

const trigger = z.object({
  type: z.enum(['vaccine_due', 'appointment_upcoming', 'treatment_plan_session', 'customer_lapsed']),
  offset_days: z.number().int().min(-90).max(90).optional(),
  offset_hours: z.number().int().min(1).max(168).optional(),
  send_time: z.string().regex(/^\d{2}:\d{2}$/).optional(),
});

const channel = z.enum(['auto', 'line', 'sms', 'email']);

function refresh() {
  revalidatePath('/notifications/automations');
  revalidatePath('/notifications/reminders');
}

const createInput = z.object({
  name: z.string().trim().min(1).max(120),
  trigger,
  condition: z.object({
    branch_id: z.string().max(40).optional(),
    species: optionalText,
    vaccine_name: optionalText,
    unconfirmed_only: z.boolean().optional(),
  }),
  channel,
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

  refresh();
  return result;
}

const updateInput = z.object({
  id: z.string().min(1).max(40),
  version: z.number().int().nonnegative(),
  values: z.object({
    name: z.string().trim().min(1).max(120).optional(),
    trigger: trigger.optional(),
    // An empty string is sent as it is: it clears that condition.
    condition: z
      .object({ species: z.string().trim().max(200).optional(), vaccine_name: z.string().trim().max(200).optional(), unconfirmed_only: z.boolean().optional() })
      .optional(),
    channel: channel.optional(),
    enabled: z.boolean().optional(),
  }),
});

/** An edit, or just the on and off switch. A stale `version` comes back as `version_conflict`. */
export async function updateAutomationAction(payload: z.input<typeof updateInput>): Promise<ActionResult<Automation>> {
  const { id, version, values } = updateInput.parse(payload);

  const result = await runAction(() =>
    updateAutomation(id, version, {
      name: values.name,
      trigger: values.trigger,
      condition: values.condition,
      action: values.channel ? { channel: values.channel } : undefined,
      enabled: values.enabled,
    }),
  );

  refresh();
  return result;
}

const removal = z.object({ id: z.string().min(1).max(40), version: z.number().int().nonnegative() });

export async function deleteAutomationAction(payload: z.infer<typeof removal>): Promise<ActionResult> {
  const { id, version } = removal.parse(payload);
  const result = await runAction(() => deleteAutomation(id, version));

  refresh();
  return result.ok ? { ok: true, data: undefined } : result;
}

const simulateInput = z.object({ id: z.string().min(1).max(40), days: z.number().int().min(1).max(30) });

export async function simulateAutomationAction(values: z.infer<typeof simulateInput>): Promise<ActionResult<Simulation>> {
  const { id, days } = simulateInput.parse(values);

  return runAction(() => simulateAutomation(id, days));
}
