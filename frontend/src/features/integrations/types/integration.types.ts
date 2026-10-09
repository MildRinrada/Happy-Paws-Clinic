/** Shapes of the "Webhooks and Integrations" endpoints, as the backend sends them (snake_case). */

/** `POST /staff/webhooks`. `secret` is in this answer and nowhere else, ever. */
export interface CreatedWebhook {
  id: string;
  url: string;
  events: string[];
  description?: string;
  active: boolean;
  secret: string;
  signing: { algorithm: string; header: string; timestamp_header: string; signed_payload: string };
  created_at: string;
}

/**
 * A webhook as `GET /staff/webhooks` lists it (docs/contract-extensions.md § 22). No secret, ever;
 * `version` goes back as `If-Match`.
 */
export interface Webhook {
  id: string;
  url: string;
  events: string[];
  description?: string;
  active: boolean;
  version: number;
  created_by_staff_id: string;
  created_at: string;
  updated_at?: string;
}

/** `PATCH /staff/webhooks/{id}`: every field optional; an empty description clears it. */
export interface WebhookPatch {
  url?: string;
  events?: string[];
  description?: string;
  active?: boolean;
}

/** `POST /staff/webhooks/{id}/rotate-secret`. `secret` is in this answer and nowhere else. */
export interface RotatedWebhookSecret {
  id: string;
  secret: string;
  signing: CreatedWebhook['signing'];
  version: number;
  rotated_at: string;
}

export interface WebhookInput {
  url: string;
  events: string[];
  description?: string;
}

/** One event on its way to one webhook, with how its latest attempt went. */
export interface WebhookDelivery {
  id: string;
  event_id: string;
  event_type: string;
  status: 'pending' | 'succeeded' | 'failed';
  attempts: number;
  max_attempts: number;
  next_attempt_at?: string;
  last_attempt_at?: string;
  last_status_code?: number;
  last_error?: string;
  last_response_body?: string;
  last_duration_ms?: number;
  completed_at?: string;
  created_at: string;
}

/** `POST /staff/api-keys`. `key` is in this answer and nowhere else, ever. */
export interface CreatedApiKey {
  id: string;
  name: string;
  key: string;
  prefix: string;
  scopes: string[];
  branch_ids: string[];
  expires_at: string;
  created_at: string;
}

export type ApiKeyStatus = 'active' | 'expired' | 'revoked';

/** A key as `GET /staff/api-keys` lists it: enough to recognise and revoke it, never the key. */
export interface ApiKeySummary {
  id: string;
  name: string;
  prefix: string;
  scopes: string[];
  branch_ids: string[];
  status: ApiKeyStatus;
  created_by_staff_id: string;
  created_at: string;
  expires_at: string;
  last_used_at?: string;
  revoked_at?: string;
}

export interface ApiKeyInput {
  name: string;
  scopes: string[];
  branch_ids?: string[];
  expires_at: string;
}

/** `PUT /staff/integrations/payment`. Secrets never come back, only whether one is set. */
export interface PaymentSettings {
  version: number;
  provider?: string;
  mode: 'test' | 'live';
  enabled: boolean;
  public_key?: string;
  has_secret_key: boolean;
  has_webhook_secret: boolean;
  connected: boolean;
  updated_at?: string;
}

export interface PaymentSettingsInput {
  provider?: string;
  mode?: 'test' | 'live';
  enabled?: boolean;
  public_key?: string;
  secret_key?: string;
  webhook_secret?: string;
}

/** `PUT /staff/integrations/lab`. The API key never comes back. */
export interface LabSettings {
  version: number;
  provider?: string;
  enabled: boolean;
  account_id?: string;
  base_url?: string;
  has_api_key: boolean;
  connected: boolean;
  updated_at?: string;
}

export interface LabSettingsInput {
  provider?: string;
  enabled?: boolean;
  account_id?: string;
  base_url?: string;
  api_key?: string;
}

/** The JSON form of `GET /staff/integrations/calendar/ics`: the signed-in person's link. */
export interface CalendarLink {
  path: string;
  token: string;
  can_view_branch: boolean;
  created_at: string;
  last_used_at?: string;
}

/** A write-only credential as typed in a form: the new value, or a request to clear the stored one. */
export interface SecretDraft {
  value: string;
  /** Sends an empty string, which clears what is stored. */
  clear: boolean;
}
