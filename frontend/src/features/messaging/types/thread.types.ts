import type { CursorPageQuery } from '@/types/pagination';

/** What became of a staff reply, as the delivery log recorded it. */
export interface ThreadDelivery {
  status: 'sent' | 'failed' | 'skipped';
  channel?: string;
  error_code?: string;
  fallback_from?: string;
}

/** One message of a thread: from the customer (`inbound`) or a staff reply (`outbound`). */
export interface ThreadMessage {
  id: string;
  thread_id: string;
  direction: 'inbound' | 'outbound';
  channel?: string;
  kind: 'text' | 'image' | 'video' | 'audio' | 'file' | 'location' | 'sticker' | 'other';
  /** Absent for a picture or a sticker: only words are kept. */
  text?: string;
  sent_at: string;
  sent_by_staff_id?: string;
  sent_by_name?: string;
  delivery?: ThreadDelivery;
}

export type ThreadStatus = 'awaiting_reply' | 'replied';

/** One customer's conversation with the clinic, across every channel. */
export interface MessageThread {
  id: string;
  customer_id: string;
  customer_name?: string;
  branch_id?: string;
  status: ThreadStatus;
  unread_count: number;
  message_count: number;
  /** The channel a reply tries first: the one the customer wrote on last. */
  reply_channel?: string;
  last_message_at: string;
  last_inbound_at?: string;
  last_reply_at?: string;
  last_message?: ThreadMessage;
  /** The newest messages, oldest first. */
  messages: ThreadMessage[];
  has_more_messages: boolean;
  version: number;
}

export interface ThreadListQuery extends CursorPageQuery {
  status?: ThreadStatus;
  customer_id?: string;
  thread_id?: string;
  messages?: number;
}
