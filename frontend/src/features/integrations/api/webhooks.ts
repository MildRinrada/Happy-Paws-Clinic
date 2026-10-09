import { staffApi } from '@/lib/api/server-client';
import type { CursorPage, CursorPageQuery } from '@/types/pagination';
import type {
  CreatedWebhook,
  RotatedWebhookSecret,
  Webhook,
  WebhookDelivery,
  WebhookInput,
  WebhookPatch,
} from '../types/integration.types';

const path = (id: string) => `/staff/webhooks/${encodeURIComponent(id)}`;

/** `POST /staff/webhooks` — needs `integration:manage`. The answer carries the signing secret, once. */
export function createWebhook(input: WebhookInput): Promise<CreatedWebhook> {
  return staffApi().post<CreatedWebhook>('/staff/webhooks', { body: input });
}

/** `GET /staff/webhooks/{id}/deliveries` — one webhook's deliveries, newest first. */
export function listWebhookDeliveries(id: string, query: CursorPageQuery = {}): Promise<CursorPage<WebhookDelivery>> {
  return staffApi().get<CursorPage<WebhookDelivery>>(`${path(id)}/deliveries`, {
    query: { limit: query.limit ?? 25, cursor: query.cursor },
  });
}

/*
 * The rest are a contract extension (docs/contract-extensions.md § 22), all `integration:manage`.
 */

/** `GET /staff/webhooks` — every webhook not deleted, newest first. No secrets. */
export async function listWebhooks(): Promise<Webhook[]> {
  return (await staffApi().get<{ data: Webhook[] }>('/staff/webhooks')).data;
}

/** `PATCH /staff/webhooks/{id}` — a stale `version` comes back as `version_conflict`. */
export function updateWebhook(id: string, version: number, patch: WebhookPatch): Promise<Webhook> {
  return staffApi().patch<Webhook>(path(id), { body: patch, ifMatch: version });
}

/** `DELETE /staff/webhooks/{id}` — it stops and leaves the list; its delivery log stays. */
export function deleteWebhook(id: string, version: number): Promise<void> {
  return staffApi().delete<void>(path(id), { ifMatch: version });
}

/** `POST /staff/webhooks/{id}/rotate-secret` — the new secret, once; the old one stops signing. */
export function rotateWebhookSecret(id: string, version: number): Promise<RotatedWebhookSecret> {
  return staffApi().post<RotatedWebhookSecret>(`${path(id)}/rotate-secret`, { ifMatch: version });
}

/** `POST /staff/webhooks/{id}/deliveries/{deliveryId}/redeliver` — a failed delivery, queued once more. */
export function redeliverWebhookDelivery(id: string, deliveryId: string): Promise<WebhookDelivery> {
  return staffApi().post<WebhookDelivery>(`${path(id)}/deliveries/${encodeURIComponent(deliveryId)}/redeliver`);
}
