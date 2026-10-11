import { listAppointments } from '@/features/appointments/api/staffAppointments';
import type { Appointment, AppointmentStatus } from '@/features/appointments/types/appointment.types';
import type { AttendanceRisk } from '@/features/appointments/types/attendance-risk.types';
import { getCustomer } from '@/features/customers/api/getCustomer';
import { getPet } from '@/features/pets/api/getPet';
import type { UpcomingVisit } from '../types/today.types';
import { tolerant } from './tolerant-read';
import { UPCOMING_HOURS, UPCOMING_LIMIT } from './today-settings';

/** Still expected: booked, confirmed, or already checked in ahead of time. */
const EXPECTED: AppointmentStatus[] = ['booked', 'confirmed', 'checked_in'];

export interface UpcomingView {
  visits: UpcomingVisit[];
  /** How many more start in the window than the list shows. */
  more: number;
}

/**
 * Today's appointments starting within the next few hours, earliest first.
 *
 * The appointment carries ids only. Names come first from the day's attendance risk, which the
 * page has already read and which names every open booking. A checked-in visit is not on it, so
 * its owner and pet come from the customer record on `customer:read`, or the pet alone from the
 * pet record on `patient:read`. A name that cannot be found is left blank rather than failing the list.
 */
export async function loadUpcoming(
  branchId: string,
  today: string,
  now: Date,
  risk: Promise<AttendanceRisk | null>,
  access: { customers: boolean; patients: boolean },
): Promise<UpcomingView> {
  const until = now.getTime() + UPCOMING_HOURS * 3_600_000;
  const [page, day] = await Promise.all([listAppointments({ date: today, branchId }), risk]);

  const soon = page.data
    .filter((appointment) => EXPECTED.includes(appointment.status))
    .filter((appointment) => {
      const starts = Date.parse(appointment.starts_at);
      return starts >= now.getTime() && starts < until;
    })
    .sort((a, b) => a.starts_at.localeCompare(b.starts_at));

  const shown = soon.slice(0, UPCOMING_LIMIT);
  const named = new Map((day?.data ?? []).map((row) => [row.appointment_id, row]));

  const missing = shown.filter((appointment) => !named.has(appointment.id));
  const customerIds = access.customers ? [...new Set(missing.map((appointment) => appointment.customer_id))] : [];
  const petIds = !access.customers && access.patients ? [...new Set(missing.map((appointment) => appointment.pet_id))] : [];

  const [customers, pets] = await Promise.all([
    Promise.all(customerIds.map((id) => tolerant(() => getCustomer(id)))),
    Promise.all(petIds.map((id) => tolerant(() => getPet(id)))),
  ]);
  const customerById = new Map(customerIds.map((id, index) => [id, customers[index]]));
  const petById = new Map(petIds.map((id, index) => [id, pets[index]]));

  const visitOf = (appointment: Appointment): UpcomingVisit => {
    const row = named.get(appointment.id);
    const customer = customerById.get(appointment.customer_id);

    return {
      id: appointment.id,
      startsAt: appointment.starts_at,
      status: appointment.status,
      petName:
        row?.pet_name ??
        customer?.pets.find((pet) => pet.id === appointment.pet_id)?.name ??
        petById.get(appointment.pet_id)?.pet.name,
      customerName: row?.customer_name ?? customer?.customer.display_name,
    };
  };

  return { visits: shown.map(visitOf), more: soon.length - shown.length };
}
