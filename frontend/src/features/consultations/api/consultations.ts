import { staffApi } from '@/lib/api/server-client';
import type { CloseResult, Consultation, ConsultationNotes } from '../types/consultation.types';

/**
 * The consulting room's calls. Scopes differ per part, as the contract draws them: `consult:read`
 * to see the case, `vitals:write` for vitals alone, `consult:write` for the vet's own entries,
 * `rx:write` to prescribe and `record:sign` to close.
 */

const path = (id: string, part?: string) => `/staff/consultations/${encodeURIComponent(id)}${part ? `/${part}` : ''}`;

/** `GET /staff/consultations/{id}` — a contract extension (docs/contract-extensions.md § 14). */
export function getConsultation(id: string): Promise<Consultation> {
  return staffApi().get<Consultation>(path(id));
}

/**
 * `POST /staff/consultations` — from a queue ticket or an appointment. Opening the same visit again
 * returns the case already open, so a second press finds it rather than failing.
 */
export function openConsultation(
  origin: { appointment_id?: string; queue_ticket_id?: string },
  idempotencyKey: string,
): Promise<Consultation> {
  return staffApi().post<Consultation>('/staff/consultations', { body: origin, idempotencyKey });
}

/** `PATCH /staff/consultations/{id}` — the autosave. Sends only what changed. */
export function updateNotes(id: string, notes: Partial<ConsultationNotes>): Promise<Consultation> {
  return staffApi().patch<Consultation>(path(id), { body: notes });
}

export function recordVitals(
  id: string,
  vitals: { weight_kg?: number; temperature_c?: number; heart_rate_bpm?: number; respiratory_rate_bpm?: number; note?: string },
): Promise<Consultation> {
  return staffApi().post<Consultation>(path(id, 'vitals'), { body: vitals });
}

/** The whole examination form. A second post replaces the first. */
export function recordExamination(
  id: string,
  findings: { body_system: string; is_normal: boolean; note?: string }[],
): Promise<Consultation> {
  return staffApi().post<Consultation>(path(id, 'examination'), { body: { findings } });
}

export function addDiagnosis(
  id: string,
  diagnosis: { description: string; is_primary: boolean; code?: string; note?: string },
): Promise<Consultation> {
  return staffApi().post<Consultation>(path(id, 'diagnoses'), { body: diagnosis });
}

export function addTreatment(
  id: string,
  treatment: { name: string; quantity: number; unit_price_satang: number; service_id?: string; note?: string },
  idempotencyKey: string,
): Promise<Consultation> {
  return staffApi().post<Consultation>(path(id, 'treatments'), { body: treatment, idempotencyKey });
}

/** A drug the patient is allergic to answers `409 allergy_conflict` until acknowledged. */
export function addPrescription(
  id: string,
  prescription: {
    drug_name: string;
    dose: string;
    frequency: string;
    duration_days: number;
    quantity: number;
    instructions?: string;
    acknowledge_allergy?: string;
  },
  idempotencyKey: string,
): Promise<Consultation> {
  return staffApi().post<Consultation>(path(id, 'prescriptions'), { body: prescription, idempotencyKey });
}

/** Signs the record, raises the bill and books the follow-up, in one go. */
export function closeConsultation(
  id: string,
  followUp: { starts_at: string; service_id?: string } | undefined,
  idempotencyKey: string,
): Promise<CloseResult> {
  return staffApi().post<CloseResult>(path(id, 'close'), {
    body: followUp ? { follow_up: followUp } : {},
    idempotencyKey,
  });
}
