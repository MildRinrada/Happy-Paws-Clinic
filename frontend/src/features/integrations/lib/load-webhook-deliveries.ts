import { ApiError } from '@/lib/api/api-error';
import { authed } from '@/lib/auth/session';
import type { CursorPage } from '@/types/pagination';
import { listWebhookDeliveries } from '../api/webhooks';
import type { WebhookDelivery } from '../types/integration.types';

/** One page of a webhook's delivery log, or null when there is no webhook with that id. */
export async function loadWebhookDeliveries(id: string, cursor?: string): Promise<CursorPage<WebhookDelivery> | null> {
  try {
    return await authed(() => listWebhookDeliveries(id, { cursor }));
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null;
    }

    throw error;
  }
}
