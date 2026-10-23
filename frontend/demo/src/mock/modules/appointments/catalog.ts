import { table, type Row } from '../../core/db';
import { clinicDayOf, clinicMinutes, instant, minutesOf, weekdayOf } from './time';

/**
 * What booking reads from the clinic and customers modules: branches, opening hours, holidays,
 * services, vets, shifts and leave, pets and owners. Their rows are read here only, never written,
 * and read defensively, so a field the owner module leaves out falls back to the product's default
 * rather than breaking the calendar.
 */

export const branches = () => table('branches').where((row) => row.is_active !== false && !row.deleted_at);
export const branchOf = (id: string | null | undefined) => (id ? table('branches').get(id) : undefined);

export const services = () => table('services').all();
export const serviceOf = (id: string | null | undefined) => (id ? table('services').get(id) : undefined);

export const staffOf = (id: string | null | undefined) => (id ? table('staff_members').get(id) : undefined);
export const petOf = (id: string | null | undefined) => (id ? table('pets').get(id) : undefined);
export const customerOf = (id: string | null | undefined) => (id ? table('customers').get(id) : undefined);

export function serviceDuration(service: Row | undefined): number {
  return Number(service?.duration_minutes) || 30;
}

/** A service offered at the branch: no branch list means every branch. */
export function offeredAt(service: Row, branchId: string): boolean {
  const ids = (service.branch_ids ?? service.branches ?? []) as string[];
  return service.is_active !== false && (ids.length === 0 || ids.includes(branchId));
}

export function onlineBookable(service: Row): boolean {
  return service.online_booking_enabled !== false && service.online_booking !== false;
}

/** Whether a member works at a branch: no branch assignment reads as every branch. */
export function worksAt(member: Row, branchId: string): boolean {
  const ids = (member.branches ?? []) as string[];
  return ids.length === 0 || ids.includes(branchId);
}

export function isVet(member: Row): boolean {
  return member.staff_type === 'veterinarian' && (member.status ?? 'active') === 'active';
}

/** The vets who work at a branch, in the staff list's order. */
export function vetsAt(branchId: string): Row[] {
  return table('staff_members').where((member) => isVet(member) && worksAt(member, branchId));
}

export function vetName(staffId: string | null | undefined): string | undefined {
  return staffOf(staffId)?.display_name;
}

// ---- Opening hours and closures ---------------------------------------------------------------------

interface DayHours {
  opens?: string;
  closes?: string;
  closed: boolean;
}

/** The week the clinic sets in settings, or the product's placeholder week when nothing is stored yet. */
function weekRows(): Array<Row & { day: string }> {
  const rows = table('clinic_hours').all();
  const document = rows.find((row) => Array.isArray(row.days));
  if (document) return document.days;
  if (rows.some((row) => typeof row.day === 'string')) return rows as Array<Row & { day: string }>;
  const profile = table('clinic_profile').all()[0];
  if (Array.isArray(profile?.hours?.days)) return profile.hours.days;
  return [];
}

const FALLBACK_HOURS: Record<string, DayHours> = {
  sunday: { opens: '10:00', closes: '17:00', closed: false },
  saturday: { opens: '09:00', closes: '18:00', closed: false },
};

export function hoursOn(day: string): DayHours {
  const weekday = weekdayOf(day);
  const stored = weekRows().find((row) => row.day === weekday);
  if (stored) return { opens: stored.opens, closes: stored.closes, closed: Boolean(stored.closed) || !stored.opens };
  return FALLBACK_HOURS[weekday] ?? { opens: '09:00', closes: '20:00', closed: false };
}

/** A special closure that day at the branch (or the whole clinic). */
export function holidayOn(day: string, branchId: string): Row | undefined {
  return table('holidays').find((row) => row.date === day && (!row.branch_id || row.branch_id === branchId));
}

export function closedOn(day: string, branchId: string): string | null {
  const holiday = holidayOn(day, branchId);
  if (holiday) return `คลินิกปิด: ${holiday.name ?? 'วันหยุดพิเศษ'}`;
  if (hoursOn(day).closed) return 'คลินิกปิดทำการในวันนี้';
  return null;
}

// ---- Shifts and leave -------------------------------------------------------------------------------

export interface Span {
  staff_id: string;
  starts_at: string;
  ends_at: string;
}

function shiftRows(): Row[] {
  return table('shifts').all();
}

function timeOffRows(): Row[] {
  return [...table('time_off').all(), ...table('staff_time_off').all(), ...table('leaves').all()];
}

const iso = (value: string) => new Date(value).toISOString();

/**
 * Who works when on a clinic day at a branch. From the roster when it has anything for that day;
 * a day the roster has not reached yet reads as every vet of the branch working the opening hours,
 * which is how a clinic without a drawn-up roster books.
 */
export function shiftsOn(day: string, branchId: string): Span[] {
  const from = instant(day, '00:00');
  const to = instant(day, '23:59');
  const all = shiftRows().filter((row) => iso(row.starts_at) < to && iso(row.ends_at) > from);
  if (all.length > 0) {
    return all
      .filter((row) => row.branch_id === branchId)
      .map((row) => ({ staff_id: row.staff_id, starts_at: iso(row.starts_at), ends_at: iso(row.ends_at) }));
  }
  const hours = hoursOn(day);
  if (hours.closed || !hours.opens || !hours.closes) return [];
  return vetsAt(branchId).map((vet) => ({ staff_id: vet.id, starts_at: instant(day, hours.opens!), ends_at: instant(day, hours.closes!) }));
}

/** Leave that touches a span, for any member. */
export function timeOffBetween(from: string, to: string): Array<Span & { reason?: string }> {
  return timeOffRows()
    .filter((row) => iso(row.starts_at) < to && iso(row.ends_at) > from)
    .map((row) => ({ staff_id: row.staff_id, starts_at: iso(row.starts_at), ends_at: iso(row.ends_at), reason: row.reason }));
}

// ---- Booking settings -------------------------------------------------------------------------------

export interface BookingRules {
  slot_minutes: number;
  lead_time_minutes: number;
  max_advance_days: number;
}

/** The clinic's booking rules (slot length, notice, how far ahead), with the backend's defaults. */
export function bookingRules(): BookingRules {
  const settings: Row = table('clinic_settings').get('booking') ?? table('clinic_settings').all()[0] ?? { id: '' };
  const booking = (settings.booking ?? settings.booking_settings ?? settings) as Record<string, unknown>;
  return {
    slot_minutes: Number(booking.slot_minutes) || 30,
    // The demo keeps the notice short, so today still has times to book an hour from now.
    lead_time_minutes: booking.lead_time_minutes === undefined ? 60 : Number(booking.lead_time_minutes),
    max_advance_days: Number(booking.max_advance_days) || 60,
  };
}

/** A service's weekly online-booking windows, when it sets any. */
export function insideBookingWindow(service: Row, startsAt: string): boolean {
  const windows = (service.booking_windows ?? []) as Array<{ day: string; from: string; to: string }>;
  if (windows.length === 0) return true;
  const day = clinicDayOf(startsAt);
  const weekday = weekdayOf(day);
  const local = clinicMinutes(startsAt);
  return windows.some((window) => window.day === weekday && local >= minutesOf(window.from) && local < minutesOf(window.to));
}

/** Pets and their owner, as booking needs them. */
export function livingPetsOf(customerId: string): Row[] {
  return table('pets').where((pet) => pet.primary_customer_id === customerId && !pet.is_deceased && !pet.deleted_at);
}
