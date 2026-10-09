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

/**
 * `POST /staff/integrations/line/broadcast`: to customers picked one by one, or to a CRM segment's
 * members, resolved by the backend at send time. Exactly one of the two.
 */
export type LineBroadcastInput = { message: string; customer_ids: string[] } | { message: string; segment_id: string };

/** The segment a broadcast was opened for, as the link from `/crm/segments` carries it. */
export interface BroadcastSegment {
  id: string;
  name: string;
  /** About how many members a broadcast would reach, when the link said. */
  reachable?: number;
}

/**
 * One recent follower of the clinic's LINE account, from `GET /staff/integrations/line/followers`
 * (docs/contract-extensions.md § 18). The counter picks one as a customer's LINE address.
 */
export interface LineFollower {
  line_user_id: string;
  display_name?: string;
  /** An `https` address on LINE's servers. */
  picture_url?: string;
  followed_at: string;
  last_seen_at: string;
  /** Only when listing linked followers too. */
  linked_customer_id?: string;
}

export interface LineFollowerPage {
  data: LineFollower[];
  page: { next_cursor?: string };
}
