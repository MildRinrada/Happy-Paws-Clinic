import type { Consultation } from '@/features/consultations/types/consultation.types';
import type { LabRequest } from '@/features/laboratory/types/lab.types';
import type { PetTimelineEntry } from '@/features/pets/types/pet.types';
import type { OwnerAttachment, OwnerLabResult, OwnerRecords, OwnerVisit, RecordSection } from '@/features/portal/types/records.types';
import type { PrescriptionItem } from '@/features/prescriptions/types/prescription.types';
import { fileUrl } from '../../core/files';
import { addTimelineSource, documentsOf } from '../customers/store';
import { medicalRecordRules } from './links';
import { consultations, labRequestOf, labRequests, medicalRecords, prescriptionItem, prescriptions } from './store';

/**
 * A patient's clinical file, read by other modules: the portal's records page, the pet page's
 * timeline (through the customers module) and anything else that wants a pet's visits, drugs or
 * lab work. Every helper answers the product's own wire shapes.
 */

/** Every case of a pet, newest first. */
export function consultationsOfPet(petId: string): Consultation[] {
  return consultations()
    .where((row) => row.pet_id === petId)
    .sort((a, b) => b.opened_at.localeCompare(a.opened_at));
}

/** A pet's prescriptions as `GET /staff/prescriptions?pet_id=` lists them, newest first. */
export function prescriptionsOfPet(petId: string): PrescriptionItem[] {
  const history = prescriptions().where((row) => row.pet_id === petId);
  return history.sort((a, b) => b.prescribed_at.localeCompare(a.prescribed_at)).map((row) => prescriptionItem(row, history));
}

/** A pet's lab requests as `GET /staff/lab-requests?pet_id=` lists them, newest first. */
export function labRequestsOfPet(petId: string): LabRequest[] {
  return labRequests()
    .where((row) => row.pet_id === petId)
    .sort((a, b) => b.requested_at.localeCompare(a.requested_at))
    .map(labRequestOf);
}

/** The lab work a vet has read, as the owner sees it: voided values left out (`GetOwnerVisibleRecordsHandler`). */
export function ownerLabResults(petId: string): OwnerLabResult[] {
  return labRequestsOfPet(petId)
    .filter((request) => request.status === 'reviewed')
    .map((request) => ({
      lab_request_id: request.id,
      requested_at: request.requested_at,
      reviewed_at: request.reviewed_at ?? null,
      tests: request.tests.map((test) => test.name),
      values: request.results
        .filter((value) => !value.voided)
        .map((value) => ({
          analyte: value.analyte,
          value: value.value_numeric ?? null,
          value_text: value.value_text ?? null,
          unit: value.unit ?? null,
          flag: value.flag ?? null,
        })),
    }));
}

/** One signed visit, cut down to the sections the clinic opens to owners. */
export function ownerVisit(recordId: string, visible: Set<string>): OwnerVisit | undefined {
  const record = medicalRecords().get(recordId);
  const content = record && consultations().get(record.consultation_id);
  if (!record) return undefined;
  if (!content) return { record_id: record.id, visited_at: record.signed_at, signed_at: record.signed_at };
  return {
    record_id: record.id,
    visited_at: content.opened_at,
    signed_at: record.signed_at,
    ...(visible.has('vitals')
      ? {
          vitals: content.vitals.map((reading) => ({
            recorded_at: reading.recorded_at,
            weight_kg: reading.weight_kg ?? null,
            temperature_c: reading.temperature_c ?? null,
            heart_rate_bpm: reading.heart_rate_bpm ?? null,
            respiratory_rate_bpm: reading.respiratory_rate_bpm ?? null,
          })),
        }
      : {}),
    ...(visible.has('examination')
      ? { examination: content.examination.map((finding) => ({ body_system: finding.body_system, is_normal: finding.is_normal, note: finding.note ?? null })) }
      : {}),
    ...(visible.has('diagnosis') ? { diagnoses: content.diagnoses.map((diagnosis) => diagnosis.description) } : {}),
    ...(visible.has('treatment') ? { treatments: content.treatments.map((treatment) => ({ name: treatment.name, quantity: treatment.quantity })) } : {}),
    ...(visible.has('prescription')
      ? {
          prescriptions: content.prescriptions.map((item) => ({
            drug_name: item.drug_name,
            dose: item.dose,
            frequency: item.frequency,
            duration_days: item.duration_days,
            instructions: item.instructions ?? null,
          })),
        }
      : {}),
  };
}

/**
 * `GET /portal/pets/{id}/records` in full: the signed visits (newest first, at most 100), the lab
 * results and the files, each only when the clinic's `owner_visible_sections` opens it. The
 * caller checks that the customer may read this pet's history.
 */
export function ownerRecords(petId: string): OwnerRecords {
  const sections = medicalRecordRules().owner_visible_sections as RecordSection[];
  const visible = new Set<string>(sections);
  const visits = medicalRecords()
    .where((record) => record.pet_id === petId)
    .sort((a, b) => b.signed_at.localeCompare(a.signed_at))
    .slice(0, 100)
    .map((record) => ownerVisit(record.id, visible)!)
    .filter(Boolean);
  const attachments: OwnerAttachment[] = documentsOf(petId, { ownerOnly: true }).map((document) => ({
    document_id: document.id,
    title: document.title,
    kind: document.kind,
    source: document.uploaded_by_staff_id ? 'clinic' : 'owner',
    content_type: document.content_type,
    added_at: document.created_at,
    download_url: fileUrl(document.file_id),
  }));
  return {
    pet_id: petId,
    visible_sections: [...sections].sort(),
    visits,
    lab_results: visible.has('lab_result') ? ownerLabResults(petId) : null,
    attachments: visible.has('attachments') ? attachments : null,
  };
}

// ---- The pet page's timeline ---------------------------------------------------------------------

function labSummary(request: LabRequest): string {
  const names = request.tests.map((test) => test.name).join(', ');
  if (request.status === 'pending') return `${names} · รอผล`;
  return request.abnormal_count > 0 ? `${names} · ผิดปกติ ${request.abnormal_count} ค่า` : `${names} · ผลปกติ`;
}

/** Visits, medicines and lab work for `GET /staff/pets/{id}/history`, worded as `PetTimelineReader` words them. */
export function clinicalTimeline(petId: string): PetTimelineEntry[] {
  const visits = consultationsOfPet(petId).map((visit) => ({
    kind: 'visit',
    reference_id: visit.id,
    occurred_at: visit.opened_at,
    summary: visit.diagnoses.find((diagnosis) => diagnosis.is_primary)?.description ?? 'ยังไม่มีคำวินิจฉัย',
    staff_id: visit.vet_staff_id,
    ...(visit.medical_record_id ? { medical_record_id: visit.medical_record_id } : {}),
  }));
  const medicines = prescriptions()
    .where((row) => row.pet_id === petId)
    .map((row) => ({
      kind: 'medicine',
      reference_id: row.id,
      occurred_at: row.prescribed_at,
      summary: `${row.drug_name} ${row.dose} × ${row.duration_days} วัน`,
      staff_id: row.prescribed_by_staff_id,
    }));
  const labs = labRequestsOfPet(petId).map((request) => ({
    kind: 'lab',
    reference_id: request.id,
    occurred_at: request.requested_at,
    summary: labSummary(request),
    staff_id: request.requested_by_staff_id,
  }));
  return [...visits, ...medicines, ...labs];
}

addTimelineSource({ kinds: ['visit', 'medicine', 'lab'], entries: clinicalTimeline });
