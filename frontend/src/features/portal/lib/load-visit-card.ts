import { listMyAppointments } from '@/features/appointments/api/listMyAppointments';
import type { Appointment, AppointmentStatus } from '@/features/appointments/types/appointment.types';
import { authed } from '@/lib/auth/session';
import { clinicDay } from '@/lib/format/datetime';
import type { PortalQueueItem } from '../types/portal.types';
import { CHANGEABLE, canChangeOnline } from './appointment-rules';
import { serviceNames, vetNames } from './display-names';
import { loadPortalHome } from './load-portal-home';
import { visitStage, type VisitStage } from './visit-stage';

/** Everything one visit card draws, already decided. The components only render it. */
export interface VisitCardView {
  /** True when the visit is today, which is when the live parts (queue, progress) mean anything. */
  today: boolean;
  petName?: string;
  appointment?: {
    id: string;
    status: AppointmentStatus;
    startsAt: string;
    serviceName?: string;
    vetName?: string;
  };
  ticket?: PortalQueueItem;
  stage: VisitStage;
  canCheckIn: boolean;
  canReschedule: boolean;
}

/** States in which today's appointment is still the one the owner is following. */
const IN_PROGRESS: ReadonlySet<AppointmentStatus> = new Set(['booked', 'confirmed', 'checked_in', 'in_consultation']);

/** A visit already under way sorts first: called, then in the room, then waiting, then not yet arrived. */
const STAGE_ORDER: Record<VisitStage, number> = { called: 0, in_room: 1, waiting: 2, before: 3, done: 4 };

/**
 * One card per visit the owner should be following, most urgent first.
 *
 * Today, that is every appointment still in progress (even once its start time has passed, which
 * `next_appointment` alone would miss) and every walk-in ticket, each card holding its own pet's
 * queue number: tickets are matched to appointments by `appointment_id`, never by position, so an
 * owner with two pets in does not see one pet's number against the other's visit. With nothing
 * today, the single next appointment on any later day.
 *
 * Answers an empty list when there is nothing to follow.
 */
export async function loadVisitCards(): Promise<VisitCardView[]> {
  const [home, past, upcoming] = await Promise.all([
    loadPortalHome(),
    authed(() => listMyAppointments(false, 10)),
    authed(() => listMyAppointments(true, 10)),
  ]);

  const todayKey = clinicDay(new Date());
  const byId = new Map<string, Appointment>();
  for (const item of [...past, ...upcoming]) {
    if (clinicDay(item.starts_at) === todayKey && IN_PROGRESS.has(item.status)) byId.set(item.id, item);
  }
  const todays = [...byId.values()];

  const walkIns = home.queue.filter((ticket) => !ticket.appointment_id || !byId.has(ticket.appointment_id));
  const next = todays.length === 0 && walkIns.length === 0 ? (home.next_appointment ?? undefined) : undefined;

  const shown = [...todays, ...(next ? [next] : [])];
  if (shown.length === 0 && walkIns.length === 0) return [];

  const [services, vets] = await Promise.all([serviceNames(shown.map((item) => item.branch_id)), vetNames()]);
  const petName = (petId: string) => home.pets.find((pet) => pet.id === petId)?.name;

  const appointmentCards = shown.map((appointment): VisitCardView => {
    const today = clinicDay(appointment.starts_at) === todayKey;
    const ticket = home.queue.find((item) => item.appointment_id === appointment.id);

    return {
      today,
      petName: petName(appointment.pet_id),
      appointment: {
        id: appointment.id,
        status: appointment.status,
        startsAt: appointment.starts_at,
        serviceName: services.get(appointment.service_id),
        vetName: appointment.staff_id ? vets.get(appointment.staff_id) : undefined,
      },
      ticket,
      stage: visitStage(ticket?.status, appointment.status),
      canCheckIn: today && !ticket && CHANGEABLE.has(appointment.status),
      canReschedule: canChangeOnline(appointment.status, appointment.starts_at),
    };
  });

  const walkInCards = walkIns.map(
    (ticket): VisitCardView => ({
      today: true,
      petName: petName(ticket.pet_id),
      ticket,
      stage: visitStage(ticket.status, undefined),
      canCheckIn: false,
      canReschedule: false,
    }),
  );

  return [...walkInCards, ...appointmentCards].sort(
    (a, b) =>
      STAGE_ORDER[a.stage] - STAGE_ORDER[b.stage] ||
      (a.appointment?.startsAt ?? '').localeCompare(b.appointment?.startsAt ?? ''),
  );
}
