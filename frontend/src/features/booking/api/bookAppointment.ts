import type { Appointment } from '@/features/appointments/types/appointment.types';
import { staffApi } from '@/lib/api/server-client';
import type { BookableType, NewPet, SlotHold } from '../types/booking.types';

export interface BookAppointmentInput {
  /** A pet the owner has. Exactly one of this and `newPet`. */
  petId?: string;
  /** A pet the clinic has not met yet, registered with the booking (docs/contract-extensions.md § 27). */
  newPet?: NewPet;
  hold: Pick<SlotHold, 'id' | 'service_id' | 'branch_id' | 'staff_id' | 'starts_at'>;
  type: BookableType;
  reason?: string;
  idempotencyKey: string;
}

/**
 * `POST /portal/appointments` — turns a hold into an appointment. The service and time are sent
 * again from the hold (the endpoint requires them) and the hold is consumed. Creates an
 * appointment, so it carries an idempotency key.
 */
export function bookAppointment({ petId, newPet, hold, type, reason, idempotencyKey }: BookAppointmentInput): Promise<Appointment> {
  return staffApi().post<Appointment>('/portal/appointments', {
    idempotencyKey,
    body: {
      pet_id: petId,
      new_pet: newPet,
      service_id: hold.service_id,
      branch_id: hold.branch_id,
      staff_id: hold.staff_id,
      starts_at: hold.starts_at,
      hold_id: hold.id,
      type,
      reason: reason || undefined,
    },
  });
}
