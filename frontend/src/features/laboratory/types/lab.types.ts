/**
 * The lab module's wire shapes, as the backend's `LabRequestDto` writes them. A member that is null
 * is left out of the JSON, so every nullable field is optional here.
 */

/** `pending` waits on the lab, `resulted` waits on a vet, `reviewed` has been read. */
export type LabStatus = 'pending' | 'resulted' | 'reviewed';

export type LabDestination = 'in_house' | 'external';

export type LabPriority = 'routine' | 'urgent';

/** Worked out from the value and its range; for a text value, the person entering it says. */
export type LabFlag = 'normal' | 'low' | 'high' | 'abnormal';

export interface LabTest {
  name: string;
  code?: string;
}

/** The part of a value that a correction replaces: what was measured, how much, and against what. */
export interface LabValue {
  analyte: string;
  value_numeric?: number;
  value_text?: string;
  unit?: string;
  reference_low?: number;
  reference_high?: number;
  reference_text?: string;
  flag?: LabFlag;
}

/** A value a correction replaced, with who replaced it, when and why (docs/contract-extensions.md § 52). */
export interface LabResultRevision extends LabValue {
  replaced_at: string;
  replaced_by_staff_id: string;
  reason: string;
}

/**
 * One measured value with the range printed next to it. A corrected value says so and keeps every
 * value it replaced; a voided one stays listed with `voided` set (§ 52).
 */
export interface LabResult extends LabValue {
  id: string;
  recorded_at: string;
  recorded_by_staff_id: string;
  amended: boolean;
  amended_at?: string;
  amended_by_staff_id?: string;
  amend_reason?: string;
  /** Oldest first. Empty for a value as first entered. */
  previous_values: LabResultRevision[];
  voided: boolean;
  voided_at?: string;
  voided_by_staff_id?: string;
  void_reason?: string;
}

/** A file from the lab on a request (§ 54). No address: opening one asks for a short-lived link. */
export interface LabAttachment {
  id: string;
  file_name: string;
  content_type: string;
  size_bytes?: number;
  attached_at: string;
  attached_by_staff_id: string;
}

/** The media types a lab file may be (§ 54). */
export type LabFileType = 'application/pdf' | 'image/jpeg' | 'image/png' | 'image/webp';

/** `POST /staff/lab-requests/{id}/attachments/uploads`: where to send the file, and the value that names it. */
export interface LabFileUpload {
  /** Send this as `file` to `POST .../attachments` once the file is in. */
  value: string;
  upload: { url: string; method: string; headers: Record<string, string>; expires_at: string };
}

/** `GET /staff/lab-requests/{id}/attachments/{attachmentId}/link`: good for five minutes. */
export interface LabFileLink {
  url: string;
  expires_at: string;
}

/** `POST .../results/{resultId}/amend`: the whole value as it should have been, and why. */
export interface LabCorrection extends Omit<LabResultInput, 'analyte'> {
  analyte?: string;
  reason: string;
}

/** A lab request (`lab_…`) and every result it has so far. */
export interface LabRequest {
  id: string;
  version: number;
  pet_id: string;
  branch_id: string;
  consultation_id?: string;
  destination: LabDestination;
  external_lab_name?: string;
  priority: LabPriority;
  tests: LabTest[];
  clinical_notes?: string;
  requested_by_staff_id: string;
  requested_at: string;
  status: LabStatus;
  resulted_at?: string;
  reviewed_at?: string;
  reviewed_by_staff_id?: string;
  /** What the vet wrote when reading the results (§ 53). Cleared when the request goes back to `resulted`. */
  review_note?: string;
  /** Not counting voided values. */
  abnormal_count: number;
  results: LabResult[];
  attachments: LabAttachment[];
}

/** `POST /staff/lab-requests`. The branch comes from the consultation when one is named. */
export interface NewLabRequest {
  pet_id: string;
  destination: LabDestination;
  tests: LabTest[];
  consultation_id?: string;
  branch_id?: string;
  external_lab_name?: string;
  priority?: LabPriority;
  clinical_notes?: string;
}

/** One value for `POST /staff/lab-requests/{id}/results`. A number goes in `value`, anything else in `value_text`. */
export interface LabResultInput {
  analyte: string;
  value?: number;
  value_text?: string;
  unit?: string;
  reference_low?: number;
  reference_high?: number;
  reference_text?: string;
  abnormal?: boolean;
}

export interface LabListQuery {
  status?: LabStatus;
  petId?: string;
  /** A staff id, or `me` for the caller's own requests. */
  requestedBy?: string;
  branchId?: string;
  cursor?: string;
  limit?: number;
}
