/** One free start time with one vet. The same time repeats once per free vet. */
export interface Slot {
  starts_at: string;
  ends_at: string;
  staff_id: string;
}

/** `GET /booking/availability` — free times for one service on one clinic day. */
export interface Availability {
  branch_id: string;
  service_id: string;
  date: string;
  duration_minutes: number;
  slots: Slot[];
}

/** `POST /booking/holds` — a time kept for this owner for ten minutes while they confirm. */
export interface SlotHold {
  id: string;
  branch_id: string;
  service_id: string;
  staff_id: string;
  starts_at: string;
  ends_at: string;
  expires_at: string;
}

/**
 * An appointment type code from `GET /portal/appointment-types`. By default `regular` and
 * `follow_up`; the clinic decides which types are bookable online.
 */
export type BookableType = string;

/** One type a customer may book, as the clinic names it. `duration_minutes` replaces the service's length. */
export interface OnlineAppointmentType {
  code: string;
  name: string;
  duration_minutes?: number | null;
}

/** What the first step of the booking form collects. */
export interface BookingDetails {
  petId: string;
  branchId: string;
  serviceId: string;
  type: BookableType;
  reason: string;
}

/** The lists the form chooses from, read on the server before the page draws. */
export interface BookingOptions {
  pets: { id: string; name: string; species: string }[];
  branches: { id: string; name: string }[];
  servicesByBranch: Record<string, { id: string; name: string; duration_minutes: number; price_satang: number }[]>;
  /** The types this clinic lets customers book online. Empty when it allows none. */
  types: OnlineAppointmentType[];
  /** Staff id to name, for the vets on the public team page. */
  vets: Record<string, string>;
}

/**
 * Where the guest booking form opens when the address names a time (`?date=&time=`). `startsAt` is
 * set when that time was still free on the server; `notice` when it was not.
 */
export interface TimePreselect {
  /** The clinic day to show times for (`YYYY-MM-DD`). */
  day: string;
  startsAt?: string;
  notice?: string;
}

/** `POST /booking/guest/start` (docs/contract-extensions.md § 27): the time kept, and when the code lapses. */
export interface GuestBookingStarted {
  hold: SlotHold;
  code_expires_at: string;
}

/** A living pet the visitor already has at the clinic, shown once their number is proved. */
export interface GuestPet {
  id: string;
  name: string;
  species: string;
}

/** What `POST /booking/guest/verify` answers besides the tokens, which stay on the server. */
export interface GuestVerified {
  account_created: boolean;
  pets: GuestPet[];
  hold: SlotHold;
}

/** A pet booked for the first time, as `new_pet` on `POST /portal/appointments`. */
export interface NewPet {
  name: string;
  species: string;
}

/**
 * What the day and time step has chosen so far. `day` is empty until the visitor or the first
 * read of the week settles on one; `vet` is empty for "any vet"; `startsAt` is the time picked,
 * not yet held.
 */
export interface TimeChoice {
  day: string;
  vet: string;
  startsAt?: string;
}

/** One day's free times, or the backend's refusal for that day alone. */
export type DaySlots = { ok: true; slots: Slot[] } | { ok: false; message: string };

/** Seven days of free times for the day chips, keyed by clinic day (`YYYY-MM-DD`). */
export type WeekSlots =
  | { status: 'loading' }
  | { status: 'ready'; days: Record<string, DaySlots> }
  | { status: 'error'; message: string };

/** What a day chip says under the date. `unknown` while loading, or when that day could not be read. */
export type DayStatus = 'free' | 'full' | 'unknown';
