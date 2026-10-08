/** `line`, `sms` or `email`. */
export type DeliveryChannel = 'line' | 'sms' | 'email';

/** `auto` lets the backend choose LINE, then SMS, then email. */
export type SendChannel = 'auto' | DeliveryChannel;

export type DeliveryStatus = 'sent' | 'failed' | 'skipped';

export type DeliverySource = 'system' | 'manual' | 'broadcast';

/** One row of `GET /staff/notifications/log`: one attempt on one channel. Absent keys are null. */
export interface Delivery {
  id: string;
  customer_id: string;
  customer_name?: string;
  template: string;
  channel?: DeliveryChannel;
  /** Masked: `***5678`, `s***@mail.com`, `U***a1b2`. */
  recipient?: string;
  status: DeliveryStatus;
  error_code?: string;
  /** The channel that failed just before this attempt. */
  fallback_from?: DeliveryChannel;
  source: DeliverySource;
  batch_id?: string;
  sent_by_staff_id?: string;
  branch_id?: string;
  attempted_at: string;
}

export interface DeliveryLogQuery {
  status?: string;
  channel?: string;
  source?: string;
  customer_id?: string;
  template?: string;
  batch_id?: string;
  cursor?: string;
}

export interface TemplateVariable {
  name: string;
  required: boolean;
  max_length: number;
}

/** One entry of `GET /staff/notifications/templates`. */
export interface NotificationTemplate {
  key: string;
  name: string;
  /** The consent it needs: `service_messages` or `marketing`. */
  purpose: string;
  staff_sendable: boolean;
  variables: TemplateVariable[];
  line_enabled: boolean;
  bodies: { title: string; line: string; sms: string };
}

/** What became of the message for one customer of a send. */
export interface RecipientResult {
  customer_id: string;
  status: DeliveryStatus | 'unknown';
  channel?: DeliveryChannel;
  error_code?: string;
  fallback_from?: DeliveryChannel;
}

/** The answer to a send or a broadcast. `quota` comes back from a broadcast only. */
export interface SendResult {
  batch_id: string;
  sent: number;
  failed: number;
  skipped: number;
  results: RecipientResult[];
  quota?: { limit: number; used: number; remaining: number };
}

/** `POST /staff/notifications/send`. */
export interface SendNotificationInput {
  template: string;
  customer_ids: string[];
  channel: SendChannel;
  variables: Record<string, string>;
}

/** A customer picked as a recipient, with what the screen shows of them. */
export interface Recipient {
  id: string;
  name: string;
  phone: string;
}
