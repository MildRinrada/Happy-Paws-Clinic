/**
 * `GET /staff/stats/demand-forecast` (docs/contract-extensions.md § 33). Absent keys are null:
 * `load_per_vet` is absent in an hour with no vet on shift, `high_load_per_vet` when the branch's
 * history is too thin to draw a line.
 */

export interface DemandHour {
  /** 0 to 23, clinic time. */
  hour: number;
  starts_at: string;
  booked_appointments: number;
  expected_walk_ins: number;
  expected_total: number;
  vets_on_shift: number;
  load_per_vet?: number;
  high_load: boolean;
}

export interface DemandDay {
  /** A clinic calendar day, `YYYY-MM-DD`. */
  date: string;
  booked_appointments: number;
  expected_walk_ins: number;
  expected_total: number;
  high_load_hours: number;
  hours: DemandHour[];
}

export interface DemandForecast {
  branch_id: string;
  from: string;
  to: string;
  history_weeks: number;
  high_load_per_vet?: number;
  days: DemandDay[];
}
