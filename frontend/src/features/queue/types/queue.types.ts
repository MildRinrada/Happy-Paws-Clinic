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

/** A vet the counter can put on a ticket: `POST /staff/queue/{id}/assign` with `staff_id`. */
export interface VetOption {
  id: string;
  name: string;
}

export interface QueueView {
  board: QueueBoard;
  tickets: TicketView[];
  /**
   * The vets working at this branch, for the assign control. Empty when this session cannot read
   * the staff list (`staff:read`), in which case the control is not offered.
   */
  vets: VetOption[];
}

/** What the history view narrows a day's tickets to. The backend has no status filter. */
export type HistoryFilter = 'finished' | 'served' | 'left' | 'all';

/** One exam room on the waiting-room screen: the numbers inside it now, and the vet there. */
export interface DisplayRoom {
  room: string;
  vetName?: string;
  inRoom: string[];
}

/**
 * Everything the waiting-room screen shows, and nothing more: ticket numbers, rooms and vet names.
 * No pet, owner or service ever reaches this shape, so none can reach a public screen.
 */
export interface DisplayView {
  serviceDate: string;
  waitingCount: number;
  /** Called tickets, the latest call first, each with where to go. */
  calls: { number: string; room?: string; vetName?: string }[];
  rooms: DisplayRoom[];
  /** The next tickets to be called, in the backend's call order. */
  next: string[];
}
