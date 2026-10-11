import { listMyAppointments } from '@/features/appointments/api/listMyAppointments';
import type { Appointment, AppointmentStatus } from '@/features/appointments/types/appointment.types';
import { authed } from '@/lib/auth/session';
import { serviceNames, vetNames } from './display-names';

/** A visit already made, as "มาตรวจล่าสุด" lists it. */
export interface PastVisitView {
  id: string;
  startsAt: string;
  serviceName?: string;
  vetName?: string;
}

/** The pet's next appointment, for the "นัดครั้งถัดไป" tile. */
export interface NextVisitView {
  id: string;
  startsAt: string;
  serviceName?: string;
}

/** How many past visits the card lists. The whole history is on the records page. */
export const RECENT_VISITS = 3;

/**
 * The API's largest page. `GET /portal/appointments` cannot be narrowed to one pet, so the page
 * reads the account's latest 100 each way and keeps this pet's. An account whose other pets fill
 * those 100 may show fewer visits here than it has; the records page still has them all.
 */
const READ_LIMIT = 100;

/** A visit that happened: the pet was seen. Cancelled and missed ones are not visits. */
const SEEN: ReadonlySet<AppointmentStatus> = new Set(['completed']);

/** Still going ahead: not cancelled, not missed, not finished. */
const AHEAD: ReadonlySet<AppointmentStatus> = new Set(['booked', 'confirmed', 'checked_in', 'in_consultation']);

/**
 * A pet's last few visits, newest first, and its next appointment, from the account's own
 * appointments (`GET /portal/appointments`), with the service and the vet named.
 *
 * Appointments rather than the visit records: they carry the service and the vet the design shows,
 * they exist for a co-carer who may not see records, and they do not depend on which record
 * sections the clinic opens to owners.
 */
export async function loadPetVisits(petId: string): Promise<{ recent: PastVisitView[]; next?: NextVisitView }> {
  const [history, upcoming] = await Promise.all([
    authed(() => listMyAppointments(false, READ_LIMIT)),
    authed(() => listMyAppointments(true, READ_LIMIT)),
  ]);

  const recent = history
    .filter((item) => item.pet_id === petId && SEEN.has(item.status))
    .sort((a, b) => Date.parse(b.starts_at) - Date.parse(a.starts_at))
    .slice(0, RECENT_VISITS);
  const next = upcoming
    .filter((item) => item.pet_id === petId && AHEAD.has(item.status))
    .sort((a, b) => Date.parse(a.starts_at) - Date.parse(b.starts_at))[0];

  const named: Appointment[] = [...recent, ...(next ? [next] : [])];
  if (named.length === 0) return { recent: [] };

  const [services, vets] = await Promise.all([serviceNames(named.map((item) => item.branch_id)), vetNames()]);

  return {
    recent: recent.map((item) => ({
      id: item.id,
      startsAt: item.starts_at,
      serviceName: services.get(item.service_id),
      vetName: item.staff_id ? vets.get(item.staff_id) : undefined,
    })),
    next: next && { id: next.id, startsAt: next.starts_at, serviceName: services.get(next.service_id) },
  };
}
