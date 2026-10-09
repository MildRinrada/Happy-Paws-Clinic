'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { revokeApiKey } from '../api/apiKeys';
import { rotateCalendarLink } from '../api/calendarLink';
import { deleteWebhook, redeliverWebhookDelivery, rotateWebhookSecret, updateWebhook } from '../api/webhooks';
import { integrationPaths } from '../lib/integration-paths';
import type { CalendarLink, RotatedWebhookSecret, Webhook, WebhookDelivery } from '../types/integration.types';

/**
 * Managing what the create calls made (docs/contract-extensions.md § 22). Every write re-reads the
 * page it changed. A new secret goes back to the button that asked for it and is shown there once;
 * nothing here keeps it.
 */

const id = z.string().min(1).max(40);
const version = z.number().int().nonnegative();

const webhookUpdate = z.object({
  id,
  version,
  values: z.object({
    url: z.string().trim().min(1).max(2000).optional(),
    events: z.array(z.string().max(60)).min(1).max(16).optional(),
    description: z.string().trim().max(200).optional(),
    active: z.boolean().optional(),
  }),
});

/** An edit, or just the on and off switch. A stale `version` comes back as `version_conflict`. */
export async function updateWebhookAction(payload: z.infer<typeof webhookUpdate>): Promise<ActionResult<Webhook>> {
  const { id: webhookId, version: current, values } = webhookUpdate.parse(payload);
  const result = await runAction(() => updateWebhook(webhookId, current, values));

  revalidatePath(integrationPaths.webhooks);
  return result;
}

const webhookRef = z.object({ id, version });

export async function deleteWebhookAction(payload: z.infer<typeof webhookRef>): Promise<ActionResult> {
  const { id: webhookId, version: current } = webhookRef.parse(payload);
  const result = await runAction(() => deleteWebhook(webhookId, current));

  revalidatePath(integrationPaths.webhooks);
  return result.ok ? { ok: true, data: undefined } : result;
}

export async function rotateWebhookSecretAction(payload: z.infer<typeof webhookRef>): Promise<ActionResult<RotatedWebhookSecret>> {
  const { id: webhookId, version: current } = webhookRef.parse(payload);
  const result = await runAction(() => rotateWebhookSecret(webhookId, current));

  revalidatePath(integrationPaths.webhooks);
  return result;
}

const redelivery = z.object({ id, deliveryId: id });

export async function redeliverWebhookAction(payload: z.infer<typeof redelivery>): Promise<ActionResult<WebhookDelivery>> {
  const { id: webhookId, deliveryId } = redelivery.parse(payload);
  const result = await runAction(() => redeliverWebhookDelivery(webhookId, deliveryId));

  revalidatePath(integrationPaths.webhookLog(webhookId));
  return result;
}

export async function revokeApiKeyAction(payload: { id: string }): Promise<ActionResult> {
  const keyId = id.parse(payload.id);
  const result = await runAction(() => revokeApiKey(keyId));

  revalidatePath(integrationPaths.apiKeys);
  return result.ok ? { ok: true, data: undefined } : result;
}

export async function rotateCalendarLinkAction(): Promise<ActionResult<CalendarLink>> {
  const result = await runAction(() => rotateCalendarLink());

  revalidatePath(integrationPaths.calendar);
  return result;
}
