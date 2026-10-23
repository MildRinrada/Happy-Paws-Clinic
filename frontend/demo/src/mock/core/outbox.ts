import { emit } from './bus';
import { nowIso, table, type Row } from './db';

/**
 * The messages the clinic would have sent: one-time codes, reminders, receipts. The demo sends
 * nothing anywhere; each lands here instead, and the demo bar shows it as it arrives.
 */

export type OutboxChannel = 'sms' | 'email' | 'line';

export interface OutboxMessage extends Row {
  channel: OutboxChannel;
  to: string;
  subject?: string;
  body: string;
  /** A one-time code in the message, shown large so it can be typed straight in. */
  code?: string;
  created_at: string;
}

const messages = () => table<OutboxMessage>('demo_outbox');

export function send(message: Omit<OutboxMessage, 'id' | 'created_at'>): OutboxMessage {
  const stored = messages().insert({ ...message, created_at: nowIso() });
  emit('outbox', stored);
  return stored;
}

export function listOutbox(): OutboxMessage[] {
  return messages()
    .all()
    .sort((a, b) => b.created_at.localeCompare(a.created_at));
}

export function clearOutbox(): void {
  messages().removeWhere(() => true);
  emit('outbox');
}
