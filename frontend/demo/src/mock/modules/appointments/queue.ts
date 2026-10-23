import { emit } from '../../core/bus';
import { nextNumber, nowIso, table } from '../../core/db';
import { fail, stateConflict } from '../../core/http';
import { send } from '../../core/outbox';
import { branchOf, customerOf, petOf } from './catalog';
import { appointments, tickets, type TicketPriority, type TicketRow, type TicketStatus } from './store';
import { clinicToday } from './time';

/**
 * Today's queue: numbers, the call order and the state machine (waiting → called → in_room →
 * served, with skipped and left), as `QueueTicketTransitions` draws it. Every change is announced
 * on the bus as the backend's stream would, so the console and a waiting-room screen in another
 * tab move together.
 */

const ALLOWED: Record<TicketStatus, TicketStatus[]> = {
  waiting: ['called', 'skipped', 'left'],
  called: ['in_room', 'skipped', 'left'],
  skipped: ['called', 'left'],
  in_room: ['served'],
  served: [],
  left: [],
};

export const LIVE: TicketStatus[] = ['waiting', 'called', 'skipped', 'in_room'];
export const AWAITING: TicketStatus[] = ['waiting', 'skipped'];

const RANK: Record<TicketPriority, number> = { emergency: 2, urgent: 1, normal: 0 };

/** The order tickets are called in: urgency, then not passed over by number, then passed over by when. */
export function callOrder(a: TicketRow, b: TicketRow): number {
  return (
    RANK[b.priority] - RANK[a.priority] ||
    (a.skipped_at ? 1 : 0) - (b.skipped_at ? 1 : 0) ||
    (a.skipped_at ?? '').localeCompare(b.skipped_at ?? '') ||
    a.sequence - b.sequence
  );
}

export type QueueEventType = 'ticket.updated' | 'ticket.called' | 'board.refresh';

/** What the backend's stream says: the branch and what kind of change, never a pet or an owner. */
export function announce(branchId: string, type: QueueEventType = 'ticket.updated', ticketId?: string): void {
  emit('queue', { branch_id: branchId, type, ...(ticketId ? { ticket_id: ticketId } : {}) });
}

export function boardOf(branchId: string, date = clinicToday(), includeFinished = false): TicketRow[] {
  return tickets()
    .where((row) => row.branch_id === branchId && row.service_date === date && (includeFinished || LIVE.includes(row.status)))
    .sort(callOrder);
}

/** The clinic's numbering (settings > queue): prefix and digits, `A014` by default. */
function numberOf(sequence: number): string {
  const settings = table('clinic_settings').get('queue');
  const prefix = typeof settings?.number_prefix === 'string' ? settings.number_prefix : 'A';
  const digits = Number(settings?.number_digits) || 3;
  return `${prefix}${String(sequence).padStart(digits, '0')}`;
}

export interface NewTicket {
  branchId: string;
  petId: string;
  customerId: string;
  serviceId: string;
  appointmentId?: string;
  staffId?: string;
  priority?: TicketPriority;
  priorityReason?: string;
  date?: string;
  issuedAt?: string;
}

/** Issues the next number of the day at a branch. */
export function issueTicket(input: NewTicket, options: { silent?: boolean } = {}): TicketRow {
  const date = input.date ?? clinicToday();
  const sequence = nextNumber(`queue:${input.branchId}:${date}`);
  const row = tickets().insert({
    version: 1,
    number: numberOf(sequence),
    branch_id: input.branchId,
    service_date: date,
    status: 'waiting',
    priority: input.priority ?? 'normal',
    pet_id: input.petId,
    customer_id: input.customerId,
    service_id: input.serviceId,
    ...(input.appointmentId ? { appointment_id: input.appointmentId } : {}),
    ...(input.staffId ? { staff_id: input.staffId } : {}),
    ...(input.priorityReason ? { priority_reason: input.priorityReason } : {}),
    skip_count: 0,
    issued_at: input.issuedAt ?? nowIso(),
    sequence,
  });
  if (!options.silent) announce(row.branch_id, 'ticket.updated', row.id);
  return row;
}

function move(ticket: TicketRow, to: TicketStatus, patch: Partial<TicketRow> = {}): TicketRow {
  if (!ALLOWED[ticket.status].includes(to)) {
    stateConflict(`คิว ${ticket.number} อยู่ในสถานะที่ทำรายการนี้ไม่ได้แล้ว โปรดโหลดหน้าคิวใหม่`);
  }
  return tickets().update(ticket.id, { status: to, ...patch })!;
}

export function ticketOrFail(id: string): TicketRow {
  const row = tickets().get(id);
  if (!row) fail(404, 'not_found', 'ไม่พบบัตรคิวนี้');
  return row;
}

/** Calls a ticket: the owner gets "ถึงคิวของคุณแล้ว" by LINE or SMS and the screens flash the number. */
export function callTicket(id: string, staffId?: string): TicketRow {
  const ticket = ticketOrFail(id);
  const called = move(ticket, 'called', { called_at: nowIso(), ...(ticket.staff_id || !staffId ? {} : { staff_id: staffId }) });
  notifyCalled(called);
  announce(called.branch_id, 'ticket.called', called.id);
  return called;
}

function notifyCalled(ticket: TicketRow) {
  const customer = customerOf(ticket.customer_id);
  const pet = petOf(ticket.pet_id);
  if (!customer) return;
  const where = ticket.room_id ? ` เชิญที่${ticket.room_id}` : ' เชิญที่หน้าห้องตรวจ';
  const line = Boolean(customer.line?.line_user_id && !customer.line.unfollowed_at);
  send({
    channel: line ? 'line' : 'sms',
    to: line ? (customer.line.display_name ?? customer.display_name ?? '') : (customer.phone ?? ''),
    body: `Happy Paws: ถึงคิวของคุณแล้ว หมายเลข ${ticket.number}${pet?.name ? ` (น้อง${pet.name})` : ''}${where}`,
  });
}

/** Back of the line: called and did not come forward, or not here yet. */
export function skipTicket(id: string): TicketRow {
  const ticket = ticketOrFail(id);
  const skipped = move(ticket, 'skipped', { skip_count: ticket.skip_count + 1, skipped_at: nowIso() });
  announce(skipped.branch_id, 'ticket.updated', skipped.id);
  return skipped;
}

export function leaveTicket(id: string, reason?: string): TicketRow {
  const ticket = ticketOrFail(id);
  const left = move(ticket, 'left', { left_at: nowIso(), ...(reason ? { left_reason: reason } : {}) });
  announce(left.branch_id, 'ticket.updated', left.id);
  return left;
}

/** Puts a vet and/or a room on a ticket; moving a called ticket to another room is a transfer. */
export function assignTicket(id: string, change: { staffId?: string; roomId?: string }): TicketRow {
  const ticket = ticketOrFail(id);
  if (!LIVE.includes(ticket.status)) stateConflict(`คิว ${ticket.number} จบไปแล้ว`);
  if (change.roomId) {
    const rooms = (branchOf(ticket.branch_id)?.exam_rooms ?? []) as string[];
    if (rooms.length && !rooms.includes(change.roomId)) fail(422, 'validation_failed', 'ไม่พบห้องตรวจนี้ในสาขา', { field: 'room_id' });
  }
  const next = tickets().update(ticket.id, {
    ...(change.staffId ? { staff_id: change.staffId } : {}),
    ...(change.roomId ? { room_id: change.roomId } : {}),
  })!;
  if (next.appointment_id && change.staffId) appointments().update(next.appointment_id, { staff_id: change.staffId });
  announce(next.branch_id, 'ticket.updated', next.id);
  return next;
}

export function setPriority(id: string, priority: TicketPriority, reason?: string): TicketRow {
  const ticket = ticketOrFail(id);
  if (!AWAITING.includes(ticket.status) && ticket.status !== 'called') stateConflict(`คิว ${ticket.number} เข้าห้องตรวจแล้ว เปลี่ยนความเร่งด่วนไม่ได้`);
  if (priority !== 'normal' && !reason?.trim()) {
    fail(409, 'invalid_priority', 'กรุณาระบุเหตุผลเมื่อเพิ่มความเร่งด่วน', { field: 'reason' });
  }
  const next = tickets().update(ticket.id, { priority, priority_reason: priority === 'normal' ? undefined : reason!.trim() })!;
  announce(next.branch_id, 'ticket.updated', next.id);
  return next;
}

/**
 * The pet went into the exam room: for the consultation module when a visit is opened. The
 * appointment, if there is one, becomes `in_consultation`.
 */
export function enterRoom(id: string, roomId?: string, staffId?: string): TicketRow {
  let ticket = ticketOrFail(id);
  // The vet opened the visit straight from the waiting line: the call happened at the door.
  if (AWAITING.includes(ticket.status)) ticket = move(ticket, 'called', { called_at: nowIso() });
  const next = move(ticket, 'in_room', {
    entered_room_at: nowIso(),
    ...(roomId ? { room_id: roomId } : {}),
    ...(staffId && !ticket.staff_id ? { staff_id: staffId } : {}),
  });
  if (next.appointment_id) appointments().update(next.appointment_id, { status: 'in_consultation' });
  announce(next.branch_id, 'ticket.updated', next.id);
  return next;
}

/** The visit is closed: for the consultation module. The appointment becomes `completed`. */
export function serveTicket(id: string): TicketRow {
  const ticket = ticketOrFail(id);
  const next = move(ticket, 'served', { served_at: nowIso() });
  if (next.appointment_id) appointments().update(next.appointment_id, { status: 'completed' });
  announce(next.branch_id, 'ticket.updated', next.id);
  return next;
}

export const ticketOfAppointment = (appointmentId: string) => tickets().find((row) => row.appointment_id === appointmentId);

/** Today's live tickets of a customer, for the portal's visit card. */
export function liveTicketsOfCustomer(customerId: string): TicketRow[] {
  const today = clinicToday();
  return tickets().where((row) => row.customer_id === customerId && row.service_date === today && LIVE.includes(row.status));
}

/**
 * How many are to be called before this ticket, in the board's own order. 0 once it has been
 * called (or is past waiting): nobody is ahead of it any more.
 */
export function aheadOf(ticket: TicketRow): number {
  const index = boardOf(ticket.branch_id, ticket.service_date)
    .filter((row) => AWAITING.includes(row.status))
    .findIndex((row) => row.id === ticket.id);
  return Math.max(0, index);
}
