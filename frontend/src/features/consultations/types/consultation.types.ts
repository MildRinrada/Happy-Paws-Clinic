/** A set of vitals. Every reading is optional: an assistant may only weigh the patient. */
export interface Vitals {
  id: string;
  recorded_at: string;
  recorded_by_staff_id: string;
  weight_kg?: number;
  temperature_c?: number;
  heart_rate_bpm?: number;
  respiratory_rate_bpm?: number;
  note?: string;
}

/** One body system on the examination form. A system not listed was not examined. */
export interface Finding {
  id: string;
  body_system: string;
  is_normal: boolean;
  note?: string;
}

export interface Diagnosis {
  id: string;
  code?: string;
  description: string;
  is_primary: boolean;
  note?: string;
}

/** A billed procedure. The bill is raised from these when the case closes. */
export interface Treatment {
  id: string;
  service_id?: string;
  name: string;
  quantity: number;
  unit_price_satang: number;
  line_total_satang: number;
  performed_by_staff_id: string;
  performed_at: string;
  note?: string;
}

export interface Prescription {
  id: string;
  drug_name: string;
  dose: string;
  frequency: string;
  duration_days: number;
  quantity: number;
  instructions?: string;
  allergy_acknowledgement?: string;
  prescribed_at: string;
  prescribed_by_staff_id: string;
  dispensed_at?: string;
  /**
   * A contract extension (docs/contract-extensions.md § 20): the stock item the drug is tied to,
   * picked when written or matched by name at dispense. Absent for a typed drug not yet dispensed.
   */
  inventory_item_id?: string;
}

/** A case — `con_…`. Every write in the section answers with this, and so does the read. */
export interface Consultation {
  id: string;
  version: number;
  status: 'draft' | 'closed';
  branch_id: string;
  pet_id: string;
  customer_id: string;
  vet_staff_id: string;
  appointment_id?: string;
  queue_ticket_id?: string;
  medical_record_id?: string;
  opened_at: string;
  closed_at?: string;
  chief_complaint?: string;
  subjective?: string;
  objective?: string;
  assessment?: string;
  plan?: string;
  vitals: Vitals[];
  examination: Finding[];
  diagnoses: Diagnosis[];
  treatments: Treatment[];
  prescriptions: Prescription[];
  treatment_charges_satang: number;
}

/** The narrative half of the case, which autosaves. */
export interface ConsultationNotes {
  chief_complaint: string;
  subjective: string;
  objective: string;
  assessment: string;
  plan: string;
}

/** `POST /staff/consultations/{id}/close` — what closing produced. */
export interface CloseResult {
  consultation: Consultation;
  record: { id: string };
  follow_up?: { id: string; starts_at: string };
  charges_satang: number;
  invoice?: { id: string; number: string };
}
