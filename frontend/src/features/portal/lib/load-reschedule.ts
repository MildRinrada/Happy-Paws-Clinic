import { notFound } from 'next/navigation';
import { optional } from '@/lib/api/api-error';
import { authed } from '@/lib/auth/session';
import { clinicDay } from '@/lib/format/datetime';
import { getMyAppointment } from '../api/getMyAppointment';
import { portalAppointments } from '../content/portal-appointments';
import { canChangeOnline } from './appointment-rules';
import { serviceNames, vetNames } from './display-names';
import { loadPortalHome } from './load-portal-home';

/** The appointment being moved, with its names looked up, and what the slot picker needs. */
export interface RescheduleView {
  id: string;
  version: number;
  serviceId: string;
  branchId: string;
  staffId?: string;
  startsAt: string;
  /** The clinic day it is on now, where the slot picker opens. */
  day: string;
  title: string;
  vet: string;
  canChange: boolean;
}

/**
 * One of the owner's appointments for the reschedule page. Someone else's id, or one that does
 * not exist, is a 404 from the backend and the page's own "not found".
 */
export async function loadReschedule(appointmentId: string): Promise<{ appointment: RescheduleView; vets: Record<string, string> }> {
  const [item, home, vets] = await Promise.all([
    authed(() => optional(getMyAppointment(appointmentId))),
    loadPortalHome(),
    vetNames(),
  ]);

  if (!item) notFound();

  const services = await serviceNames([item.branch_id]);
  const pet = home.pets.find((candidate) => candidate.id === item.pet_id)?.name;
  const text = portalAppointments;

  return {
    appointment: {
      id: item.id,
      version: item.version,
      serviceId: item.service_id,
      branchId: item.branch_id,
      staffId: item.staff_id ?? undefined,
      startsAt: item.starts_at,
      day: clinicDay(item.starts_at),
      title: [services.get(item.service_id) ?? text.serviceFallback, pet && `${text.petPrefix}${pet}`].filter(Boolean).join(' '),
      vet: (item.staff_id && vets.get(item.staff_id)) || text.vetFallback,
      canChange: canChangeOnline(item.status, item.starts_at),
    },
    vets: Object.fromEntries(vets),
  };
}
