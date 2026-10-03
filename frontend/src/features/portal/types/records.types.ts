/** A section of the record the clinic may open to owners (`owner_visible_sections`). */
export type RecordSection =
  | 'vitals'
  | 'examination'
  | 'diagnosis'
  | 'treatment'
  | 'prescription'
  | 'lab_result'
  | 'attachments';

/**
 * One signed visit, cut down to what the clinic shows owners. A section the clinic has not opened
 * is absent, not empty: "nothing prescribed" and "not shown here" are different things.
 */
export interface OwnerVisit {
  record_id: string;
  visited_at: string;
  signed_at: string;
  vitals?: {
    recorded_at: string;
    weight_kg?: number | null;
    temperature_c?: number | null;
    heart_rate_bpm?: number | null;
    respiratory_rate_bpm?: number | null;
  }[];
  examination?: { body_system: string; is_normal: boolean; note?: string | null }[];
  diagnoses?: string[];
  treatments?: { name: string; quantity: number }[];
  prescriptions?: { drug_name: string; dose: string; frequency: string; duration_days: number; instructions?: string | null }[];
}

export interface OwnerLabResult {
  lab_request_id: string;
  requested_at: string;
  reviewed_at?: string | null;
  tests: string[];
  values: { analyte: string; value?: number | null; value_text?: string | null; unit?: string | null; flag?: string | null }[];
}

export interface OwnerAttachment {
  document_id: string;
  title: string;
  kind: string;
  source: string;
  content_type: string;
  added_at: string;
  download_url?: string | null;
}

/** `GET /portal/pets/{id}/records` — "เฉพาะส่วนที่คลินิกตั้งค่าให้เจ้าของเห็น". */
export interface OwnerRecords {
  pet_id: string;
  visible_sections: RecordSection[];
  visits: OwnerVisit[];
  lab_results?: OwnerLabResult[] | null;
  attachments?: OwnerAttachment[] | null;
}
