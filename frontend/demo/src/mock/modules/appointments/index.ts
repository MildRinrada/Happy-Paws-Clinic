/**
 * What the appointments module offers other modules (portal, clinical, billing, crm). Import from
 * here rather than from the files behind it; rows come back in the API's own shapes plus the
 * internal fields documented in store.ts (strip them with `appointmentDto` / `ticketDto`).
 *
 * Reading
 * - `appointmentsOfCustomer(customerId, { upcoming? })`: `true` from now on soonest first, `false`
 *   the history latest first, absent all latest first.
 * - `appointmentsOfPet(petId)`: latest first.
 * - `availability({ serviceId, branchId, date, staffId?, online? })`: `{ duration_minutes, slots }`,
 *   free starts once per free vet, from the roster less leave, bookings and live holds.
 * - `attendanceOf(customerId)`: no-shows, late cancellations and the fee due under the policy.
 * - `boardOf(branchId, date?, includeFinished?)`: tickets in call order; `liveTicketsOfCustomer`,
 *   `ticketOfAppointment`, `aheadOf(ticket)` for the portal's visit card.
 *
 * Changing (each one sends the owner's message and announces queue changes itself)
 * - `createAppointment({ petId, serviceId, startsAt, branchId, staffId?, type?, reason?, source })`
 *   checks the slot (422 `slot_unavailable` with suggested times) and texts a confirmation.
 * - `cancelAppointment(id, reason?, by)`, `confirmAppointment(id)`,
 *   `rescheduleAppointment(id, { startsAt, staffId?, by })`, `checkInAppointment(id)` (issues the
 *   ticket), `markNoShow(id)`.
 * - `startAppointment(id, staffId?)` / `completeAppointment(id)`: a visit opened or closed, with or
 *   without a queue ticket (with one, they go through `enterRoom` / `serveTicket`).
 * - Queue, for the consultation module: `enterRoom(ticketId, roomId?, staffId?)` when a visit is
 *   opened (appointment becomes `in_consultation`), `serveTicket(ticketId)` when it is closed
 *   (appointment becomes `completed`); `issueTicket`, `callTicket`, `skipTicket`, `leaveTicket`,
 *   `assignTicket`, `setPriority` as the console uses them.
 */

export {
  appointmentOrFail,
  appointmentsOfCustomer,
  appointmentsOfPet,
  attendanceOf,
  cancelAppointment,
  checkInAppointment,
  completeAppointment,
  confirmAppointment,
  createAppointment,
  markNoShow,
  rescheduleAppointment,
  startAppointment,
  tellOwner,
} from './appointments';
export { availability, conflictsFor, resolveSlot } from './availability';
export {
  aheadOf,
  announce,
  assignTicket,
  boardOf,
  callTicket,
  enterRoom,
  issueTicket,
  leaveTicket,
  liveTicketsOfCustomer,
  serveTicket,
  setPriority,
  skipTicket,
  ticketOfAppointment,
} from './queue';
export { appointmentDto, appointments, appointmentTypes, cancellationPolicy, holds, ticketDto, tickets, typeOf, waitingList } from './store';
export type { AppointmentRow, AppointmentStatus, TicketRow, TicketStatus, TicketPriority } from './store';
