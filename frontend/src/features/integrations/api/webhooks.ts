import { staffApi } from '@/lib/api/server-client';
import type { CursorPage, CursorPageQuery } from '@/types/pagination';
import type { CreatedWebhook, WebhookDelivery, WebhookInput } from '../types/integration.types';

/** `POST /staff/webhooks` — needs `integration:manage`. The answer carries the signing secret, once. */
export function createWebhook(input: WebhookInput): Promise<CreatedWebhook> {
  return staffApi().post<CreatedWebhook>('/staff/webhooks', { body: input });
}

/** `GET /staff/webhooks/{id}/deliveries` — one webhook's deliveries, newest first. */
export function listWebhookDeliveries(id: string, query: CursorPageQuery = {}): Promise<CursorPage<WebhookDelivery>> {
  return staffApi().get<CursorPage<WebhookDelivery>>(`/staff/webhooks/${encodeURIComponent(id)}/deliveries`, {
    query: { limit: query.limit ?? 25, cursor: query.cursor },
  });
}
