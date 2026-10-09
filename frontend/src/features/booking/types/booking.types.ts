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
