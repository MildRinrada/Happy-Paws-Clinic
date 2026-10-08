/** Where a LINE credential in use comes from. */
export type CredentialSource = 'database' | 'configuration' | 'none';

/**
 * The answer of `PUT /staff/integrations/line`. It never carries the secret or the token, only
 * whether each is stored and which one is in use.
 */
export interface LineSettings {
  version: number;
  channel_id?: string;
  enabled: boolean;
  has_channel_secret: boolean;
  has_channel_access_token: boolean;
  secret_source: CredentialSource;
  token_source: CredentialSource;
  can_push: boolean;
  can_verify: boolean;
  /** Every template key, true when it may go out on LINE. */
  notification_types: Record<string, boolean>;
  broadcast_monthly_limit: number;
  /** Under the API's base URL, the address to give LINE as the webhook. */
  webhook_path: string;
  updated_at?: string;
}

/** Absent fields are left as they are; an empty string clears a stored credential. */
export interface LineSettingsInput {
  channel_id?: string;
  channel_secret?: string;
  channel_access_token?: string;
  enabled?: boolean;
  notification_types?: Record<string, boolean>;
  broadcast_monthly_limit?: number;
}

/** `POST /staff/integrations/line/broadcast`. */
export interface LineBroadcastInput {
  customer_ids: string[];
  message: string;
}
