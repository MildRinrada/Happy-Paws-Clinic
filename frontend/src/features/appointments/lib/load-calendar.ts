import { getOpeningHours } from '@/features/clinic/api/getOpeningHours';
import type { Weekday } from '@/features/clinic/types/clinic.types';
import { getCustomer } from '@/features/customers/api/getCustomer';
import { getPet } from '@/features/pets/api/getPet';
import { vetNames } from '@/features/portal/lib/display-names';
import { listServices } from '@/features/service-catalog/api/listServices';
import { listStaffMembers } from '@/features/staff/api/listStaffMembers';
import { optional } from '@/lib/api/api-error';
import { listAppointments } from '../api/staffAppointments';
import { appointmentCalendar } from '../content/appointment-calendar';
import type { Appointment } from '../types/appointment.types';
import type { CalendarColumn, CalendarItem, CalendarView, VetOption } from '../types/calendar.types';
import { clinicMinutes, minutesOf, weekdayIndex } from './calendar-day';

const WEEKDAYS: Weekday[] = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];

/** The grid when the clinic has not set its hours, or the hours cannot be read. */
const DEFAULT_START = 8 * 60;
const DEFAULT_END = 20 * 60;

/** A very full day is a few dozen appointments; five pages of a hundred is a runaway, not a day. */
const MAX_PAGES = 5;

async function allAppointments(day: string, branchId: string): Promise<Appointment[]> {
  const rows: Appointment[] = [];
  let cursor: string | undefined;

  for (let page = 0; page < MAX_PAGES; page += 1) {
    const result = await listAppointments({ date: day, branchId, cursor });
    rows.push(...result.data);
    cursor = result.page.next_cursor;
    if (!cursor) break;
  }

  return rows;
}

/**
 * The vets the counter can book with at this branch, with their names.
 *
 * From the staff list when the session holds `staff:read`: everyone whose type is vet and who
 * works here (no branch assignment means every branch). Without it, the public team page's vets,
 * which may leave out someone the clinic does not show on its website.
 */
export async function loadVets(branchId: string): Promise<VetOption[]> {
  const staff = await optional(listStaffMembers());

  if (staff) {
    return staff
      .filter((member) => member.staff_type === 'veterinarian')
      .filter((member) => member.branches.length === 0 || member.branches.includes(branchId))
      .map((member) => ({ id: member.id, name: member.display_name }));
  }

  return [...(await vetNames())].map(([id, name]) => ({ id, name }));
}

/**
 * One clinic day at one branch, with the names a person reads.
 *
 * The appointments are the one read the page cannot do without. Every name comes from a read of
 * its own, under its own scope, and is optional: a session that may not read the customer
 * register still sees the board, with the pet's name and no owner.
 */
export async function loadCalendar(day: string, branchId: string): Promise<CalendarView> {
  const appointments = await allAppointments(day, branchId);

  const petIds = [...new Set(appointments.map((appointment) => appointment.pet_id))];
  const customerIds = [...new Set(appointments.map((appointment) => appointment.customer_id))];

  const [pets, customers, services, vets, hours] = await Promise.all([
    Promise.all(petIds.map((id) => optional(getPet(id)))),
    Promise.all(customerIds.map((id) => optional(getCustomer(id)))),
    optional(listServices(branchId)),
    loadVets(branchId),
    getOpeningHours().catch(() => null),
  ]);

  const petById = new Map(petIds.map((id, index) => [id, pets[index]]));
  const customerById = new Map(customerIds.map((id, index) => [id, customers[index]]));
  const serviceById = new Map((services ?? []).map((service) => [service.id, service.name]));
  const vetById = new Map(vets.map((vet) => [vet.id, vet.name]));

  const items: CalendarItem[] = appointments
    .map((appointment) => {
      const pet = petById.get(appointment.pet_id);
      const customer = customerById.get(appointment.customer_id)?.customer;

      return {
        appointment,
        petName: pet?.pet.name,
        petDetail: pet ? [pet.pet.species, pet.pet.breed, pet.pet.hospital_number].filter(Boolean).join(' / ') : undefined,
        allergies: pet?.allergies.map((allergy) => allergy.substance) ?? [],
        customerName: customer?.display_name,
        customerPhone: customer?.phone,
        serviceName: serviceById.get(appointment.service_id),
        vetName: appointment.staff_id ? vetById.get(appointment.staff_id) : undefined,
      };
    })
    .sort((a, b) => a.appointment.starts_at.localeCompare(b.appointment.starts_at));

  // Every vet who works here gets a column, so a free afternoon is visible. A vet not on the list
  // (moved branch, or unnamed to this session) still gets one for the appointments they hold.
  const columns: CalendarColumn[] = vets.map((vet) => ({ staffId: vet.id, name: vet.name }));
  for (const appointment of appointments) {
    const staffId = appointment.staff_id ?? null;
    if (staffId && !columns.some((column) => column.staffId === staffId)) {
      columns.push({ staffId, name: appointmentCalendar.unknownVet });
    }
  }
  if (appointments.some((appointment) => !appointment.staff_id)) {
    columns.push({ staffId: null, name: appointmentCalendar.noVet });
  }

  const today = hours?.configured ? hours.days.find((entry) => entry.day === WEEKDAYS[weekdayIndex(day)]) : undefined;
  const closedDay = today?.closed ?? false;

  let startMinute = today?.opens ? minutesOf(today.opens) : DEFAULT_START;
  let endMinute = today?.closes ? minutesOf(today.closes) : DEFAULT_END;

  // An appointment outside opening hours (an emergency, a vet who stayed late) still has to show.
  for (const appointment of appointments) {
    startMinute = Math.min(startMinute, clinicMinutes(appointment.starts_at));
    endMinute = Math.max(endMinute, clinicMinutes(appointment.ends_at));
  }

  return {
    day,
    items,
    columns,
    startMinute: Math.floor(startMinute / 60) * 60,
    endMinute: Math.ceil(endMinute / 60) * 60,
    closedDay,
    vets,
  };
}
