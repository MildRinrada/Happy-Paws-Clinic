/** `GET /staff/customers/due-back` (docs/contract-extensions.md § 33). Absent keys are null. */

export type DueBackReason = 'vaccine_overdue' | 'lapsed';

export interface OverdueVaccine {
  vaccine_name: string;
  /** A clinic calendar day, `YYYY-MM-DD`. */
  due_on: string;
}

export interface DueBackPet {
  pet_id: string;
  name: string;
  species: string;
  /** Empty for `lapsed`. */
  overdue_vaccines: OverdueVaccine[];
}

export interface DueBackCustomer {
  customer_id: string;
  customer_name: string;
  reason: DueBackReason;
  pets: DueBackPet[];
  last_visit_on?: string;
  /** `lapsed` only. */
  visit_count?: number;
  /** `lapsed` only: the median gap between visits, in days. */
  usual_interval_days?: number;
  /** `lapsed` only: how long after the last visit they count as late. */
  allowed_days?: number;
  due_on: string;
  days_overdue: number;
}
