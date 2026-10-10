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
  /** Staff responses only, absent when unset (docs/contract-extensions.md § 31). */
  priority_reason?: string;
  entered_room_at?: string;
  served_at?: string;
  left_at?: string;
  left_reason?: string;
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

/** What the history view narrows a day's tickets to, sent as `status=` (docs/contract-extensions.md § 31). */
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

/** One ticket on `GET /queue/display-board`: a number and, once called, where to go and who is there. */
export interface DisplayBoardTicket {
  number: string;
  /** `skipped` arrives as `waiting`; finished tickets are not on the board. */
  status: 'waiting' | 'called' | 'in_room';
  room_id?: string;
  vet_name?: string;
  called_at?: string;
}

/**
 * `GET /queue/display-board` — what a waiting-room screen's token may read
 * (docs/contract-extensions.md § 30). No ticket, pet, owner or service id, no names but the vets'.
 */
export interface DisplayBoard {
  branch_id: string;
  branch_name: string;
  service_date: string;
  waiting_count: number;
  exam_rooms: string[];
  tickets: DisplayBoardTicket[];
}

/** A screen's state in the staff list. */
export type QueueDisplayStatus = 'pending_setup' | 'setup_expired' | 'active' | 'expired' | 'revoked';

/** `GET /staff/queue/displays` — one screen, never its token or code. */
export interface QueueDisplaySummary {
  id: string;
  name: string;
  branch_id: string;
  status: QueueDisplayStatus;
  created_by_staff_id: string;
  created_at: string;
  setup_expires_at?: string;
  claimed_at?: string;
  expires_at?: string;
  last_seen_at?: string;
  revoked_at?: string;
}

/** `POST /staff/queue/displays` — the new screen and its setup code, shown once. */
export interface CreatedQueueDisplay {
  id: string;
  name: string;
  branch_id: string;
  status: QueueDisplayStatus;
  setup_code: string;
  setup_expires_at: string;
  expires_at?: string;
  created_at: string;
}

/** What the console shows once after making a screen: the code, the setup link and its QR code. */
export interface DisplaySetup {
  display: CreatedQueueDisplay;
  setupUrl: string;
  qrDataUrl: string;
}

/** `POST /queue/displays/claim` — the token, which only this app's server ever holds. */
export interface ClaimedQueueDisplay {
  display_id: string;
  name: string;
  branch_id: string;
  token: string;
  expires_at?: string;
}
