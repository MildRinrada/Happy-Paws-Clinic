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

/** One measured value with the range printed next to it. */
export interface LabResult {
  id: string;
  analyte: string;
  value_numeric?: number;
  value_text?: string;
  unit?: string;
  reference_low?: number;
  reference_high?: number;
  reference_text?: string;
  flag?: LabFlag;
  recorded_at: string;
  recorded_by_staff_id: string;
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
  abnormal_count: number;
  results: LabResult[];
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
