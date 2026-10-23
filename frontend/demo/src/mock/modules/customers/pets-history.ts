import type { PetTimelineEntry } from '@/features/pets/types/pet.types';
import { table, type Row } from '../../core/db';
import { timelineSources, vaccinations } from './store';

/**
 * A pet's timeline, newest first: the doses this module records, plus visits, medicines and lab
 * work from the clinical module. A module that registers a source with `addTimelineSource()`
 * answers for its kinds; for a kind nobody registered, its tables are read here in the shape of
 * the product's types (`Consultation`, `LabRequest`).
 */
export function timelineOf(petId: string): PetTimelineEntry[] {
  const entries: PetTimelineEntry[] = [];
  const covered = new Set(timelineSources.flatMap((source) => source.kinds));
  for (const source of timelineSources) entries.push(...source.entries(petId));

  for (const dose of vaccinations().where((row) => row.pet_id === petId)) {
    entries.push({
      kind: 'vaccination',
      reference_id: dose.id,
      occurred_at: dose.administered_at,
      summary: `${dose.vaccine_name} · ล็อต ${dose.lot_number}`,
      staff_id: dose.administered_by_staff_id,
    });
  }

  const visits = table('consultations').where((row) => row.pet_id === petId);
  if (!covered.has('visit')) {
    for (const visit of visits) {
      const diagnosis = (visit.diagnoses ?? []).find((entry: { is_primary?: boolean }) => entry.is_primary) ?? visit.diagnoses?.[0];
      const summary = [visit.chief_complaint, diagnosis?.description].filter(Boolean).join(' · ') || 'ตรวจรักษา';
      entries.push({
        kind: 'visit',
        reference_id: visit.id,
        occurred_at: visit.closed_at ?? visit.opened_at,
        summary,
        ...(visit.vet_staff_id ? { staff_id: visit.vet_staff_id } : {}),
        ...(visit.medical_record_id ? { medical_record_id: visit.medical_record_id } : {}),
      });
    }
  }

  if (!covered.has('medicine')) {
    const seen = new Set<string>();
    const medicines = [
      ...visits.flatMap((visit) => (visit.prescriptions ?? []).map((rx: Row) => ({ ...rx, medical_record_id: visit.medical_record_id }))),
      ...table('prescriptions').where((row) => row.pet_id === petId),
    ];
    for (const rx of medicines) {
      if (seen.has(rx.id)) continue;
      seen.add(rx.id);
      entries.push({
        kind: 'medicine',
        reference_id: rx.id,
        occurred_at: rx.prescribed_at ?? rx.created_at,
        summary: [rx.drug_name, rx.dose, rx.frequency].filter(Boolean).join(' '),
        ...(rx.prescribed_by_staff_id ? { staff_id: rx.prescribed_by_staff_id } : {}),
        ...(rx.medical_record_id ? { medical_record_id: rx.medical_record_id } : {}),
      });
    }
  }

  if (!covered.has('lab')) {
    for (const lab of table('lab_requests').where((row) => row.pet_id === petId)) {
      const tests = (lab.tests ?? []).map((test: { name?: string; code?: string }) => test.name ?? test.code).filter(Boolean);
      entries.push({
        kind: 'lab',
        reference_id: lab.id,
        occurred_at: lab.resulted_at ?? lab.requested_at,
        summary: `ส่งตรวจ ${tests.join(', ') || 'แล็บ'}`,
        ...(lab.requested_by_staff_id ? { staff_id: lab.requested_by_staff_id } : {}),
      });
    }
  }

  return entries.filter((entry) => entry.occurred_at).sort((a, b) => b.occurred_at.localeCompare(a.occurred_at));
}
