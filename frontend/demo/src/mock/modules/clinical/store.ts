import type { Consultation } from '@/features/consultations/types/consultation.types';
import type { LabRequest } from '@/features/laboratory/types/lab.types';
import type { PrescriptionItem } from '@/features/prescriptions/types/prescription.types';
import type { TreatmentTemplate } from '@/features/prescriptions/types/treatment-template.types';
import { nowIso, table, type Row } from '../../core/db';

/**
 * The clinical module's tables. Each row is the shape its main read answers with, plus the few
 * internal fields noted on the type, so another module reading `table('consultations')` gets a
 * `Consultation` as `GET /staff/consultations/{id}` would.
 */

/** A case. `prescriptions` is kept in step with the `prescriptions` table by `syncPrescriptions`. */
export type ConsultationRow = Consultation & Row;

/**
 * One prescription, as `GET /staff/prescriptions` lists it, less `active` and `duplicate_of`: those
 * depend on the clock and the patient's other drugs, so `prescriptionItem()` works them out on
 * every read. Internal: `branch_id`, `customer_id`.
 */
export type PrescriptionRow = Omit<PrescriptionItem, 'active' | 'duplicate_of'> & Row & { branch_id: string; customer_id: string };

/** A lab request, as `GET /staff/lab-requests` lists it. Attachments carry an internal `file_id`. */
export type LabRequestRow = LabRequest & Row;

/**
 * A signed record, as the close answers it (`MedicalRecordDto`) but without `content` and
 * `original_content`: a closed case never changes, so both are the case itself and are read from
 * `consultations` by `recordContent()` instead of being stored three times in browser storage.
 */
export interface MedicalRecordRow extends Row {
  version: number;
  status: 'signed' | 'amended';
  consultation_id: string;
  pet_id: string;
  customer_id: string;
  branch_id: string;
  signed_at: string;
  signed_by_staff_id: string;
  content_hash: string;
  integrity_verified: boolean;
  amendments: { id: string; created_at: string; by_staff_id: string; reason: string; changes: Record<string, unknown> }[];
}

export type TemplateRow = TreatmentTemplate & Row;

export const consultations = () => table<ConsultationRow>('consultations');
export const prescriptions = () => table<PrescriptionRow>('prescriptions');
export const labRequests = () => table<LabRequestRow>('lab_requests');
export const medicalRecords = () => table<MedicalRecordRow>('medical_records');
export const treatmentTemplates = () => table<TemplateRow>('treatment_templates');

// ---- Derived values ------------------------------------------------------------------------------

const DAY_MS = 86_400_000;

export function courseEndsAt(prescribedAt: string, durationDays: number): string {
  return new Date(new Date(prescribedAt).getTime() + durationDays * DAY_MS).toISOString();
}

/** The drug without strength or form, as the backend compares them: the first word, lower case. */
export function drugKey(name: string): string {
  return name.trim().split(/\s+/)[0]?.toLowerCase() ?? '';
}

/** A prescription as the list answers it now: whether the course is running, and its overlapping twins. */
export function prescriptionItem(row: PrescriptionRow, history?: PrescriptionRow[]): PrescriptionItem {
  const now = nowIso();
  const others = history ?? prescriptions().where((other) => other.pet_id === row.pet_id);
  const key = drugKey(row.drug_name);
  const duplicate_of = others
    .filter((other) => other.id !== row.id && drugKey(other.drug_name) === key)
    .filter((other) => row.prescribed_at < other.course_ends_at && other.prescribed_at < row.course_ends_at)
    .sort((a, b) => a.prescribed_at.localeCompare(b.prescribed_at))
    .map((other) => other.id);
  const { branch_id: _branch, customer_id: _customer, ...item } = row;
  return { ...item, active: now >= row.prescribed_at && now < row.course_ends_at, duplicate_of };
}

/** Writes the case's own copy of its prescriptions, in the case's shape. */
export function syncPrescriptions(consultationId: string): ConsultationRow | undefined {
  const rows = prescriptions()
    .where((row) => row.consultation_id === consultationId)
    .sort((a, b) => a.prescribed_at.localeCompare(b.prescribed_at));
  return consultations().update(consultationId, {
    prescriptions: rows.map((row) => ({
      id: row.id,
      drug_name: row.drug_name,
      dose: row.dose,
      frequency: row.frequency,
      duration_days: row.duration_days,
      quantity: row.quantity,
      ...(row.instructions ? { instructions: row.instructions } : {}),
      ...(row.allergy_acknowledgement ? { allergy_acknowledgement: row.allergy_acknowledgement } : {}),
      prescribed_at: row.prescribed_at,
      prescribed_by_staff_id: row.prescribed_by_staff_id,
      ...(row.dispensed_at ? { dispensed_at: row.dispensed_at } : {}),
      ...(row.inventory_item_id ? { inventory_item_id: row.inventory_item_id } : {}),
    })),
  });
}

/** Values flagged low, high or abnormal, not counting voided ones. */
export function abnormalCount(request: Pick<LabRequest, 'results'>): number {
  return request.results.filter((result) => !result.voided && (result.flag === 'low' || result.flag === 'high' || result.flag === 'abnormal')).length;
}

/** A lab request as the API answers it: the internal file ids taken off its attachments. */
export function labRequestOf(row: LabRequestRow): LabRequest {
  return {
    ...row,
    abnormal_count: abnormalCount(row),
    attachments: row.attachments.map(({ file_id: _file, ...attachment }: Row) => attachment as LabRequest['attachments'][number]),
  };
}

/** A short, stable hash of a string, to stand for the record's content hash. */
export function hashOf(text: string): string {
  let a = 0x811c9dc5;
  let b = 0x01000193;
  for (let index = 0; index < text.length; index += 1) {
    const code = text.charCodeAt(index);
    a = Math.imul(a ^ code, 0x01000193) >>> 0;
    b = Math.imul(b + code, 0x5bd1e995) >>> 0;
  }
  return `sha256:${a.toString(16).padStart(8, '0')}${b.toString(16).padStart(8, '0')}`;
}
