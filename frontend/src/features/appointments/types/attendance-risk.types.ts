/** `GET /staff/appointments/attendance-risk` (docs/contract-extensions.md § 33). */

export type AttendanceRiskLevel = 'low' | 'medium' | 'high';

export interface AttendanceRiskRow {
  appointment_id: string;
  branch_id: string;
  starts_at: string;
  status: 'booked' | 'confirmed';
  customer_id: string;
  customer_name: string;
  pet_id: string;
  pet_name: string;
  staff_id?: string;
  risk: AttendanceRiskLevel;
  score: number;
  /** One Thai sentence per signal that scored, worded by the backend. */
  reasons: string[];
  signals: {
    no_shows: number;
    late_cancellations: number;
    days_booked_ahead: number;
    confirmed: boolean;
  };
}

export interface AttendanceRisk {
  date: string;
  branch_id?: string;
  window_days: number;
  /** High first, then medium, then low; earliest first within each. */
  data: AttendanceRiskRow[];
}
