import type { Appointment } from '@/features/appointments/types/appointment.types';

export type PortalTicketStatus = 'waiting' | 'skipped' | 'called' | 'in_room' | 'served' | 'left';

/** A ticket on today's board, as its owner follows it. */
export interface PortalQueueItem {
  ticket_id: string;
  number: string;
  status: PortalTicketStatus;
  /** Tickets still waiting to be called before this one. */
  ahead: number;
  /** Whose ticket this is. */
  pet_id: string;
  /** The appointment it was checked in from; absent for a walk-in. */
  appointment_id?: string | null;
}

/** A pet as its owner sees it — `GET /portal/pets`. */
export interface MyPet {
  id: string;
  version: number;
  name: string;
  nickname?: string | null;
  /** Free text in Thai, as the clinic recorded it: "สุนัข", "แมว". */
  species: string;
  breed?: string | null;
  hospital_number: string;
  microchip_number?: string | null;
  photo_url?: string | null;
  diet?: string | null;
  behaviour?: string | null;
  is_deceased: boolean;
  latest_weight?: { weight_kg: number; recorded_at: string } | null;
  allergies: { substance: string; severity: string }[];
  /** Whether `GET /portal/pets/{id}/records` will open for this owner. */
  may_view_history: boolean;
  /** Recorded by staff, read-only here (docs/contract-extensions.md § 28). */
  sex?: 'male' | 'female' | 'unknown';
  /** `YYYY-MM-DD`. */
  birth_date?: string | null;
  birth_date_estimated?: boolean;
  neutered?: boolean | null;
}

/** One message in the owner's inbox. */
export interface InboxItem {
  id: string;
  template: string;
  title: string;
  body: string;
  sent_at: string;
  read_at?: string | null;
}

/** A vaccine due or overdue for one of the owner's pets. */
export interface VaccinationDue {
  pet_id: string;
  pet_name: string;
  vaccine_name: string;
  next_due_on: string;
  /** `upcoming` ones are left out of the home page by the backend. */
  status: 'overdue' | 'due_soon';
}

/** `GET /portal/home` — "สรุปหน้าแรก: สัตว์ นัดถัดไป คิว แจ้งเตือน". */
export interface PortalHome {
  customer_id: string;
  display_name: string;
  pets: MyPet[];
  next_appointment: Appointment | null;
  queue: PortalQueueItem[];
  unread_notifications: number;
  latest_notifications: InboxItem[];
  vaccinations_due: VaccinationDue[];
}
