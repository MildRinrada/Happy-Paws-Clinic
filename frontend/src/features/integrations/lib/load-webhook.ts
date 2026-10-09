import { authed } from '@/lib/auth/session';
import { listWebhooks } from '../api/webhooks';
import type { Webhook } from '../types/integration.types';

/**
 * One webhook, read from the list (there is no single read). Undefined when it is not in the list,
 * which means it was deleted or never existed; its delivery log may still be readable by id.
 */
export async function loadWebhook(id: string): Promise<Webhook | undefined> {
  const webhooks = await authed(listWebhooks);
  return webhooks.find((webhook) => webhook.id === id);
}
