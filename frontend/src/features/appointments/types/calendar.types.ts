import type { Appointment } from './appointment.types';

/** One appointment with the names a person reads. Any name the session may not read is absent. */
export interface CalendarItem {
  appointment: Appointment;
  petName?: string;
  petDetail?: string;
  /** What the pet is allergic to, for the red warning. Empty when nothing is recorded, or unreadable. */
  allergies: string[];
  customerName?: string;
  customerPhone?: string;
  serviceName?: string;
  vetName?: string;
}

/** A column on the day grid: one vet, or the appointments no vet has been given yet. */
export interface CalendarColumn {
  staffId: string | null;
  name: string;
}

/** Everything the calendar page draws for one clinic day. */
export interface CalendarView {
  day: string;
  items: CalendarItem[];
  columns: CalendarColumn[];
  /** The grid's first and last minute of the day, on the clinic's wall clock. */
  startMinute: number;
  endMinute: number;
  /** The clinic is closed that weekday, by its own opening hours. */
  closedDay: boolean;
  /** The vets at this branch, for moving an appointment to another one. */
  vets: VetOption[];
}

/** A vet the counter can pick when booking or moving an appointment. */
export interface VetOption {
  id: string;
  name: string;
}
