import { listMyAppointments } from '@/features/appointments/api/listMyAppointments';
import type { Appointment, AppointmentStatus } from '@/features/appointments/types/appointment.types';
import { authed } from '@/lib/auth/session';
import { portalAppointments } from '../content/portal-appointments';
import { serviceNames, vetNames } from './display-names';
import { loadPortalHome } from './load-portal-home';

/** One line of the appointments page, with every name already looked up. */
export interface AppointmentRowView {
  id: string;
  startsAt: string;
  status: AppointmentStatus;
  title: string;
  vet: string;
}

/** How many of each list the page shows. Older history is for the clinic to look up. */
const PAGE_SIZE = 20;

/** The owner's upcoming appointments, soonest first, and their past ones, latest first. */
export async function loadAppointments(): Promise<{ upcoming: AppointmentRowView[]; history: AppointmentRowView[] }> {
  const [home, upcoming, history] = await Promise.all([
    loadPortalHome(),
    authed(() => listMyAppointments(true, PAGE_SIZE)),
    authed(() => listMyAppointments(false, PAGE_SIZE)),
  ]);

  const all = [...upcoming, ...history];
  const [services, vets] = await Promise.all([serviceNames(all.map((item) => item.branch_id)), vetNames()]);
  const pets = new Map(home.pets.map((pet) => [pet.id, pet.name]));
  const text = portalAppointments;

  const toRow = (item: Appointment): AppointmentRowView => {
    const pet = pets.get(item.pet_id);

    return {
      id: item.id,
      startsAt: item.starts_at,
      status: item.status,
      title: [services.get(item.service_id) ?? text.serviceFallback, pet && `${text.petPrefix}${pet}`]
        .filter(Boolean)
        .join(' '),
      vet: (item.staff_id && vets.get(item.staff_id)) || text.vetFallback,
    };
  };

  return { upcoming: upcoming.map(toRow), history: history.map(toRow) };
}
