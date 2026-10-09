/** A patient as the registry lists them — `GET /staff/pets`. */
export interface PetSummary {
  id: string;
  version: number;
  hospital_number: string;
  name: string;
  species: string;
  breed?: string;
  microchip_number?: string;
  primary_customer_id: string;
  is_deceased: boolean;
  deceased_at?: string;
  nickname?: string;
  diet?: string;
  behaviour?: string;
}

export type AllergySeverity = 'mild' | 'moderate' | 'severe';

export interface PetAllergy {
  id: string;
  substance: string;
  severity: AllergySeverity | string;
  reaction?: string;
  recorded_at?: string;
}

/** Who may bring the pet and read its history. Revoked links stay, with `revoked_at`. */
export interface PetCarer {
  id: string;
  customer_id: string;
  is_primary: boolean;
  may_view_history: boolean;
  revoked_at?: string;
}

/** One patient in full — `GET /staff/pets/{id}`, "สายพันธุ์ น้ำหนักล่าสุด แพ้ยา ไมโครชิป". */
export interface PetDetail {
  pet: PetSummary;
  latest_weight?: { weight_kg: number; recorded_at: string };
  allergies: PetAllergy[];
  carers?: PetCarer[];
}

/** `POST /staff/pets` — the HN is issued by the server. */
export interface NewPet {
  name: string;
  species: string;
  primary_customer_id: string;
  breed?: string;
  microchip_number?: string;
}

export type TimelineKind = 'visit' | 'medicine' | 'vaccination' | 'lab';

/** One event on `GET /staff/pets/{id}/history`, newest first. */
export interface PetTimelineEntry {
  kind: TimelineKind | string;
  reference_id: string;
  occurred_at: string;
  summary: string;
  staff_id?: string;
  medical_record_id?: string;
}

export interface VaccinationDose {
  id: string;
  vaccine_name: string;
  manufacturer?: string;
  lot_number: string;
  vial_expires_on: string;
  administered_at: string;
  administered_by_staff_id: string;
  next_due_on?: string;
  note?: string;
}

export interface VaccinationDue {
  vaccine_name: string;
  next_due_on: string;
  status: 'overdue' | 'due_soon' | 'upcoming';
  last_dose_id: string;
  last_given_at: string;
}

/** `GET /staff/pets/{id}/vaccinations` — doses given, and the next of each. */
export interface PetVaccinations {
  pet_id: string;
  doses: VaccinationDose[];
  due: VaccinationDue[];
}

/** `GET /staff/pets/{id}/weights` — oldest first, as a graph reads. */
export interface WeightSeries {
  pet_id: string;
  unit: string;
  points: { recorded_at: string; weight_kg: number; consultation_id: string }[];
}

/** `POST /staff/pets/{id}/vaccinations`. Give `next_due_on` or `valid_for_days`, not both; neither means a year. */
export interface NewVaccination {
  vaccine_name: string;
  lot_number: string;
  /** `YYYY-MM-DD`. */
  vial_expires_on: string;
  administered_at?: string;
  next_due_on?: string;
  /** `0` means no further dose. */
  valid_for_days?: number;
  manufacturer?: string;
  note?: string;
}

/** `POST /staff/pets/{id}/treatment-plans`. The sessions here are `sessions` every `interval_days`. */
export interface NewTreatmentPlan {
  service_id: string;
  first_starts_at: string;
  title: string;
  sessions: number;
  interval_days: number;
  staff_id?: string;
  branch_id?: string;
  notes?: string;
  book: boolean;
}

/** What `POST /staff/pets/{id}/treatment-plans` answers. A session with no appointment is planned, not booked. */
export interface TreatmentPlan {
  id: string;
  pet_id: string;
  title: string;
  service_id: string;
  notes?: string;
  series_id?: string;
  created_at: string;
  sessions: { number: number; planned_at: string; appointment_id?: string }[];
}

export type PetDocumentKind = 'photo' | 'lab_result' | 'referral' | 'imaging' | 'other';

/** `POST /staff/pets/{id}/documents` describes the file to come; the bytes go to the URL in the answer. */
export interface NewPetDocument {
  file_name: string;
  content_type: string;
  size_bytes: number;
  kind: PetDocumentKind;
  title?: string;
  visible_to_owner: boolean;
}

/** The document as recorded, and the signed URL to `PUT` its file to (15 minutes). */
export interface PetDocumentUpload {
  document: { id: string; title: string; file_name: string };
  upload: { url: string; method: 'PUT'; headers: Record<string, string>; expires_at: string };
}

export type GeneratedDocumentType = 'vaccination_book' | 'health_certificate' | 'referral_letter';

/** `POST /staff/documents/generate`. A referral needs `refer_to` and `reason`. */
export interface DocumentRequest {
  type: GeneratedDocumentType;
  pet_id: string;
  refer_to?: string;
  reason?: string;
  remarks?: string;
}

/** `POST /staff/pets/{id}/deceased`. No `occurred_at` means now; the note goes to the audit trail. */
export interface MarkDeceasedInput {
  occurred_at?: string;
  note?: string;
}

/** What closing the record did: every future visit it cancelled. */
export interface PetDeceasedResult {
  pet_id: string;
  deceased_at: string;
  cancelled_appointment_ids: string[];
}
