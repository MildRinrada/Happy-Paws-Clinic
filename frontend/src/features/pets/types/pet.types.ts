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
