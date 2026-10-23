import type {
  CancelledVisit,
  PetCarer,
  PetDeceasedResult,
  PetDeceasedUndone,
  PetDetail,
  PetDocumentUpload,
  TreatmentPlan,
} from '@/features/pets/types/pet.types';
import { day, minutesFromNow, nowIso, table, type Row } from '../../core/db';
import { signedUpload } from '../../core/files';
import { checkVersion, created, fail, file, invalid, noContent, notFound, page, requireStaff, route, stateConflict, type Ctx } from '../../core/http';
import { appointmentsOfPet, cancelAppointment, createSeries, OPEN } from '../appointments/appointments';
import { buildPdf, type PdfLine } from './pdf';
import { timelineOf } from './pets-history';
import {
  addPetDocument,
  allergies,
  allergiesOf,
  carers,
  compact,
  customerName,
  customers,
  latestWeight,
  matchesSearch,
  petDocuments,
  pets,
  petSummary,
  registerPet,
  searchKey,
  vaccinations,
  vaccinationsOf,
  weightSeries,
  type CarerRow,
  type PetRow,
} from './store';

/** The patient registry: `/staff/pets*` and `POST /staff/documents/generate`. */

const SEX = ['male', 'female', 'unknown'];
const SEVERITY = ['mild', 'moderate', 'severe'];
const DOCUMENT_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/heic', 'application/pdf'];
const DOCUMENT_KINDS = ['photo', 'lab_result', 'referral', 'imaging', 'other'];
const MAX_DOCUMENT_BYTES = 10 * 1024 * 1024;

function petOr404(id: string): PetRow {
  const pet = pets().get(id);
  if (!pet) notFound('สัตว์เลี้ยง');
  return pet;
}

function validationFailed(errors: Record<string, string[]>): never {
  fail(422, 'validation_failed', 'ข้อมูลที่ส่งมาไม่ถูกต้อง', { details: { errors } });
}

function chipTaken(chip: string, exceptId?: string): boolean {
  return pets().all().some((pet) => pet.id !== exceptId && pet.microchip_number === chip);
}

function checkIdentity(body: Record<string, unknown>, errors: Record<string, string[]>) {
  if (body.sex !== undefined && !SEX.includes(String(body.sex))) errors.sex = ['เพศไม่ถูกต้อง'];
  if (body.birth_date !== undefined && body.birth_date !== '') {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(String(body.birth_date))) errors.birth_date = ['วันเกิดต้องอยู่ในรูป YYYY-MM-DD'];
    else if (String(body.birth_date) > day(0)) errors.birth_date = ['วันเกิดต้องไม่เป็นวันในอนาคต'];
  }
  if (body.microchip_number) {
    const chip = String(body.microchip_number).trim();
    if (!/^[0-9A-Za-z]{9,15}$/.test(chip)) errors.microchip_number = ['เลขไมโครชิปต้องเป็นตัวเลข 9-15 หลัก'];
  }
}

export function petDetail(pet: PetRow): PetDetail {
  const weight = latestWeight(pet.id);
  return {
    pet: petSummary(pet),
    ...(weight ? { latest_weight: weight } : {}),
    allergies: allergiesOf(pet.id),
    carers: carers()
      .where((link) => link.pet_id === pet.id)
      .sort((a, b) => Number(b.is_primary) - Number(a.is_primary) || a.created_at.localeCompare(b.created_at))
      .map(carerDto),
  };
}

function carerDto(link: CarerRow): PetCarer {
  return compact({ id: link.id, customer_id: link.customer_id, is_primary: link.is_primary, may_view_history: link.may_view_history, revoked_at: link.revoked_at });
}

route('GET', '/staff/pets', (ctx) => {
  requireStaff(ctx, 'patient:read');
  const q = (ctx.query.get('q') ?? '').trim();
  const customerId = ctx.query.get('customer_id');
  const species = ctx.query.get('species');
  const includeDeceased = ctx.query.get('include_deceased') === 'true';
  const caredFor = customerId ? new Set(carers().where((link) => link.customer_id === customerId && !link.revoked_at).map((link) => link.pet_id)) : null;
  // The counter types whatever it has: the pet's name or HN, or the owner's name or phone.
  const people = new Map<string, string[]>();
  if (q) {
    for (const link of carers().where((row) => !row.revoked_at)) people.set(link.pet_id, [...(people.get(link.pet_id) ?? []), link.customer_id]);
  }
  const ownersOf = (pet: PetRow) => [...new Set([pet.primary_customer_id, ...(people.get(pet.id) ?? [])])].map((id) => customers().get(id)).filter((row) => row !== undefined);
  const found = (pet: PetRow) => {
    if (matchesSearch(q, [pet.name, pet.nickname, pet.hospital_number, pet.microchip_number])) return true;
    const owners = ownersOf(pet);
    return matchesSearch(q, owners.map((owner) => owner.display_name), owners.map((owner) => owner.phone));
  };
  const exactKey = searchKey(q);
  const rows = pets()
    .where(
      (pet) =>
        (includeDeceased || !pet.is_deceased) &&
        (!species || pet.species === species) &&
        (!caredFor || pet.primary_customer_id === customerId || caredFor.has(pet.id)) &&
        (!q || found(pet)),
    )
    .sort((a, b) => {
      // An exact HN or chip comes first: that is what was scanned.
      const exact = (pet: PetRow) => Number(searchKey(pet.hospital_number) === exactKey || pet.microchip_number === q);
      return exact(b) - exact(a) || a.name.localeCompare(b.name, 'th');
    });
  const answer = page(rows, ctx.query, 25);
  return { data: answer.data.map(petSummary), page: answer.page };
});

route('POST', '/staff/pets', (ctx) => {
  requireStaff(ctx, 'patient:write');
  const body = ctx.body ?? {};
  const errors: Record<string, string[]> = {};
  const name = String(body.name ?? '').trim();
  const species = String(body.species ?? '').trim();
  if (!name) errors.name = ['กรุณากรอกชื่อสัตว์เลี้ยง'];
  else if (name.length > 100) errors.name = ['ชื่อยาวได้ไม่เกิน 100 ตัวอักษร'];
  if (!species) errors.species = ['กรุณาเลือกชนิดสัตว์'];
  else if (species.length > 60) errors.species = ['ชนิดยาวได้ไม่เกิน 60 ตัวอักษร'];
  checkIdentity(body, errors);
  const owner = customers().get(body.primary_customer_id);
  if (!owner) errors.primary_customer_id = ['ไม่พบลูกค้าเจ้าของสัตว์'];
  else if (owner.merged_into_customer_id) errors.primary_customer_id = ['เรกคอร์ดลูกค้านี้ถูกรวมไปแล้ว'];
  const chip = String(body.microchip_number ?? '').trim();
  if (chip && !errors.microchip_number && chipTaken(chip)) errors.microchip_number = ['ไมโครชิปนี้ถูกใช้กับคนไข้รายอื่นแล้ว'];
  if (Object.keys(errors).length) validationFailed(errors);

  const pet = registerPet(owner!.id, {
    name,
    species,
    breed: String(body.breed ?? ''),
    ...(chip ? { microchip_number: chip } : {}),
    sex: body.sex || undefined,
    birth_date: body.birth_date || undefined,
    birth_date_estimated: Boolean(body.birth_date_estimated),
    neutered: typeof body.neutered === 'boolean' ? body.neutered : undefined,
  });
  return created(petSummary(pet));
});

route('GET', '/staff/pets/:id', (ctx) => {
  requireStaff(ctx, 'patient:read');
  return petDetail(petOr404(ctx.params.id!));
});

route('PATCH', '/staff/pets/:id', (ctx) => {
  requireStaff(ctx, 'patient:write');
  const pet = petOr404(ctx.params.id!);
  if (ctx.ifMatch === undefined) fail(428, 'precondition_required', 'ต้องส่ง If-Match');
  checkVersion(ctx, pet);
  const body = ctx.body ?? {};
  const errors: Record<string, string[]> = {};
  checkIdentity(body, errors);
  if (body.name !== undefined && !String(body.name).trim()) errors.name = ['กรุณากรอกชื่อสัตว์เลี้ยง'];
  if (body.species !== undefined && !String(body.species).trim()) errors.species = ['กรุณาเลือกชนิดสัตว์'];
  const chip = body.microchip_number !== undefined ? String(body.microchip_number).trim() : undefined;
  if (chip && !errors.microchip_number && chipTaken(chip, pet.id)) errors.microchip_number = ['ไมโครชิปนี้ถูกใช้กับคนไข้รายอื่นแล้ว'];
  if (Object.keys(errors).length) validationFailed(errors);
  const clear: string[] = Array.isArray(body.clear) ? body.clear : [];
  const neuterChanges = (typeof body.neutered === 'boolean' && body.neutered !== pet.neutered) || clear.includes('neutered');
  if (pet.is_deceased && neuterChanges) stateConflict('บันทึกของสัตว์ที่เสียชีวิตแล้ว เปลี่ยนสถานะการทำหมันไม่ได้');

  const patch: Partial<PetRow> = {};
  if (body.name !== undefined) patch.name = String(body.name).trim();
  if (body.species !== undefined) patch.species = String(body.species).trim();
  if (body.breed !== undefined) patch.breed = String(body.breed).trim() || undefined;
  if (chip !== undefined) patch.microchip_number = chip || undefined;
  if (body.sex !== undefined) patch.sex = body.sex;
  if (body.birth_date) {
    patch.birth_date = body.birth_date;
    patch.birth_date_estimated = Boolean(body.birth_date_estimated);
  } else if (body.birth_date_estimated !== undefined && pet.birth_date) patch.birth_date_estimated = Boolean(body.birth_date_estimated);
  if (typeof body.neutered === 'boolean') patch.neutered = body.neutered;
  if (clear.includes('birth_date')) Object.assign(patch, { birth_date: undefined, birth_date_estimated: undefined });
  if (clear.includes('neutered')) patch.neutered = undefined;
  for (const field of ['nickname', 'diet', 'behaviour'] as const) {
    if (body[field] !== undefined) patch[field] = String(body[field] ?? '').trim() || undefined;
  }
  return petSummary(pets().update(pet.id, patch)!);
});

route('POST', '/staff/pets/:id/allergies', (ctx) => {
  requireStaff(ctx, 'record:write');
  const pet = petOr404(ctx.params.id!);
  if (pet.is_deceased) stateConflict('บันทึกของสัตว์ที่เสียชีวิตแล้วปิดไปแล้ว');
  const substance = String(ctx.body?.substance ?? '').trim();
  const severity = String(ctx.body?.severity ?? '');
  const errors: Record<string, string[]> = {};
  if (!substance) errors.substance = ['กรุณาระบุสิ่งที่แพ้'];
  if (!SEVERITY.includes(severity)) errors.severity = ['กรุณาเลือกความรุนแรง'];
  if (Object.keys(errors).length) validationFailed(errors);
  const reaction = String(ctx.body?.reaction ?? '').trim();
  const row = allergies().insert({ pet_id: pet.id, substance, severity, ...(reaction ? { reaction } : {}), recorded_at: nowIso() });
  const { pet_id: _pet, ...allergy } = row;
  return created(allergy);
});

// ---- Records read from the profile page ------------------------------------------------------------

route('GET', '/staff/pets/:id/history', (ctx) => {
  requireStaff(ctx, 'record:read');
  const pet = petOr404(ctx.params.id!);
  return page(timelineOf(pet.id), ctx.query, 30);
});

route('GET', '/staff/pets/:id/vaccinations', (ctx) => {
  requireStaff(ctx, 'patient:read');
  return vaccinationsOf(petOr404(ctx.params.id!).id);
});

route('POST', '/staff/pets/:id/vaccinations', (ctx) => {
  const auth = requireStaff(ctx, 'record:write');
  const pet = petOr404(ctx.params.id!);
  if (pet.is_deceased) stateConflict('บันทึกของสัตว์ที่เสียชีวิตแล้วปิดไปแล้ว รับวัคซีนเพิ่มไม่ได้');
  const body = ctx.body ?? {};
  const errors: Record<string, string[]> = {};
  const name = String(body.vaccine_name ?? '').trim();
  const lot = String(body.lot_number ?? '').trim();
  const expires = String(body.vial_expires_on ?? '').trim();
  if (!name) errors.vaccine_name = ['กรุณาระบุชื่อวัคซีน'];
  if (!lot) errors.lot_number = ['กรุณาระบุเลขล็อต'];
  if (!/^\d{4}-\d{2}-\d{2}$/.test(expires)) errors.vial_expires_on = ['กรุณาระบุวันหมดอายุของขวด'];
  if (body.next_due_on && body.valid_for_days !== undefined && body.valid_for_days !== null) errors.next_due_on = ['ระบุวันนัดครั้งถัดไปหรือจำนวนวัน อย่างใดอย่างหนึ่ง'];
  const administered = body.administered_at ? String(body.administered_at) : nowIso();
  if (administered > minutesFromNow(5)) errors.administered_at = ['วันที่ฉีดต้องไม่เป็นเวลาในอนาคต'];
  if (expires && /^\d{4}-\d{2}-\d{2}$/.test(expires) && expires < administered.slice(0, 10)) errors.vial_expires_on = ['ขวดวัคซีนหมดอายุก่อนวันที่ฉีด'];
  if (Object.keys(errors).length) validationFailed(errors);

  let nextDue: string | undefined = body.next_due_on || undefined;
  if (!nextDue) {
    const days = body.valid_for_days === undefined || body.valid_for_days === null ? 365 : Number(body.valid_for_days);
    if (days > 0) {
      const date = new Date(administered);
      date.setDate(date.getDate() + days);
      nextDue = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    }
  }
  const row = vaccinations().insert({
    pet_id: pet.id,
    vaccine_name: name,
    ...(body.manufacturer ? { manufacturer: String(body.manufacturer).trim() } : {}),
    lot_number: lot,
    vial_expires_on: expires,
    administered_at: administered,
    administered_by_staff_id: auth.staffId,
    ...(nextDue ? { next_due_on: nextDue } : {}),
    ...(body.note ? { note: String(body.note).trim() } : {}),
  });
  const { pet_id: _pet, ...dose } = row;
  return created(dose);
});

route('GET', '/staff/pets/:id/weights', (ctx) => {
  requireStaff(ctx, 'patient:read');
  return weightSeries(petOr404(ctx.params.id!).id);
});

// ---- Owners and carers ----------------------------------------------------------------------------

route('POST', '/staff/pets/:id/owners', (ctx) => {
  requireStaff(ctx, 'patient:write');
  const pet = petOr404(ctx.params.id!);
  if (pet.is_deceased) stateConflict('บันทึกของสัตว์ที่เสียชีวิตแล้วปิดไปแล้ว');
  const customer = customers().get(ctx.body?.customer_id);
  if (!customer || customer.deleted_at) invalid('customer_id', 'ไม่พบลูกค้ารายนี้');
  if (customer.merged_into_customer_id) invalid('customer_id', 'เรกคอร์ดลูกค้านี้ถูกรวมไปแล้ว');
  const mayView = Boolean(ctx.body?.may_view_history);
  const live = carers().find((link) => link.pet_id === pet.id && link.customer_id === customer.id && !link.revoked_at);
  // Adding someone already linked changes what they may read instead of failing.
  const link = live
    ? carers().update(live.id, { may_view_history: live.is_primary ? true : mayView })!
    : carers().insert({ pet_id: pet.id, customer_id: customer.id, is_primary: false, may_view_history: mayView, created_at: nowIso() });
  return created(carerDto(link));
});

route('DELETE', '/staff/pets/:id/owners/:customerId', (ctx) => {
  requireStaff(ctx, 'patient:write');
  const pet = petOr404(ctx.params.id!);
  const link = carers().find((row) => row.pet_id === pet.id && row.customer_id === ctx.params.customerId && !row.revoked_at);
  if (!link) notFound('ผู้ดูแลรายนี้');
  if (link.is_primary || pet.primary_customer_id === link.customer_id) stateConflict('เจ้าของหลักถอดออกไม่ได้ ให้โอนเจ้าของหลักก่อน');
  carers().update(link.id, { revoked_at: nowIso() });
  return noContent();
});

// ---- Documents ----------------------------------------------------------------------------------

route('POST', '/staff/pets/:id/documents', (ctx) => {
  const auth = requireStaff(ctx, 'record:write');
  const pet = petOr404(ctx.params.id!);
  const body = ctx.body ?? {};
  const errors: Record<string, string[]> = {};
  const fileName = String(body.file_name ?? '').trim();
  const contentType = String(body.content_type ?? '').toLowerCase();
  const size = Number(body.size_bytes);
  if (!fileName) errors.file_name = ['ต้องระบุชื่อไฟล์'];
  if (!DOCUMENT_TYPES.includes(contentType)) errors.content_type = ['รับเฉพาะไฟล์ JPEG, PNG, WEBP, HEIC หรือ PDF'];
  if (!Number.isFinite(size) || size <= 0) errors.size_bytes = ['ไฟล์ว่างเปล่า'];
  else if (size > MAX_DOCUMENT_BYTES) errors.size_bytes = ['ไฟล์ใหญ่เกิน 10 MB'];
  if (!DOCUMENT_KINDS.includes(body.kind)) errors.kind = ['ประเภทเอกสารไม่ถูกต้อง'];
  if (Object.keys(errors).length) validationFailed(errors);

  const upload = signedUpload({ name: fileName, content_type: contentType, size });
  const title = String(body.title ?? '').trim() || fileName.replace(/\.[^.]+$/, '');
  const document = addPetDocument({
    pet_id: pet.id,
    kind: body.kind,
    title,
    file_name: fileName,
    content_type: contentType,
    size_bytes: size,
    visible_to_owner: Boolean(body.visible_to_owner),
    file_id: upload.id,
    uploaded_by_staff_id: auth.staffId,
  });
  const answer: PetDocumentUpload = {
    document: { id: document.id, title, file_name: fileName },
    upload: { url: upload.url, method: 'PUT', headers: upload.headers, expires_at: minutesFromNow(15) },
  };
  return created(answer);
});

// ---- Death and its undoing ------------------------------------------------------------------------

/**
 * Appointments are the appointments module's: a death cancels the pet's open future visits through
 * its `cancelAppointment` (each cancellation tells the owner, as the module does for any cancel), and
 * a booked treatment plan is one all-or-nothing `createSeries`.
 */
route('POST', '/staff/pets/:id/deceased', (ctx) => {
  requireStaff(ctx, 'patient:write');
  const pet = petOr404(ctx.params.id!);
  if (pet.is_deceased) stateConflict('บันทึกนี้ปิดไว้แล้ว');
  const occurredAt = ctx.body?.occurred_at ? String(ctx.body.occurred_at) : nowIso();
  if (occurredAt > minutesFromNow(5)) invalid('occurred_at', 'เวลาที่เสียชีวิตต้องไม่เป็นเวลาในอนาคต');

  const now = nowIso();
  const cancelled = appointmentsOfPet(pet.id)
    .filter((row) => OPEN.includes(row.status) && row.starts_at > now)
    .map((row) => cancelAppointment(row.id, 'สัตว์เลี้ยงเสียชีวิต').id);
  pets().update(pet.id, { is_deceased: true, deceased_at: occurredAt, deceased_cancelled_ids: cancelled });
  const answer: PetDeceasedResult = { pet_id: pet.id, deceased_at: occurredAt, cancelled_appointment_ids: cancelled };
  return answer;
});

route('POST', '/staff/pets/:id/deceased/undo', (ctx) => {
  requireStaff(ctx, 'patient:write');
  const pet = petOr404(ctx.params.id!);
  const reason = String(ctx.body?.reason ?? '').trim();
  if (!reason) invalid('reason', 'กรุณาระบุเหตุผล');
  if (!pet.is_deceased) stateConflict('บันทึกนี้ยังเปิดอยู่');
  const previous = pet.deceased_at ?? nowIso();
  const now = nowIso();
  const visits: CancelledVisit[] = (pet.deceased_cancelled_ids ?? [])
    .map((id) => table('appointments').get(id))
    .filter((row): row is Row => Boolean(row))
    .map((row) => ({
      id: row.id,
      branch_id: row.branch_id,
      service_id: row.service_id,
      ...(row.staff_id ? { staff_id: row.staff_id } : {}),
      starts_at: row.starts_at,
      ends_at: row.ends_at,
      has_passed: row.starts_at < now,
    }));
  pets().update(pet.id, { is_deceased: false, deceased_at: undefined, deceased_cancelled_ids: undefined });
  const answer: PetDeceasedUndone = { pet_id: pet.id, undone_at: now, previous_deceased_at: previous, cancelled_appointments: visits };
  return answer;
});

// ---- Treatment plans ----------------------------------------------------------------------------

const plans = () => table<Row & TreatmentPlan & { idempotency_key?: string }>('treatment_plans');

route('POST', '/staff/pets/:id/treatment-plans', (ctx) => {
  requireStaff(ctx, 'consult:write');
  const pet = petOr404(ctx.params.id!);
  if (ctx.idempotencyKey) {
    const earlier = plans().find((row) => row.idempotency_key === ctx.idempotencyKey);
    if (earlier) {
      const { idempotency_key: _key, ...plan } = earlier;
      return created(plan);
    }
  }
  if (pet.is_deceased) stateConflict('บันทึกของสัตว์ที่เสียชีวิตแล้วปิดไปแล้ว');
  const body = ctx.body ?? {};
  const errors: Record<string, string[]> = {};
  const title = String(body.title ?? '').trim();
  const sessions = Number(body.sessions);
  const interval = Number(body.interval_days);
  const first = new Date(String(body.first_starts_at ?? ''));
  if (!title) errors.title = ['กรุณาตั้งชื่อแผนการรักษา'];
  if (!body.service_id) errors.service_id = ['กรุณาเลือกบริการ'];
  if (!Number.isInteger(sessions) || sessions < 1 || sessions > 52) errors.sessions = ['จำนวนครั้งต้องอยู่ระหว่าง 1 ถึง 52'];
  if (!Number.isInteger(interval) || interval < 1 || interval > 365) errors.interval_days = ['ระยะห่างต้องอยู่ระหว่าง 1 ถึง 365 วัน'];
  if (Number.isNaN(first.getTime())) errors.first_starts_at = ['กรุณาเลือกวันเวลาครั้งแรก'];
  else if (body.book && first.toISOString() < nowIso()) errors.first_starts_at = ['ครั้งแรกต้องเป็นเวลาหลังจากนี้'];
  if (Object.keys(errors).length) validationFailed(errors);

  const starts = Array.from({ length: sessions }, (_, index) => {
    const when = new Date(first);
    when.setDate(when.getDate() + index * interval);
    return when.toISOString();
  });
  const booked = body.book
    ? createSeries({
        petId: pet.id,
        serviceId: String(body.service_id),
        branchId: body.branch_id || undefined,
        staffId: body.staff_id || undefined,
        reason: [title, body.notes].filter(Boolean).join(' · '),
        source: 'staff',
        starts,
      })
    : undefined;
  const seriesId = booked?.series_id;
  const planned = starts.map((planned_at, index) => {
    const appointment_id = booked?.appointments[index]?.id;
    return { number: index + 1, planned_at, ...(appointment_id ? { appointment_id } : {}) };
  });
  const plan = plans().insert({
    pet_id: pet.id,
    title,
    service_id: body.service_id,
    ...(body.notes ? { notes: String(body.notes) } : {}),
    ...(seriesId ? { series_id: seriesId } : {}),
    created_at: nowIso(),
    sessions: planned,
    ...(ctx.idempotencyKey ? { idempotency_key: ctx.idempotencyKey } : {}),
  });
  const { idempotency_key: _key, ...answer } = plan;
  return created(answer);
});

// ---- Printed documents --------------------------------------------------------------------------

const TYPE_TITLES: Record<string, string> = {
  vaccination_book: 'Vaccination record',
  health_certificate: 'Health certificate',
  referral_letter: 'Referral letter',
};

function englishSpecies(species: string): string {
  return ({ สุนัข: 'Dog', แมว: 'Cat', กระต่าย: 'Rabbit', นก: 'Bird', หนูแฮมสเตอร์: 'Hamster', เต่า: 'Tortoise' } as Record<string, string>)[species] ?? 'Animal';
}

route('POST', '/staff/documents/generate', (ctx: Ctx) => {
  const auth = requireStaff(ctx, 'record:read');
  const body = ctx.body ?? {};
  const type = String(body.type ?? '');
  if (!TYPE_TITLES[type]) invalid('type', 'ประเภทเอกสารไม่ถูกต้อง');
  const staff = table('staff_members').get(auth.staffId);
  if (type !== 'vaccination_book' && !staff?.license_no) fail(403, 'insufficient_scope', 'ใบรับรองและใบส่งตัวต้องออกโดยผู้มีเลขใบอนุญาตประกอบวิชาชีพ');
  const pet = pets().get(body.pet_id);
  if (!pet) notFound('สัตว์เลี้ยง');
  if (type === 'referral_letter') {
    const errors: Record<string, string[]> = {};
    if (!String(body.refer_to ?? '').trim()) errors.refer_to = ['กรุณาระบุสถานพยาบาลที่ส่งตัวไป'];
    if (!String(body.reason ?? '').trim()) errors.reason = ['กรุณาระบุเหตุผลที่ส่งตัว'];
    if (Object.keys(errors).length) validationFailed(errors);
  }

  const clinic = table('clinic_profile').all()[0];
  const lines: PdfLine[] = [
    { text: `${clinic?.name_en ?? 'Happy Paws Animal Clinic'}`, size: 18, bold: true, gap: 0 },
    { text: TYPE_TITLES[type]!, size: 14, bold: true, gap: 30 },
    { text: `Patient: ${pet.hospital_number} (${englishSpecies(pet.species)}${pet.breed ? `, ${pet.breed}` : ''})`, gap: 30 },
    { text: `Microchip: ${pet.microchip_number ?? '-'}` },
    { text: `Birth date: ${pet.birth_date ?? 'unknown'}   Sex: ${pet.sex ?? 'unknown'}   Neutered: ${pet.neutered === undefined ? 'unknown' : pet.neutered ? 'yes' : 'no'}` },
    { text: `Latest weight: ${latestWeight(pet.id)?.weight_kg ?? '-'} kg` },
  ];
  if (type === 'vaccination_book') {
    lines.push({ text: 'Date          Vaccine                         Lot          Next due', bold: true, gap: 28 });
    for (const dose of vaccinationsOf(pet.id).doses.slice().reverse()) {
      const vaccine = dose.vaccine_name.replace(/[^\x20-\x7e]/g, '').trim() || 'Vaccine';
      lines.push({ text: `${dose.administered_at.slice(0, 10)}    ${vaccine.padEnd(30)}  ${dose.lot_number.padEnd(11)}  ${dose.next_due_on ?? '-'}` });
    }
  } else if (type === 'health_certificate') {
    lines.push({ text: 'The animal above was examined today and found clinically healthy and fit to travel.', gap: 28 });
    if (body.remarks) lines.push({ text: `Remarks: ${body.remarks}` });
  } else {
    lines.push({ text: `Referred to: ${body.refer_to}`, gap: 28 }, { text: `Reason: ${body.reason}` });
  }
  lines.push(
    { text: `Issued: ${new Date().toISOString().slice(0, 10)} by staff ${staff?.license_no ? `licence ${staff.license_no}` : auth.staffId.slice(0, 8)}`, gap: 36 },
    { text: `Owner record: ${customerName(pet.primary_customer_id) ? 'on file' : '-'}` },
    { text: 'Demo document. The real system prints this in Thai with the clinic\'s letterhead.', size: 9, gap: 40 },
  );
  const bytes = buildPdf(lines);
  return file(bytes as unknown as BlobPart, 'application/pdf', `${type}-${pet.hospital_number}.pdf`);
});
