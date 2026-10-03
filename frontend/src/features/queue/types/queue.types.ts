/** The contract's queue state machine: waiting → called → in_room → served, with skipped and left. */
export type TicketStatus = 'waiting' | 'called' | 'in_room' | 'served' | 'skipped' | 'left';

/** Three levels, not five — "now, soon, or in turn". A contract extension; see docs. */
export type TicketPriority = 'normal' | 'urgent' | 'emergency';

/** A queue ticket as the API returns it — `tkt_A014`. */
export interface QueueTicket {
  id: string;
  version: number;
  number: string;
  branch_id: string;
  service_date: string;
  status: TicketStatus;
  priority: TicketPriority;
  pet_id: string;
  customer_id: string;
  service_id: string;
  appointment_id?: string;
  staff_id?: string;
  room_id?: string;
  skip_count: number;
  issued_at: string;
  called_at?: string;
}

/** `GET /staff/queue` — today's board for one branch, already in the order tickets will be called. */
export interface QueueBoard {
  branch_id: string;
  service_date: string;
  room_id?: string;
  waiting_count: number;
  tickets: QueueTicket[];
}

/**
 * A ticket with the names a person reads — what the board draws.
 *
 * The API's ticket carries ids. The names are looked up beside it, each under its own scope, and
 * any of them may be missing: a session without `patient:read` still sees the queue, with the
 * patient shown as an id.
 */
export interface TicketView {
  ticket: QueueTicket;
  petName?: string;
  petDetail?: string;
  hasAllergy: boolean;
  serviceName?: string;
  vetName?: string;
}

export interface QueueView {
  board: QueueBoard;
  tickets: TicketView[];
}
