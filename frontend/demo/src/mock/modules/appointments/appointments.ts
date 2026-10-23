import { nowIso, uuid } from '../../core/db';
import { fail, invalid, notFound, stateConflict } from '../../core/http';
import { send } from '../../core/outbox';
import { durationFor, resolveSlot, type SlotQuery } from './availability';
import { branchOf, branches, customerOf, petOf, serviceOf, vetName } from './catalog';
import { enterRoom, issueTicket, serveTicket, ticketOfAppointment } from './queue';
import {
  appointments,
  cancellationPolicy,
  holds,
  typeOf,
  waitingList,
  type AppointmentRow,
  type AppointmentStatus,
  type TicketRow,
} from './store';
import { addDays, clinicDayOf, clinicToday, thaiWhen } from './time';

/**
 * Booking, moving, cancelling and attending an appointment, with the side effects the backend has:
 * the owner is told through the outbox, a check-in puts the pet in today's queue, a cancellation
 * offers the freed time to the waiting list. Handlers and other modules call these.
 */

export const OPEN: AppointmentStatus[] = ['booked', 'confirmed'];

export function appointmentOrFail(id: string): AppointmentRow {
  const row = appointments().get(id);
  if (!row) notFound('นัดหมาย');
  return row;
}

// ---- Messages ---------------------------------------------------------------------------------------

/** One message to the pet's owner, by the channel they would get it on. */
export function tellOwner(customerId: string, subject: string, body: string): void {
  const customer = customerOf(customerId);
  if (!customer?.phone && !customer?.email) return;
  if (customer.phone) send({ channel: 'sms', to: customer.phone, subject, body: `Happy Paws: ${body}` });
  else send({ channel: 'email', to: customer.email, subject, body });
}

function describe(row: AppointmentRow): string {
  const pet = petOf(row.pet_id)?.name;
  const service = serviceOf(row.service_id)?.name ?? 'นัดหมาย';
  const branch = branchOf(row.branch_id)?.name;
  const vet = vetName(row.staff_id);
  return `${pet ? `น้อง${pet} ` : ''}${service} ${thaiWhen(row.starts_at)}${branch ? ` ${branch}` : ''}${vet ? ` กับ ${vet}` : ''}`;
}

// ---- Booking ----------------------------------------------------------------------------------------

export interface NewAppointment {
  petId: string;
  serviceId: string;
  startsAt: string;
  branchId?: string;
  staffId?: string;
  reason?: string;
  type?: string;
  /** The counter books past the notice period and the online switch; the owner does not. */
  source: 'staff' | 'online';
  /** The hold being turned into this appointment. */
  holdId?: string;
  seriesId?: string;
  /** For seeds: no message to the owner, no slot check. */
  quiet?: boolean;
  status?: AppointmentStatus;
  walkIn?: boolean;
}

/** The branch to book at when none was named: only a one-branch clinic may leave it out. */
function defaultBranch(): string {
  const open = branches();
  return open.length === 1 ? open[0]!.id : '';
}

/** Books one appointment after checking the slot, and texts the owner a confirmation. */
export function createAppointment(input: NewAppointment): AppointmentRow {
  const pet = petOf(input.petId);
  if (!pet || pet.is_deceased) invalid('pet_id', 'ไม่พบสัตว์เลี้ยงนี้');
  const type = typeOf(input.type);
  if (!type) invalid('type', 'ไม่พบประเภทนัดนี้');
  if (input.source === 'online' && !type.online_booking) invalid('type', 'ประเภทนัดนี้จองออนไลน์ไม่ได้');
  const branchId = input.branchId || defaultBranch();
  if (!branchId) invalid('branch_id', 'คลินิกมีหลายสาขา ต้องระบุสาขา');

  const request: Omit<SlotQuery, 'date'> & { startsAt: string } = {
    serviceId: input.serviceId,
    branchId,
    startsAt: input.startsAt,
    staffId: input.staffId,
    online: input.source === 'online',
    durationMinutes: type.duration_minutes,
    leadTimeMinutes: type.lead_time_minutes,
    excludeHoldId: input.holdId,
  };
  const slot = input.quiet
    ? { starts_at: new Date(input.startsAt).toISOString(), ends_at: new Date(Date.parse(input.startsAt) + durationFor(input.serviceId, input.type) * 60_000).toISOString(), staff_id: input.staffId ?? '' }
    : resolveSlot(request);

  const row = appointments().insert({
    version: 1,
    status: input.status ?? 'booked',
    branch_id: branchId,
    customer_id: pet.primary_customer_id,
    pet_id: pet.id,
    service_id: input.serviceId,
    staff_id: slot.staff_id || null,
    starts_at: slot.starts_at,
    ends_at: slot.ends_at,
    reason: input.reason || null,
    walk_in: input.walkIn ?? false,
    confirmed_at: null,
    checked_in_at: null,
    cancelled_at: null,
    cancellation_reason: null,
    type: type.code,
    series_id: input.seriesId ?? null,
    created_at: nowIso(),
    source: input.source,
  });

  if (input.holdId) holds().update(input.holdId, { consumed: true });
  if (!input.quiet) tellOwner(row.customer_id, 'ยืนยันการนัดหมาย', `ยืนยันการนัด ${describe(row)} หากต้องการเลื่อนหรือยกเลิก แจ้งได้ที่พอร์ทัลลูกค้า`);
  return row;
}

/** A course of visits: every one is checked first, and either all are booked or none is. */
export function createSeries(input: Omit<NewAppointment, 'startsAt' | 'seriesId'> & { starts: string[] }): { series_id: string; appointments: AppointmentRow[] } {
  const branchId = input.branchId || defaultBranch();
  const conflicts: Array<{ starts_at: string; reason: string; suggested: string[] }> = [];
  const type = typeOf(input.type);
  for (const startsAt of input.starts) {
    try {
      resolveSlot({ serviceId: input.serviceId, branchId, startsAt, staffId: input.staffId, durationMinutes: type?.duration_minutes });
    } catch (error) {
      const details = (error as { extra?: { details?: { suggested?: string[] } } }).extra?.details;
      conflicts.push({ starts_at: new Date(startsAt).toISOString(), reason: (error as Error).message, suggested: details?.suggested ?? [] });
    }
  }
  if (conflicts.length) {
    fail(422, 'slot_unavailable', `นัด ${conflicts.length} ครั้งในชุดนี้จองไม่ได้ จึงยังไม่ได้จองครั้งใดเลย`, {
      field: 'starts_at',
      details: { suggested: [], conflicts },
    });
  }
  const seriesId = uuid();
  const rows: AppointmentRow[] = [];
  for (const startsAt of input.starts) {
    // The first visit's vet keeps the course, so the pet sees the same face each time.
    const staffId = input.staffId ?? rows[0]?.staff_id ?? undefined;
    rows.push(createAppointment({ ...input, branchId, startsAt, staffId, seriesId, quiet: false }));
  }
  return { series_id: seriesId, appointments: rows };
}

// ---- Changes ----------------------------------------------------------------------------------------

export function confirmAppointment(id: string): AppointmentRow {
  const row = appointmentOrFail(id);
  if (row.status === 'confirmed') return row;
  if (row.status !== 'booked') stateConflict('นัดนี้ยืนยันไม่ได้ในสถานะปัจจุบัน');
  return appointments().update(row.id, { status: 'confirmed', confirmed_at: nowIso() })!;
}

/**
 * Cancels. Never refused for being late, only counted: inside the policy's cut-off the answer says
 * `late_cancellation: true`. The freed time is offered to the first matching waiting-list entry.
 */
export function cancelAppointment(id: string, reason?: string, by: 'staff' | 'customer' = 'staff'): AppointmentRow {
  const row = appointmentOrFail(id);
  if (!OPEN.includes(row.status)) stateConflict('นัดนี้ยกเลิกไม่ได้แล้ว');
  const policy = cancellationPolicy.get();
  const late = Date.parse(row.starts_at) - Date.now() < policy.cutoff_hours * 3_600_000;
  const next = appointments().update(row.id, {
    status: 'cancelled',
    cancelled_at: nowIso(),
    cancellation_reason: reason || (by === 'customer' ? 'ลูกค้ายกเลิกผ่านพอร์ทัล' : null),
    late_cancellation: by === 'customer' ? late : false,
  })!;
  tellOwner(next.customer_id, 'ยกเลิกนัดหมาย', `ยกเลิกนัด ${describe(next)} เรียบร้อยแล้ว`);
  offerToWaitingList(next);
  return next;
}

/** Moves to a new start (and optionally another vet). A moved appointment is `booked` again. */
export function rescheduleAppointment(id: string, change: { startsAt: string; staffId?: string; by: 'staff' | 'customer' }): AppointmentRow {
  const row = appointmentOrFail(id);
  if (!OPEN.includes(row.status)) stateConflict('นัดนี้เลื่อนไม่ได้ในสถานะปัจจุบัน');
  if (change.by === 'customer') {
    const policy = cancellationPolicy.get();
    if (Date.parse(row.starts_at) - Date.now() < policy.cutoff_hours * 3_600_000) {
      fail(409, 'cutoff_passed', `เลื่อนนัดออนไลน์ได้ก่อนเวลานัดอย่างน้อย ${policy.cutoff_hours} ชั่วโมง กรุณาโทรหาคลินิก`);
    }
  }
  const type = typeOf(row.type);
  const slot = resolveSlot({
    serviceId: row.service_id,
    branchId: row.branch_id,
    startsAt: change.startsAt,
    staffId: change.staffId ?? row.staff_id ?? undefined,
    online: change.by === 'customer',
    durationMinutes: type?.duration_minutes,
    excludeAppointmentId: row.id,
  });
  const next = appointments().update(row.id, {
    starts_at: slot.starts_at,
    ends_at: slot.ends_at,
    staff_id: slot.staff_id,
    status: 'booked',
    confirmed_at: null,
  })!;
  tellOwner(next.customer_id, 'เลื่อนนัดหมาย', `เลื่อนนัดเป็น ${describe(next)} เรียบร้อยแล้ว`);
  return next;
}

/** Checks in for today and issues the queue number, once: a second check-in answers the same ticket. */
export function checkInAppointment(id: string): { appointment: AppointmentRow; ticket: TicketRow } {
  const row = appointmentOrFail(id);
  const existing = ticketOfAppointment(row.id);
  if (existing && row.status === 'checked_in') return { appointment: row, ticket: existing };
  if (!OPEN.includes(row.status)) stateConflict('นัดนี้เช็กอินไม่ได้ในสถานะปัจจุบัน');
  if (clinicDayOf(row.starts_at) !== clinicToday()) stateConflict('เช็กอินได้เฉพาะนัดของวันนี้');
  const appointment = appointments().update(row.id, { status: 'checked_in', checked_in_at: nowIso() })!;
  const ticket = issueTicket({
    branchId: row.branch_id,
    petId: row.pet_id,
    customerId: row.customer_id,
    serviceId: row.service_id,
    appointmentId: row.id,
    staffId: row.staff_id ?? undefined,
    priority: typeOf(row.type)?.queue_priority ?? 'normal',
  });
  return { appointment, ticket };
}

export function markNoShow(id: string): { appointment: AppointmentRow; attendance: ReturnType<typeof attendanceOf> } {
  const row = appointmentOrFail(id);
  if (!OPEN.includes(row.status)) stateConflict('นัดนี้บันทึกว่าไม่มาไม่ได้ในสถานะปัจจุบัน');
  if (Date.parse(row.starts_at) > Date.now()) stateConflict('บันทึกว่าไม่มาได้หลังเวลานัดเท่านั้น');
  const appointment = appointments().update(row.id, { status: 'no_show', no_show_at: nowIso() })!;
  return { appointment, attendance: attendanceOf(row.customer_id) };
}

/**
 * The visit was opened in the consultation room. With a live queue ticket this is the ticket going
 * `in_room` (which moves the appointment); without one (a vet opening the case straight from the
 * calendar) the appointment alone becomes `in_consultation`.
 */
export function startAppointment(id: string, staffId?: string): AppointmentRow {
  const row = appointmentOrFail(id);
  const ticket = ticketOfAppointment(row.id);
  if (ticket && ['waiting', 'called', 'skipped'].includes(ticket.status)) {
    enterRoom(ticket.id, undefined, staffId);
    return appointmentOrFail(row.id);
  }
  if (row.status === 'in_consultation') return row;
  if (!['booked', 'confirmed', 'checked_in'].includes(row.status)) stateConflict('นัดนี้เริ่มตรวจไม่ได้ในสถานะปัจจุบัน');
  return appointments().update(row.id, {
    status: 'in_consultation',
    ...(row.checked_in_at ? {} : { checked_in_at: nowIso() }),
    ...(staffId && !row.staff_id ? { staff_id: staffId } : {}),
  })!;
}

/** The visit was closed: the ticket is served when there is one in the room, and the appointment `completed`. */
export function completeAppointment(id: string): AppointmentRow {
  const row = appointmentOrFail(id);
  const ticket = ticketOfAppointment(row.id);
  if (ticket?.status === 'in_room') {
    serveTicket(ticket.id);
    return appointmentOrFail(row.id);
  }
  if (row.status === 'completed') return row;
  if (['cancelled', 'no_show'].includes(row.status)) stateConflict('นัดนี้ปิดเป็นตรวจเสร็จไม่ได้');
  return appointments().update(row.id, { status: 'completed' })!;
}

// ---- Reads ------------------------------------------------------------------------------------------

/** A customer's missed and late-cancelled visits within the policy's window. */
export function attendanceOf(customerId: string) {
  const policy = cancellationPolicy.get();
  const since = new Date(Date.now() - policy.window_days * 86_400_000).toISOString();
  const rows = appointments().where((row) => row.customer_id === customerId && row.starts_at >= since);
  const noShows = rows.filter((row) => row.status === 'no_show').length;
  const late = rows.filter((row) => row.status === 'cancelled' && row.late_cancellation).length;
  const strikes = noShows + late;
  return {
    customer_id: customerId,
    no_shows: noShows,
    late_cancellations: late,
    strikes_in_window: strikes,
    window_days: policy.window_days,
    free_allowance: policy.free_allowance,
    fee_due_satang: strikes > policy.free_allowance ? policy.fee_satang : 0,
  };
}

/**
 * A customer's appointments. `upcoming: true` is everything from now on, soonest first; `false` is
 * the history, latest first; absent is all of them, latest first.
 */
export function appointmentsOfCustomer(customerId: string, options: { upcoming?: boolean } = {}): AppointmentRow[] {
  const now = nowIso();
  const rows = appointments().where((row) => row.customer_id === customerId);
  if (options.upcoming === true) {
    return rows
      .filter((row) => row.ends_at >= now && !['cancelled', 'no_show', 'completed'].includes(row.status))
      .sort((a, b) => a.starts_at.localeCompare(b.starts_at));
  }
  if (options.upcoming === false) {
    return rows.filter((row) => row.ends_at < now || ['cancelled', 'no_show', 'completed'].includes(row.status)).sort((a, b) => b.starts_at.localeCompare(a.starts_at));
  }
  return rows.sort((a, b) => b.starts_at.localeCompare(a.starts_at));
}

export function appointmentsOfPet(petId: string): AppointmentRow[] {
  return appointments()
    .where((row) => row.pet_id === petId)
    .sort((a, b) => b.starts_at.localeCompare(a.starts_at));
}

// ---- Waiting list -----------------------------------------------------------------------------------

/** Whether an entry is still live: `waiting` through `latest_on`, `lapsed` after. */
export const waitingStatus = (row: { latest_on: string }) => (row.latest_on >= clinicToday() ? 'waiting' : 'lapsed');

/**
 * A cancelled time is texted to the oldest three live entries it fits, as the backend's notifier
 * does. Nothing is booked for them: whoever books first through the usual pages gets it.
 */
function offerToWaitingList(freed: AppointmentRow) {
  const day = clinicDayOf(freed.starts_at);
  const entries = waitingList()
    .where(
      (row) =>
        row.service_id === freed.service_id &&
        (!row.branch_id || row.branch_id === freed.branch_id) &&
        (!row.staff_id || row.staff_id === freed.staff_id) &&
        row.earliest_on <= day &&
        row.latest_on >= day &&
        waitingStatus(row) === 'waiting',
    )
    .sort((a, b) => a.added_at.localeCompare(b.added_at))
    .slice(0, 3);
  for (const entry of entries) {
    waitingList().update(entry.id, { last_notified_at: nowIso(), notified_count: entry.notified_count + 1 });
    const pet = petOf(entry.pet_id)?.name;
    tellOwner(entry.customer_id, 'มีคิวว่าง', `มีคิวว่าง${pet ? `สำหรับน้อง${pet}` : ''} ${thaiWhen(freed.starts_at)} ${branchOf(freed.branch_id)?.name ?? ''} จองได้ที่หน้าจองนัดออนไลน์หรือโทรหาคลินิก ใครจองก่อนได้ก่อน`);
  }
}

/** For the waiting list: the default window, today plus fourteen days. */
export const defaultWindow = () => ({ earliest_on: clinicToday(), latest_on: addDays(clinicToday(), 14) });

