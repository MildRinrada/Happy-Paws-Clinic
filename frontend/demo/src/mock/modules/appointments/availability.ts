import { fail, invalid, notFound } from '../../core/http';
import { nowIso, type Row } from '../../core/db';
import {
  bookingRules,
  branchOf,
  closedOn,
  holidayOn,
  hoursOn,
  insideBookingWindow,
  isVet,
  offeredAt,
  onlineBookable,
  serviceDuration,
  serviceOf,
  shiftsOn,
  staffOf,
  timeOffBetween,
  vetName,
  type Span,
} from './catalog';
import { appointments, holds, typeOf, type AppointmentRow } from './store';
import { addMinutes, clinicDayOf, clockOf, instant, overlaps } from './time';

/**
 * Free times, worked out the way the backend's slot resolver does: a vet's shift on the day, less
 * leave, less the appointments and live holds already in it, in steps of the clinic's slot length.
 * The public booking page, the portal's and the counter's new-appointment form all read from here,
 * so a time booked in one is gone from the others.
 */

/** Statuses that keep a vet's time taken. */
const OCCUPYING = new Set(['draft', 'booked', 'confirmed', 'checked_in', 'in_consultation', 'completed']);

export interface Slot {
  starts_at: string;
  ends_at: string;
  staff_id: string;
}

export interface SlotQuery {
  serviceId: string;
  branchId: string;
  /** A clinic day, `YYYY-MM-DD`. */
  date: string;
  /** Only this vet's times. */
  staffId?: string;
  /** The customer paths: online services only, inside their windows, with the clinic's notice. */
  online?: boolean;
  /** An appointment type's own length, replacing the service's. */
  durationMinutes?: number | null;
  /** An appointment type's own notice, replacing the clinic's. */
  leadTimeMinutes?: number | null;
  /** The appointment being moved, so it does not collide with itself. */
  excludeAppointmentId?: string;
  /** The hold being redeemed. */
  excludeHoldId?: string;
  /** For the seed: times in the past count as free too. */
  ignoreClock?: boolean;
}

/** Appointments and live holds that take a vet's time on a clinic day at a branch. */
export function busySpans(branchId: string, date: string, exclude: { appointmentId?: string; holdId?: string } = {}): Span[] {
  const now = nowIso();
  const taken: Span[] = appointments()
    .where(
      (row) =>
        row.branch_id === branchId &&
        OCCUPYING.has(row.status) &&
        Boolean(row.staff_id) &&
        clinicDayOf(row.starts_at) === date &&
        row.id !== exclude.appointmentId,
    )
    .map((row) => ({ staff_id: row.staff_id!, starts_at: row.starts_at, ends_at: row.ends_at }));
  const held: Span[] = holds()
    .where((row) => row.branch_id === branchId && !row.consumed && row.expires_at > now && clinicDayOf(row.starts_at) === date && row.id !== exclude.holdId)
    .map((row) => ({ staff_id: row.staff_id, starts_at: row.starts_at, ends_at: row.ends_at }));
  return [...taken, ...held];
}

/** The vets who may do a service: its own list, or any vet on shift. */
function eligible(service: Row, staffId: string): boolean {
  const ids = (service.eligible_staff_ids ?? []) as string[];
  const member = staffOf(staffId);
  if (!member || !isVet(member)) return false;
  return ids.length === 0 || ids.includes(staffId);
}

/** Every free start for one service on one day, once per free vet, earliest first. */
export function availability(query: SlotQuery): { duration_minutes: number; slots: Slot[] } {
  const service = serviceOf(query.serviceId);
  if (!service) notFound('บริการ');
  if (!branchOf(query.branchId)) notFound('สาขา');
  const duration = query.durationMinutes || serviceDuration(service);
  if (!offeredAt(service, query.branchId) || (query.online && !onlineBookable(service))) return { duration_minutes: duration, slots: [] };
  if (closedOn(query.date, query.branchId)) return { duration_minutes: duration, slots: [] };

  const rules = bookingRules();
  const now = Date.now();
  const lead = query.leadTimeMinutes ?? rules.lead_time_minutes;
  // Staff may book a pet that is already standing at the counter; not into an hour already gone.
  const earliest = query.online ? now + lead * 60_000 : now - 15 * 60_000;
  const latest = query.online ? now + rules.max_advance_days * 86_400_000 : Number.POSITIVE_INFINITY;

  const busy = busySpans(query.branchId, query.date, { appointmentId: query.excludeAppointmentId, holdId: query.excludeHoldId });
  const leave = timeOffBetween(instant(query.date, '00:00'), instant(query.date, '23:59'));
  const hours = hoursOn(query.date);
  const closes = hours.closes ? instant(query.date, hours.closes) : undefined;

  const slots: Slot[] = [];
  for (const shift of shiftsOn(query.date, query.branchId)) {
    if (query.staffId && shift.staff_id !== query.staffId) continue;
    if (!eligible(service, shift.staff_id)) continue;
    for (let start = shift.starts_at; addMinutes(start, duration) <= shift.ends_at; start = addMinutes(start, rules.slot_minutes)) {
      const end = addMinutes(start, duration);
      const at = Date.parse(start);
      if (!query.ignoreClock && (at < earliest || at > latest)) continue;
      if (closes && end > closes && query.online) continue;
      if (query.online && !insideBookingWindow(service, start)) continue;
      if (leave.some((span) => span.staff_id === shift.staff_id && overlaps(start, end, span.starts_at, span.ends_at))) continue;
      if (busy.some((span) => span.staff_id === shift.staff_id && overlaps(start, end, span.starts_at, span.ends_at))) continue;
      slots.push({ starts_at: start, ends_at: end, staff_id: shift.staff_id });
    }
  }

  slots.sort((a, b) => a.starts_at.localeCompare(b.starts_at) || a.staff_id.localeCompare(b.staff_id));
  return { duration_minutes: duration, slots: slots.filter((slot, index) => index === 0 || slot.starts_at !== slots[index - 1]!.starts_at || slot.staff_id !== slots[index - 1]!.staff_id) };
}

/** Up to six free `HH:mm` times that day, for a refusal. */
function suggested(query: SlotQuery): string[] {
  const times = [...new Set(availability(query).slots.map((slot) => clockOf(slot.starts_at)))];
  return times.slice(0, 6);
}

export function slotUnavailable(message: string, query?: SlotQuery): never {
  const list = query ? suggested(query) : [];
  fail(422, 'slot_unavailable', message, { field: 'starts_at', details: { suggested: list } });
}

export interface ResolveRequest extends Omit<SlotQuery, 'date'> {
  startsAt: string;
  /** For "any vet": how to pick among the free ones. */
  species?: string;
}

/**
 * Checks one requested start and answers the vet and the end, or refuses with `slot_unavailable`
 * and the day's free times, as `BookingSlotResolver` does.
 */
export function resolveSlot(request: ResolveRequest): Slot {
  const startsAt = new Date(request.startsAt).toISOString();
  if (Number.isNaN(Date.parse(startsAt))) invalid('starts_at', 'เวลาไม่ถูกต้อง');
  const service = serviceOf(request.serviceId);
  if (!service) invalid('service_id', 'ไม่พบบริการนี้');
  if (!branchOf(request.branchId)) invalid('branch_id', 'ไม่พบสาขานี้');
  if (!offeredAt(service, request.branchId)) invalid('service_id', 'บริการนี้ไม่เปิดให้บริการที่สาขานี้');
  if (request.online && !onlineBookable(service)) invalid('service_id', 'บริการนี้ไม่เปิดให้จองออนไลน์');
  if (request.staffId && !eligible(service, request.staffId)) invalid('staff_id', 'ผู้ให้บริการนี้ไม่สามารถให้บริการที่เลือกได้');

  const date = clinicDayOf(startsAt);
  const query: SlotQuery = { ...request, date };
  const holiday = holidayOn(date, request.branchId);
  if (holiday) fail(422, 'slot_unavailable', `คลินิกปิดทำการในวันดังกล่าว (${holiday.name ?? 'วันหยุด'})`, { field: 'starts_at', details: { suggested: [] } });

  const free = availability({ ...query, staffId: request.staffId }).slots.filter((slot) => slot.starts_at === startsAt);
  if (free.length > 0) {
    // "Any vet": the one with the fewest bookings that day, so the day spreads out.
    if (free.length === 1 || request.staffId) return free[0]!;
    const load = (staffId: string) => busySpans(request.branchId, date).filter((span) => span.staff_id === staffId).length;
    return [...free].sort((a, b) => load(a.staff_id) - load(b.staff_id))[0]!;
  }

  const duration = request.durationMinutes || serviceDuration(service);
  const end = addMinutes(startsAt, duration);
  const covered = shiftsOn(date, request.branchId).some(
    (shift) => (!request.staffId || shift.staff_id === request.staffId) && shift.starts_at <= startsAt && shift.ends_at >= end,
  );
  if (!covered) slotUnavailable('ไม่มีตารางเวรรองรับช่วงเวลานี้', query);
  if (request.online && Date.parse(startsAt) < Date.now() + (request.leadTimeMinutes ?? bookingRules().lead_time_minutes) * 60_000) {
    slotUnavailable('เวลานี้กระชั้นชิดเกินไปสำหรับการจองออนไลน์ กรุณาเลือกเวลาอื่นหรือโทรหาคลินิก', query);
  }
  slotUnavailable('ช่วงเวลานี้ถูกจองแล้ว', query);
}

// ---- Conflict reports (GET /staff/appointments/conflicts) -----------------------------------------------

export interface Conflict {
  kind: string;
  message: string;
  starts_at: string;
  ends_at: string;
  appointment_id?: string | null;
  staff_id?: string | null;
  conflicting_appointment_id?: string | null;
}

/** Everything wrong with one visit, without refusing anything. */
export function conflictsFor(visit: {
  startsAt: string;
  endsAt: string;
  branchId: string;
  staffId?: string | null;
  petId?: string | null;
  appointmentId?: string;
}): Conflict[] {
  const startsAt = new Date(visit.startsAt).toISOString();
  const endsAt = new Date(visit.endsAt).toISOString();
  const date = clinicDayOf(startsAt);
  const base = { starts_at: startsAt, ends_at: endsAt, appointment_id: visit.appointmentId ?? null };
  const found: Conflict[] = [];

  const closed = closedOn(date, visit.branchId);
  const hours = hoursOn(date);
  if (closed) {
    found.push({ ...base, kind: 'clinic_closed', message: closed });
  } else if (hours.opens && hours.closes && (startsAt < instant(date, hours.opens) || endsAt > instant(date, hours.closes))) {
    found.push({ ...base, kind: 'clinic_closed', message: `นอกเวลาทำการของคลินิก (${hours.opens}-${hours.closes} น.)` });
  }

  if (visit.staffId) {
    const name = vetName(visit.staffId) ?? 'หมอ';
    const onShift = shiftsOn(date, visit.branchId).some((shift) => shift.staff_id === visit.staffId && shift.starts_at <= startsAt && shift.ends_at >= endsAt);
    if (!onShift && !closed) found.push({ ...base, staff_id: visit.staffId, kind: 'staff_off_shift', message: `${name}ไม่มีเวรในช่วงเวลานี้` });
    for (const leave of timeOffBetween(startsAt, endsAt).filter((span) => span.staff_id === visit.staffId)) {
      found.push({ ...base, staff_id: visit.staffId, kind: 'staff_on_time_off', message: `หมอลาในช่วงนี้${leave.reason ? `: ${leave.reason}` : ''}` });
    }
    for (const other of openAppointmentsOverlapping(startsAt, endsAt, visit.appointmentId).filter((row) => row.staff_id === visit.staffId)) {
      found.push({
        ...base,
        staff_id: visit.staffId,
        kind: 'staff_double_booked',
        message: `${name}มีนัดอื่นเวลา ${clockOf(other.starts_at)}-${clockOf(other.ends_at)} น.`,
        conflicting_appointment_id: other.id,
      });
    }
  }

  if (visit.petId) {
    for (const other of openAppointmentsOverlapping(startsAt, endsAt, visit.appointmentId).filter((row) => row.pet_id === visit.petId)) {
      found.push({
        ...base,
        kind: 'pet_double_booked',
        message: `สัตว์เลี้ยงตัวนี้มีนัดอื่นเวลา ${clockOf(other.starts_at)}-${clockOf(other.ends_at)} น.`,
        conflicting_appointment_id: other.id,
      });
    }
  }
  return found;
}

function openAppointmentsOverlapping(startsAt: string, endsAt: string, except?: string): AppointmentRow[] {
  return appointments().where(
    (row) => row.id !== except && ['booked', 'confirmed', 'checked_in', 'in_consultation'].includes(row.status) && overlaps(startsAt, endsAt, row.starts_at, row.ends_at),
  );
}

/** The length of a visit of this service and type. */
export function durationFor(serviceId: string, type?: string | null): number {
  return typeOf(type)?.duration_minutes || serviceDuration(serviceOf(serviceId));
}
