'use server';

import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import {
  addDiagnosis,
  addPrescription,
  addTreatment,
  closeConsultation,
  openConsultation,
  recordExamination,
  recordVitals,
  updateNotes,
} from '../api/consultations';
import { consultationPaths } from '../lib/consultation-paths';

/**
 * The consulting room's writes. What a case may hold, when it is complete, and who may sign it
 * are the backend's rules; these check the shape, make one call, and re-read the page.
 */

const id = z.string().min(1).max(64);
const text = (max: number) => z.string().trim().max(max);

function refresh(consultationId: string) {
  revalidatePath(consultationPaths.detail(consultationId));
}

const origin = z
  .object({ appointmentId: id.optional(), ticketId: id.optional(), idempotencyKey: z.uuid() })
  .refine((value) => value.appointmentId || value.ticketId);

/** Takes the patient into the room and opens the case page. Opening again finds the same case. */
export async function openConsultationAction(input: z.infer<typeof origin>): Promise<ActionResult> {
  const parsed = origin.parse(input);
  const result = await runAction(() =>
    openConsultation({ appointment_id: parsed.appointmentId, queue_ticket_id: parsed.ticketId }, parsed.idempotencyKey),
  );

  if (!result.ok) return result;

  revalidatePath('/queue');
  revalidatePath('/appointments');
  redirect(consultationPaths.detail(result.data.id));
}

const notes = z.object({
  consultationId: id,
  changes: z
    .object({
      chief_complaint: text(2000),
      subjective: text(8000),
      objective: text(8000),
      assessment: text(8000),
      plan: text(8000),
    })
    .partial(),
});

/** The autosave. Answers with the saved version so the form knows it is in step. */
export async function saveNotesAction(input: z.infer<typeof notes>) {
  const parsed = notes.parse(input);
  const result = await runAction(() => updateNotes(parsed.consultationId, parsed.changes));

  return result.ok ? { ok: true as const, data: { version: result.data.version } } : result;
}

const optionalNumber = z.number().nonnegative().optional();

const vitals = z.object({
  consultationId: id,
  weightKg: optionalNumber,
  temperatureC: optionalNumber,
  heartRateBpm: z.number().int().nonnegative().optional(),
  respiratoryRateBpm: z.number().int().nonnegative().optional(),
  note: text(500).optional(),
});

export async function recordVitalsAction(input: z.infer<typeof vitals>) {
  const parsed = vitals.parse(input);
  const result = await runAction(() =>
    recordVitals(parsed.consultationId, {
      weight_kg: parsed.weightKg,
      temperature_c: parsed.temperatureC,
      heart_rate_bpm: parsed.heartRateBpm,
      respiratory_rate_bpm: parsed.respiratoryRateBpm,
      note: parsed.note || undefined,
    }),
  );

  refresh(parsed.consultationId);
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const examination = z.object({
  consultationId: id,
  findings: z.array(z.object({ bodySystem: text(80).min(1), isNormal: z.boolean(), note: text(500).optional() })).max(40),
});

export async function recordExaminationAction(input: z.infer<typeof examination>) {
  const parsed = examination.parse(input);
  const result = await runAction(() =>
    recordExamination(
      parsed.consultationId,
      parsed.findings.map((finding) => ({ body_system: finding.bodySystem, is_normal: finding.isNormal, note: finding.note || undefined })),
    ),
  );

  refresh(parsed.consultationId);
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const diagnosis = z.object({
  consultationId: id,
  description: text(500).min(1),
  isPrimary: z.boolean(),
  code: text(40).optional(),
  note: text(500).optional(),
});

export async function addDiagnosisAction(input: z.infer<typeof diagnosis>) {
  const parsed = diagnosis.parse(input);
  const result = await runAction(() =>
    addDiagnosis(parsed.consultationId, {
      description: parsed.description,
      is_primary: parsed.isPrimary,
      code: parsed.code || undefined,
      note: parsed.note || undefined,
    }),
  );

  refresh(parsed.consultationId);
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const treatment = z.object({
  consultationId: id,
  name: text(200).min(1),
  quantity: z.number().int().positive(),
  unitPriceSatang: z.number().int().nonnegative(),
  note: text(500).optional(),
  idempotencyKey: z.uuid(),
});

export async function addTreatmentAction(input: z.infer<typeof treatment>) {
  const parsed = treatment.parse(input);
  const result = await runAction(() =>
    addTreatment(
      parsed.consultationId,
      { name: parsed.name, quantity: parsed.quantity, unit_price_satang: parsed.unitPriceSatang, note: parsed.note || undefined },
      parsed.idempotencyKey,
    ),
  );

  refresh(parsed.consultationId);
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const prescription = z.object({
  consultationId: id,
  drugName: text(200).min(1),
  dose: text(100).min(1),
  frequency: text(100).min(1),
  durationDays: z.number().int().positive(),
  quantity: z.number().int().positive(),
  instructions: text(500).optional(),
  acknowledgeAllergy: text(500).optional(),
  idempotencyKey: z.uuid(),
});

/** A refused allergy check comes back as a result; its details say which allergy and whether it can be overridden. */
export async function addPrescriptionAction(input: z.infer<typeof prescription>) {
  const parsed = prescription.parse(input);
  const result = await runAction(() =>
    addPrescription(
      parsed.consultationId,
      {
        drug_name: parsed.drugName,
        dose: parsed.dose,
        frequency: parsed.frequency,
        duration_days: parsed.durationDays,
        quantity: parsed.quantity,
        instructions: parsed.instructions || undefined,
        acknowledge_allergy: parsed.acknowledgeAllergy || undefined,
      },
      parsed.idempotencyKey,
    ),
  );

  refresh(parsed.consultationId);
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const close = z.object({
  consultationId: id,
  followUpAt: z.iso.datetime({ offset: true }).optional(),
  /** Needed when the visit was a walk-in: there is no booked service to repeat. */
  followUpServiceId: id.optional(),
  idempotencyKey: z.uuid(),
});

/** Signs and closes. Answers with what it produced: the bill's number and the follow-up, if any. */
export async function closeConsultationAction(input: z.infer<typeof close>) {
  const parsed = close.parse(input);
  const result = await runAction(() =>
    closeConsultation(
      parsed.consultationId,
      parsed.followUpAt ? { starts_at: parsed.followUpAt, service_id: parsed.followUpServiceId } : undefined,
      parsed.idempotencyKey,
    ),
  );

  refresh(parsed.consultationId);
  revalidatePath('/queue');
  revalidatePath('/billing');

  return result.ok
    ? {
        ok: true as const,
        data: { invoice: result.data.invoice, followUpAt: result.data.follow_up?.starts_at },
      }
    : result;
}
