import type { CloseResult, Consultation } from '@/features/consultations/types/consultation.types';
import { nowIso, uuid } from '../../core/db';
import { checkVersion, created, fail, invalid, notFound, requireStaff, route, type Ctx } from '../../core/http';
import { send } from '../../core/outbox';
import {
  allergiesOf,
  allergyMatches,
  appointments,
  customers,
  medicalRecordRules,
  medicines,
  petOf,
  queueTickets,
  speciesKind,
} from './links';
import { consultations, courseEndsAt, hashOf, medicalRecords, prescriptions, syncPrescriptions, treatmentTemplates, type ConsultationRow } from './store';
import { bookFollowUp, finishVisit, invoiceOf, raiseInvoice, startVisit } from './visits';

/**
 * The consulting room: `POST /staff/consultations` and every part of a case under
 * `/staff/consultations/{id}`. Every write answers the whole case, as the backend's do.
 */

export function recordLocked(recordId: string): never {
  fail(423, 'record_locked', 'เวชระเบียนถูกลงนามแล้ว ต้องแก้ไขผ่าน amendment', { details: { record_id: recordId } });
}

function canSeeBranch(ctx: Ctx, branchId: string): boolean {
  const branches = ctx.auth?.branches ?? [];
  return branches.length === 0 || branches.includes(branchId);
}

/** The case, if this session may see its branch; a case in another branch is not found. */
function caseFor(ctx: Ctx, id: string): ConsultationRow {
  const row = consultations().get(id);
  if (!row || !canSeeBranch(ctx, row.branch_id)) notFound('เคสการตรวจ');
  return row;
}

function openCase(ctx: Ctx, id: string): ConsultationRow {
  const row = caseFor(ctx, id);
  if (row.status !== 'draft') recordLocked(row.medical_record_id ?? row.id);
  return row;
}

function answer(id: string): Consultation {
  return consultations().get(id)!;
}

const trimmed = (value: unknown) => (typeof value === 'string' && value.trim() ? value.trim() : undefined);

// ---- Opening -------------------------------------------------------------------------------------

route('POST', '/staff/consultations', (ctx) => {
  const auth = requireStaff(ctx, 'consult:write');
  const appointmentId = trimmed(ctx.body?.appointment_id);
  const ticketId = trimmed(ctx.body?.queue_ticket_id);
  if (!appointmentId && !ticketId) invalid('appointment_id', 'ต้องระบุนัดหรือบัตรคิวอย่างใดอย่างหนึ่ง');

  let ticket = ticketId ? queueTickets().get(ticketId) : undefined;
  if (ticketId && !ticket) notFound('บัตรคิว');
  const appointment = appointments().get(appointmentId ?? ticket?.appointment_id);
  if ((appointmentId ?? ticket?.appointment_id) && !appointment) notFound('นัดหมาย');
  ticket ??= appointment
    ? queueTickets().find((row) => row.appointment_id === appointment.id && !['served', 'left', 'skipped'].includes(row.status))
    : undefined;
  if (ticket && appointment && ticket.appointment_id !== appointment.id) invalid('queue_ticket_id', 'บัตรคิวนี้ไม่ใช่ของนัดที่ระบุ');

  const branchId: string = appointment?.branch_id ?? ticket!.branch_id;
  if (!canSeeBranch(ctx, branchId)) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่มีสิทธิ์ในสาขานี้');

  // Pressing again finds the case already open for this visit, as the backend does.
  const existing = consultations().find(
    (row) => Boolean((appointment && row.appointment_id === appointment.id) || (ticket && row.queue_ticket_id === ticket.id)),
  );
  if (existing) return existing;

  const now = nowIso();
  const consultation = consultations().insert({
    id: uuid(),
    version: 1,
    status: 'draft',
    branch_id: branchId,
    pet_id: appointment?.pet_id ?? ticket!.pet_id,
    customer_id: appointment?.customer_id ?? ticket!.customer_id,
    vet_staff_id: auth.staffId,
    ...(appointment ? { appointment_id: appointment.id } : {}),
    ...(ticket ? { queue_ticket_id: ticket.id } : {}),
    opened_at: now,
    ...(trimmed(ctx.body?.chief_complaint) ?? trimmed(appointment?.reason) ? { chief_complaint: trimmed(ctx.body?.chief_complaint) ?? trimmed(appointment?.reason) } : {}),
    vitals: [],
    examination: [],
    diagnoses: [],
    treatments: [],
    prescriptions: [],
    treatment_charges_satang: 0,
  });

  // The appointment goes in_consultation and the ticket in_room, with the case or not at all.
  startVisit({ appointmentId: appointment?.id, ticketId: ticket?.id, staffId: auth.staffId, at: now });
  return created(consultation);
});

route('GET', '/staff/consultations/:id', (ctx) => {
  requireStaff(ctx, 'consult:read');
  const row = caseFor(ctx, ctx.params.id!);
  // A closed case names its bill, as the close answered it; looked up fresh, since the bill is billing's.
  const invoice = row.status === 'closed' ? invoiceOf(row.id) : undefined;
  return invoice ? { ...row, invoice } : row;
});

// ---- The vet's notes and findings ----------------------------------------------------------------

const NOTE_FIELDS = ['chief_complaint', 'subjective', 'objective', 'assessment', 'plan'] as const;

route('PATCH', '/staff/consultations/:id', (ctx) => {
  requireStaff(ctx, 'consult:write');
  const row = openCase(ctx, ctx.params.id!);
  checkVersion(ctx, row);
  const next: Record<string, unknown> = { ...row };
  for (const field of NOTE_FIELDS) {
    const value = ctx.body?.[field];
    if (typeof value !== 'string') continue;
    // A blank field is cleared, as the backend's `Trimmed` does.
    if (value.trim()) next[field] = value.trim();
    else delete next[field];
  }
  next.version = row.version + 1;
  return consultations().put(next as ConsultationRow);
});

route('POST', '/staff/consultations/:id/vitals', (ctx) => {
  const auth = requireStaff(ctx, 'vitals:write');
  const row = openCase(ctx, ctx.params.id!);
  const body = ctx.body ?? {};
  const weight = typeof body.weight_kg === 'number' ? body.weight_kg : undefined;
  const temperature = typeof body.temperature_c === 'number' ? body.temperature_c : undefined;
  if (weight !== undefined && (weight <= 0 || weight > 500)) fail(409, 'invalid_vitals', 'น้ำหนักไม่อยู่ในช่วงที่รับได้');
  if (temperature !== undefined && (temperature < 20 || temperature > 46)) fail(409, 'invalid_vitals', 'อุณหภูมิไม่อยู่ในช่วงที่รับได้');
  const reading = {
    id: uuid(),
    recorded_at: nowIso(),
    recorded_by_staff_id: auth.staffId,
    ...(weight !== undefined ? { weight_kg: weight } : {}),
    ...(temperature !== undefined ? { temperature_c: temperature } : {}),
    ...(typeof body.heart_rate_bpm === 'number' ? { heart_rate_bpm: body.heart_rate_bpm } : {}),
    ...(typeof body.respiratory_rate_bpm === 'number' ? { respiratory_rate_bpm: body.respiratory_rate_bpm } : {}),
    ...(trimmed(body.note) ? { note: trimmed(body.note) } : {}),
  };
  return consultations().update(row.id, { vitals: [...row.vitals, reading] });
});

route('POST', '/staff/consultations/:id/examination', (ctx) => {
  requireStaff(ctx, 'consult:write');
  const row = openCase(ctx, ctx.params.id!);
  const findings = Array.isArray(ctx.body?.findings) ? (ctx.body.findings as Record<string, unknown>[]) : [];
  if (findings.length === 0) fail(409, 'invalid_examination', 'ต้องมีผลตรวจอย่างน้อยหนึ่งระบบ');
  const systems = findings.map((finding) => String(finding.body_system ?? '').trim().toLowerCase());
  if (systems.some((system) => !system)) invalid('findings', 'ต้องระบุระบบที่ตรวจ');
  if (new Set(systems).size !== systems.length) fail(409, 'invalid_examination', 'ระบบที่ตรวจซ้ำกัน');
  return consultations().update(row.id, {
    examination: findings.map((finding) => ({
      id: uuid(),
      body_system: String(finding.body_system).trim(),
      is_normal: Boolean(finding.is_normal),
      ...(trimmed(finding.note) ? { note: trimmed(finding.note) } : {}),
    })),
  });
});

route('POST', '/staff/consultations/:id/diagnoses', (ctx) => {
  requireStaff(ctx, 'consult:write');
  const row = openCase(ctx, ctx.params.id!);
  const description = trimmed(ctx.body?.description);
  if (!description) invalid('description', 'กรุณาระบุคำวินิจฉัย');
  // The first diagnosis is primary whatever the caller said; a new primary demotes the old one.
  const primary = Boolean(ctx.body?.is_primary) || row.diagnoses.length === 0;
  const diagnoses = row.diagnoses.map((diagnosis) => (primary ? { ...diagnosis, is_primary: false } : diagnosis));
  diagnoses.push({
    id: uuid(),
    description,
    is_primary: primary,
    ...(trimmed(ctx.body?.code) ? { code: trimmed(ctx.body?.code) } : {}),
    ...(trimmed(ctx.body?.note) ? { note: trimmed(ctx.body?.note) } : {}),
  });
  return consultations().update(row.id, { diagnoses });
});

route('POST', '/staff/consultations/:id/treatments', (ctx) => {
  const auth = requireStaff(ctx, 'consult:write');
  const row = openCase(ctx, ctx.params.id!);
  const name = trimmed(ctx.body?.name);
  const quantity = Number(ctx.body?.quantity);
  const unit = Number(ctx.body?.unit_price_satang);
  if (!name) invalid('name', 'กรุณาระบุชื่อหัตถการ');
  if (!Number.isInteger(quantity) || quantity <= 0) invalid('quantity', 'จำนวนต้องมากกว่า 0');
  if (!Number.isInteger(unit) || unit < 0) invalid('unit_price_satang', 'ราคาไม่ถูกต้อง');
  const treatment = {
    id: uuid(),
    ...(trimmed(ctx.body?.service_id) ? { service_id: trimmed(ctx.body?.service_id) } : {}),
    name,
    quantity,
    unit_price_satang: unit,
    line_total_satang: quantity * unit,
    performed_by_staff_id: auth.staffId,
    performed_at: nowIso(),
    ...(trimmed(ctx.body?.note) ? { note: trimmed(ctx.body?.note) } : {}),
  };
  const treatments = [...row.treatments, treatment];
  return consultations().update(row.id, {
    treatments,
    treatment_charges_satang: treatments.reduce((sum, line) => sum + line.line_total_satang, 0),
  });
});

route('POST', '/staff/consultations/:id/prescriptions', (ctx) => {
  const auth = requireStaff(ctx, 'rx:write');
  const row = openCase(ctx, ctx.params.id!);
  const body = ctx.body ?? {};

  const itemId = trimmed(body.inventory_item_id);
  const item = itemId ? medicines().find((candidate) => candidate.id === itemId) : undefined;
  if (itemId && !item) notFound('รายการยา');
  const drugName = trimmed(body.drug_name) ?? item?.name;
  if (!drugName) invalid('drug_name', 'กรุณาระบุชื่อยา');
  const dose = trimmed(body.dose);
  const frequency = trimmed(body.frequency);
  const days = Number(body.duration_days);
  const quantity = Number(body.quantity);
  if (!dose) invalid('dose', 'กรุณาระบุขนาดยา');
  if (!frequency) invalid('frequency', 'กรุณาระบุวิธีใช้');
  if (!Number.isInteger(days) || days <= 0) invalid('duration_days', 'จำนวนวันต้องมากกว่า 0');
  if (!Number.isInteger(quantity) || quantity <= 0) invalid('quantity', 'จำนวนต้องมากกว่า 0');

  // "ตรวจแพ้ยา": refused until acknowledged; a severe allergy cannot be overridden here.
  const names = [drugName, item?.name].filter((name): name is string => Boolean(name));
  const conflicts = allergiesOf(row.pet_id).filter((allergy) => names.some((name) => allergyMatches(allergy.substance, name)));
  const acknowledgement = trimmed(body.acknowledge_allergy);
  if (conflicts.length > 0) {
    const severe = conflicts.some((allergy) => allergy.severity === 'severe');
    if (severe || !acknowledgement) {
      fail(409, 'allergy_conflict', `คนไข้มีประวัติแพ้ ${conflicts.map((allergy) => allergy.substance).join(', ')}`, {
        field: 'drug_name',
        details: { drug_name: drugName, allergies: conflicts.map((allergy) => allergy.substance), overridable: !severe },
        message_en: severe
          ? 'The patient has a recorded severe allergy to this drug. It cannot be overridden here.'
          : 'The patient has a recorded allergy to this drug. Resend with acknowledge_allergy to proceed.',
      });
    }
  }

  const prescribedAt = nowIso();
  prescriptions().insert({
    id: uuid(),
    version: 1,
    pet_id: row.pet_id,
    consultation_id: row.id,
    branch_id: row.branch_id,
    customer_id: row.customer_id,
    drug_name: drugName,
    dose,
    frequency,
    duration_days: days,
    quantity,
    ...(trimmed(body.instructions) ? { instructions: trimmed(body.instructions) } : {}),
    ...(conflicts.length > 0 ? { allergy_acknowledgement: acknowledgement } : {}),
    prescribed_at: prescribedAt,
    prescribed_by_staff_id: auth.staffId,
    course_ends_at: courseEndsAt(prescribedAt, days),
    ...(item ? { inventory_item_id: item.id } : {}),
  });
  return syncPrescriptions(row.id);
});

// ---- Closing -------------------------------------------------------------------------------------

/** The case as the record keeps it: what the close signed. */
export function recordContent(consultationId: string): Consultation | undefined {
  return consultations().get(consultationId);
}

route('POST', '/staff/consultations/:id/close', (ctx) => {
  const auth = requireStaff(ctx, 'record:sign');
  const row = openCase(ctx, ctx.params.id!);
  checkVersion(ctx, row);

  const rules = medicalRecordRules();
  if (rules.signer_must_be_case_vet && row.vet_staff_id !== auth.staffId) {
    fail(403, 'insufficient_scope', 'คลินิกกำหนดให้สัตวแพทย์เจ้าของเคสเป็นผู้ลงนามเท่านั้น');
  }
  const present: Record<string, boolean> = {
    chief_complaint: Boolean(row.chief_complaint),
    subjective: Boolean(row.subjective),
    objective: Boolean(row.objective),
    assessment: Boolean(row.assessment),
    plan: Boolean(row.plan),
    vitals: row.vitals.length > 0,
    examination: row.examination.length > 0,
  };
  const missing = rules.required_fields.filter((field) => present[field] === false);
  if (missing.length > 0) {
    fail(409, 'incomplete_consultation', 'ยังกรอกข้อมูลที่คลินิกกำหนดให้บังคับไม่ครบ', { field: missing[0], details: { missing_fields: missing } });
  }
  if (row.diagnoses.length === 0) fail(409, 'incomplete_consultation', 'ต้องมีคำวินิจฉัยอย่างน้อยหนึ่งรายการก่อนปิดเคส');

  // A follow-up needs a service: the one asked for, or the one the visit was booked for.
  const followUpBody = ctx.body?.follow_up;
  const followUpService = trimmed(followUpBody?.service_id) ?? appointments().get(row.appointment_id)?.service_id;
  if (followUpBody && !trimmed(followUpBody.starts_at)) invalid('follow_up.starts_at', 'กรุณาระบุวันเวลานัดติดตาม');
  if (followUpBody && !followUpService) invalid('follow_up.service_id', 'ต้องระบุบริการสำหรับนัดติดตาม');

  // The follow-up first: it is the one step that can be refused (a taken slot), and a refusal must
  // leave the case open, as the backend's single transaction does.
  const followUp = followUpBody
    ? bookFollowUp({
        consultation: row,
        startsAt: String(followUpBody.starts_at),
        serviceId: followUpService!,
        staffId: trimmed(followUpBody.staff_id) ?? row.vet_staff_id,
        bookedBy: auth.staffId,
      })
    : undefined;

  const now = nowIso();
  const recordId = uuid();
  const closed = consultations().update(row.id, { status: 'closed', closed_at: now, medical_record_id: recordId })!;
  const record = medicalRecords().insert({
    id: recordId,
    version: 1,
    status: 'signed',
    consultation_id: row.id,
    pet_id: row.pet_id,
    customer_id: row.customer_id,
    branch_id: row.branch_id,
    signed_at: now,
    signed_by_staff_id: auth.staffId,
    content_hash: hashOf(JSON.stringify(closed)),
    integrity_verified: true,
    amendments: [],
  });

  finishVisit({ appointmentId: row.appointment_id, ticketId: row.queue_ticket_id, at: now });
  const invoice = raiseInvoice({ consultation: closed, staffId: auth.staffId, at: now });

  notifyOwner(closed);

  // The record in full, as `MedicalRecordDto` answers it; the page reads only its id.
  const signed = { ...record, content: closed, original_content: closed };
  const result: CloseResult = {
    consultation: closed,
    record: signed,
    ...(followUp ? { follow_up: followUp } : {}),
    charges_satang: closed.treatment_charges_satang,
    ...(invoice ? { invoice } : {}),
  };
  return result;
});

/** The visit summary the product sends the owner on close (`consultation_summary`). */
function notifyOwner(consultation: Consultation) {
  const owner = customers().get(consultation.customer_id);
  if (!owner?.phone) return;
  const pet = petOf(consultation.pet_id);
  const diagnosis = consultation.diagnoses.find((item) => item.is_primary)?.description;
  const charges = (consultation.treatment_charges_satang / 100).toLocaleString('th-TH');
  send({
    channel: owner.line_user_id ? 'line' : 'sms',
    to: owner.line_user_id ? owner.display_name : owner.phone,
    body: `Happy Paws: สรุปการตรวจน้อง${pet?.name ?? ''} วันนี้${diagnosis ? ` ผลวินิจฉัย ${diagnosis}` : ''} ค่าหัตถการ ${charges} บาท ดูรายละเอียดได้ที่พอร์ทัลเจ้าของสัตว์`,
  });
}

// ---- Treatment templates -------------------------------------------------------------------------

const SPECIES_WORDS: Record<string, string> = { dog: 'สุนัข', cat: 'แมว' };

route('GET', '/staff/templates/treatments', (ctx) => {
  requireStaff(ctx, 'consult:read');
  const species = ctx.query.get('species')?.trim();
  const q = ctx.query.get('q')?.trim().toLowerCase();
  const kind = species ? speciesKind(species) : undefined;
  const data = treatmentTemplates()
    .where((template) => {
      if (species && template.species) {
        const same = template.species.toLowerCase() === species.toLowerCase() || (kind !== 'other' && template.species === SPECIES_WORDS[kind!]);
        if (!same) return false;
      }
      return !q || template.name.toLowerCase().includes(q) || (template.description ?? '').toLowerCase().includes(q);
    })
    .sort((a, b) => a.name.localeCompare(b.name, 'th'));
  return { data };
});
