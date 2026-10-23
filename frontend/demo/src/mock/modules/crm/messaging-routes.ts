import { created, fail, invalid, notFound, page, requireCustomer, requireStaff, route } from '../../core/http';
import { threads } from './store';
import { customerWrites, messagesOf, portalMessageView, staffReplies, threadOfCustomer, threadStatus, threadView } from './threads';

/**
 * Conversations with customers: the staff inbox (`/staff/threads`) and the owner's side of the same
 * thread in the portal (`/portal/thread`, docs/contract-extensions.md § 24).
 */

route('GET', '/staff/threads', (ctx) => {
  const auth = requireStaff(ctx, 'message:read');
  const q = ctx.query;
  const status = q.get('status');
  const customerId = q.get('customer_id');
  const threadId = q.get('thread_id');
  const messages = Math.max(0, Math.min(100, Number(q.get('messages') ?? 0) || 0));
  const rows = threads()
    .where((t) => !auth.branches.length || !t.branch_id || auth.branches.includes(t.branch_id))
    .filter((t) => (!customerId || t.customer_id === customerId) && (!threadId || t.id === threadId))
    .filter((t) => messagesOf(t.id).length > 0)
    .map((t) => ({ t, status: threadStatus(t) }))
    .filter((x) => !status || x.status === status)
    .map((x) => threadView(x.t, messages))
    .sort((a, b) => (a.status === b.status ? b.last_message_at.localeCompare(a.last_message_at) : a.status === 'awaiting_reply' ? -1 : 1));
  return page(rows, q, 30);
});

route('POST', '/staff/threads/:id/messages', (ctx) => {
  const auth = requireStaff(ctx, 'message:write');
  const thread = threads().get(ctx.params.id!);
  if (!thread) notFound('บทสนทนา');
  if (auth.branches.length && thread.branch_id && !auth.branches.includes(thread.branch_id)) {
    fail(403, 'insufficient_scope', 'บทสนทนานี้เป็นของสาขาอื่น');
  }
  const text = String(ctx.body?.text ?? '').trim();
  if (!text) invalid('text', 'กรุณาพิมพ์ข้อความ');
  if (text.length > 1000) invalid('text', 'ข้อความยาวเกิน 1,000 ตัวอักษร');
  const { customer_read_at: _r, ...message } = staffReplies(thread.id, text, auth.staffId);
  return created(message);
});

route('GET', '/portal/thread', (ctx) => {
  const auth = requireCustomer(ctx);
  const thread = threadOfCustomer(auth.customerId);
  if (!thread) return { data: [], page: { next_cursor: null } };
  const newestFirst = messagesOf(thread.id).reverse().map(portalMessageView);
  const limit = Math.max(1, Math.min(100, Number(ctx.query.get('limit')) || 50));
  const offset = Math.max(0, Number(ctx.query.get('cursor')) || 0);
  const data = newestFirst.slice(offset, offset + limit);
  return {
    id: thread.id,
    status: threadStatus(thread),
    data,
    page: { next_cursor: offset + limit < newestFirst.length ? String(offset + limit) : null },
  };
});

route('POST', '/portal/thread/messages', (ctx) => {
  const auth = requireCustomer(ctx);
  const text = String(ctx.body?.text ?? '').trim();
  if (!text) invalid('text', 'กรุณาพิมพ์ข้อความ');
  if (text.length > 1000) invalid('text', 'ข้อความยาวเกิน 1,000 ตัวอักษร');
  // The messaging rate limit: a burst of more than five in a minute is refused, as the backend does.
  const thread = threadOfCustomer(auth.customerId);
  const minuteAgo = new Date(Date.now() - 60_000).toISOString();
  if (thread && messagesOf(thread.id).filter((m) => m.direction === 'inbound' && m.sent_at > minuteAgo).length >= 5) {
    fail(429, 'rate_limited', 'ส่งข้อความถี่เกินไป กรุณารอสักครู่แล้วลองใหม่');
  }
  return created(portalMessageView(customerWrites(auth.customerId, text, 'portal')));
});
