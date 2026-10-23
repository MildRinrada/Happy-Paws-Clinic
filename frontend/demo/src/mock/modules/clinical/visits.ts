import type { Consultation } from '@/features/consultations/types/consultation.types';
import type { Row } from '../../core/db';
import { HttpError } from '../../core/http';
import { completeAppointment, createAppointment, enterRoom, serveTicket, startAppointment } from '../appointments';
import { invoiceForVisit } from '../billing/api';
import { dispenseMedicine, raiseConsultationInvoice, type DispenseOutcome } from '../billing/visits';
import { queueTickets } from './links';

/**
 * Where a case touches the rest of the clinic: the queue and the appointment when it opens and
 * closes, the follow-up visit, the bill, and the shelf at dispense. Each goes through the owning
 * module's exported helper; this module never writes their tables.
 *
 * A case from an appointment moves it through `startAppointment` / `completeAppointment`, which
 * take the ticket along when there is one. A walk-in's case has only the ticket.
 */

const LIVE = ['waiting', 'called', 'skipped'];

/**
 * An appointment that can no longer move (cancelled, or closed by the counter meanwhile) does not
 * stop the vet's case; the appointment is left as it is.
 */
function tolerant(move: () => void, what: string) {
  try {
    move();
  } catch (error) {
    if (!(error instanceof HttpError)) throw error;
    console.warn(`[mock] clinical: ${what} skipped: ${error.message}`);
  }
}

/** The case opened: the ticket goes in_room (called first if nobody called it), the appointment in_consultation. */
export function startVisit(origin: { appointmentId?: string; ticketId?: string; staffId: string; at: string }): void {
  if (origin.appointmentId) {
    tolerant(() => startAppointment(origin.appointmentId!, origin.staffId), 'appointment -> in_consultation');
    return;
  }
  const ticket = queueTickets().get(origin.ticketId);
  if (ticket && LIVE.includes(ticket.status)) enterRoom(ticket.id, undefined, origin.staffId);
}

/** The case closed: the ticket is served and the appointment completed. */
export function finishVisit(origin: { appointmentId?: string; ticketId?: string; at: string }): void {
  if (origin.appointmentId) {
    tolerant(() => completeAppointment(origin.appointmentId!), 'appointment -> completed');
    return;
  }
  const ticket = queueTickets().get(origin.ticketId);
  if (ticket?.status === 'in_room') serveTicket(ticket.id);
}

/** "สร้างนัดติดตาม": booked as the counter would, past the notice period, with the owner told. */
export function bookFollowUp(input: { consultation: Consultation; startsAt: string; serviceId: string; staffId?: string; bookedBy: string }) {
  const book = (staffId?: string) =>
    createAppointment({
      petId: input.consultation.pet_id,
      serviceId: input.serviceId,
      startsAt: input.startsAt,
      branchId: input.consultation.branch_id,
      staffId,
      reason: 'นัดติดตามผล',
      source: 'staff',
    });
  let row;
  try {
    row = book(input.staffId);
  } catch (error) {
    // The case's signer may not take this service (the owner closing a case they opened): the
    // booking then goes to whichever vet the calendar offers, as the counter would book it.
    if (!(error instanceof HttpError) || error.extra.field !== 'staff_id' || !input.staffId) throw error;
    row = book(undefined);
  }
  return { id: row.id, starts_at: row.starts_at };
}

/**
 * "ออกบิล": the treatments, and medicine already dispensed, as one bill (`ConsultationBilling`,
 * the billing module's). Answers the bill's id and number, or nothing when there was nothing to charge.
 */
export function raiseInvoice(input: { consultation: Consultation; staffId: string; at: string }): { id: string; number: string } | undefined {
  const invoice = raiseConsultationInvoice(input.consultation, input.staffId, input.at);
  return invoice ? { id: invoice.id, number: invoice.number } : undefined;
}

/** The bill a case is on (open, paid or refunded; never a voided one), as the close answers it. */
export function invoiceOf(consultationId: string): { id: string; number: string } | undefined {
  const invoice = invoiceForVisit({ consultationId });
  return invoice ? { id: invoice.id, number: invoice.number } : undefined;
}

/**
 * "ตัดสต๊อก" and the medicine's charge at dispense (`StockDispenser` and `MedicineBilling`, the
 * billing module's). A dispense is never refused for stock: the drug is already in the owner's hand.
 */
export function dispenseFromStock(input: { prescription: Row; branchId: string; staffId: string; consultation?: Consultation }): DispenseOutcome {
  return dispenseMedicine(input);
}

export type { DispenseOutcome };
