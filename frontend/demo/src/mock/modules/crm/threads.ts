import { emit } from '../../core/bus';
import { nowIso, type Row } from '../../core/db';
import { dispatch, type Channel } from './notify';
import { customer, homeBranchOf, staffName, threadMessages, threads } from './store';

/**
 * One conversation per customer, across every channel (docs/contract-extensions.md § 24). The
 * thread row keeps only who and where; its status, unread count and last message are worked out
 * from the messages on every read, so a reply from the staff inbox and a message from the portal
 * always agree.
 */

export function messagesOf(threadId: string): Row[] {
  return threadMessages()
    .where((m) => m.thread_id === threadId)
    .sort((a, b) => String(a.sent_at).localeCompare(String(b.sent_at)));
}

/** The customer's thread, if they have one. */
export function threadOfCustomer(customerId: string): Row | undefined {
  return threads().find((t) => t.customer_id === customerId);
}

/** The customer's thread, opened now when they have none. */
export function ensureThread(customerId: string): Row {
  return (
    threadOfCustomer(customerId) ??
    threads().insert({ customer_id: customerId, branch_id: homeBranchOf(customerId), version: 1, created_at: nowIso() })
  );
}

const DELIVERY_CHANNELS = new Set(['line', 'sms', 'email']);

/** Status and counts of a thread, from its messages. */
function summary(thread: Row, list: Row[]) {
  const lastInbound = [...list].reverse().find((m) => m.direction === 'inbound');
  const lastReply = [...list].reverse().find((m) => m.direction === 'outbound');
  const awaiting = Boolean(lastInbound && (!lastReply || lastInbound.sent_at > lastReply.sent_at));
  const unread = list.filter((m) => m.direction === 'inbound' && (!lastReply || m.sent_at > lastReply.sent_at)).length;
  const last = list[list.length - 1];
  return {
    awaiting,
    unread,
    lastInbound,
    lastReply,
    last,
    replyChannel: lastInbound && DELIVERY_CHANNELS.has(lastInbound.channel) ? (lastInbound.channel as Channel) : undefined,
    lastMessageAt: last?.sent_at ?? thread.created_at,
  };
}

function messageView(m: Row) {
  const { customer_read_at: _r, ...rest } = m;
  return rest;
}

/** `GET /staff/threads`'s entry, with the newest `messageCount` messages, oldest first. */
export function threadView(thread: Row, messageCount = 0) {
  const list = messagesOf(thread.id);
  const s = summary(thread, list);
  const newest = messageCount > 0 ? list.slice(-messageCount) : [];
  return {
    id: thread.id,
    customer_id: thread.customer_id,
    ...(customer(thread.customer_id)?.display_name ? { customer_name: customer(thread.customer_id)!.display_name } : {}),
    ...(thread.branch_id ? { branch_id: thread.branch_id } : {}),
    status: s.awaiting ? 'awaiting_reply' : 'replied',
    unread_count: s.unread,
    message_count: list.length,
    ...(s.replyChannel ? { reply_channel: s.replyChannel } : {}),
    last_message_at: s.lastMessageAt,
    ...(s.lastInbound ? { last_inbound_at: s.lastInbound.sent_at } : {}),
    ...(s.lastReply ? { last_reply_at: s.lastReply.sent_at } : {}),
    ...(s.last ? { last_message: messageView(s.last) } : {}),
    messages: newest.map(messageView),
    has_more_messages: list.length > newest.length,
    version: thread.version ?? 1,
  };
}

export function threadStatus(thread: Row): 'awaiting_reply' | 'replied' {
  return summary(thread, messagesOf(thread.id)).awaiting ? 'awaiting_reply' : 'replied';
}

/** A message from the customer: from the portal, or (in the seed) from LINE. */
export function customerWrites(customerId: string, text: string, channel = 'portal', sentAt = nowIso()): Row {
  const thread = ensureThread(customerId);
  const message = threadMessages().insert({
    thread_id: thread.id,
    direction: 'inbound',
    channel,
    kind: 'text',
    text,
    sent_at: sentAt,
  });
  threads().update(thread.id, {});
  emit('messages', { thread_id: thread.id });
  return message;
}

/**
 * A staff answer. It goes out through the dispatcher as `thread.reply`, on the channel the customer
 * wrote on last, falling back as any notification does; the portal shows it in the conversation.
 */
export function staffReplies(threadId: string, text: string, staffId?: string): Row {
  const thread = threads().get(threadId)!;
  const s = summary(thread, messagesOf(threadId));
  const result = dispatch(thread.customer_id, 'thread.reply', { message: text }, {
    preferred: s.replyChannel,
    source: 'system',
    staffId,
    branchId: thread.branch_id,
  });
  const delivery =
    result.status === 'sent'
      ? { status: 'sent', channel: result.channel, ...(result.fallback_from ? { fallback_from: result.fallback_from } : {}) }
      : { status: result.status === 'unknown' ? 'failed' : result.status, ...(result.channel ? { channel: result.channel } : {}), ...(result.error_code ? { error_code: result.error_code } : {}) };
  const message = threadMessages().insert({
    thread_id: threadId,
    direction: 'outbound',
    ...(result.channel ? { channel: result.channel } : {}),
    kind: 'text',
    text,
    sent_at: nowIso(),
    ...(staffId ? { sent_by_staff_id: staffId, sent_by_name: staffName(staffId) ?? 'คลินิก' } : {}),
    delivery,
  });
  threads().update(threadId, {});
  emit('messages', { thread_id: threadId });
  return message;
}

/** `GET /portal/thread`'s message: the owner's view, without staff ids or delivery details. */
export function portalMessageView(m: Row) {
  return {
    id: m.id,
    direction: m.direction,
    ...(m.direction === 'inbound' && m.channel ? { channel: m.channel } : {}),
    kind: m.kind,
    ...(m.text !== undefined ? { text: m.text } : {}),
    sent_at: m.sent_at,
    ...(m.direction === 'outbound' && m.sent_by_name ? { sent_by_name: m.sent_by_name } : {}),
  };
}
