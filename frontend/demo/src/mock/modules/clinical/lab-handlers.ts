import type { LabFlag, LabResult, LabResultInput } from '@/features/laboratory/types/lab.types';
import { minutesFromNow, nowIso, uuid } from '../../core/db';
import { fileUrl, getFile, signedUpload } from '../../core/files';
import { checkVersion, created, fail, invalid, notFound, page, requireStaff, route, stateConflict, type Ctx } from '../../core/http';
import { branches, petOf } from './links';
import { abnormalCount, consultations, labRequestOf, labRequests, type LabRequestRow } from './store';

/**
 * The lab: `/staff/lab-requests` and its results, corrections and files. A request is its own
 * record, apart from the case, because results arrive after the case is signed. Statuses follow
 * the backend: `pending` waits on the lab, `resulted` waits on a vet, `reviewed` has been read.
 */

const STATUSES = ['pending', 'resulted', 'reviewed'];
const FILE_TYPES = ['application/pdf', 'image/jpeg', 'image/png', 'image/webp'];
const MAX_FILE_BYTES = 10 * 1024 * 1024;

function visible(ctx: Ctx, branchId: string): boolean {
  const mine = ctx.auth?.branches ?? [];
  return mine.length === 0 || mine.includes(branchId);
}

function requestFor(ctx: Ctx, id: string): LabRequestRow {
  const row = labRequests().get(id);
  if (!row || !visible(ctx, row.branch_id)) notFound('คำขอตรวจแล็บ');
  return row;
}

const text = (value: unknown) => (typeof value === 'string' && value.trim() ? value.trim() : undefined);
const num = (value: unknown) => (typeof value === 'number' && Number.isFinite(value) ? value : undefined);

/** Worked out from the value and its range; for a text value, as the person entering it says (`LabResult.FlagFor`). */
export function flagFor(input: { value?: number; reference_low?: number; reference_high?: number; abnormal?: boolean }): LabFlag | undefined {
  if (input.value !== undefined && (input.reference_low !== undefined || input.reference_high !== undefined)) {
    if (input.reference_low !== undefined && input.value < input.reference_low) return 'low';
    if (input.reference_high !== undefined && input.value > input.reference_high) return 'high';
    return 'normal';
  }
  if (input.abnormal === true) return 'abnormal';
  if (input.abnormal === false) return 'normal';
  return undefined;
}

/** One value as the request keeps it: the input read the way `LabResult.Record` reads it. */
function valueOf(input: Partial<LabResultInput>, field: string) {
  const analyte = text(input.analyte);
  if (!analyte) invalid(`${field}.analyte`, 'ต้องระบุชื่อค่าที่ตรวจ');
  const value = num(input.value);
  const valueText = text(input.value_text);
  if (value === undefined && !valueText) invalid(`${field}.value`, `ต้องระบุผลของ ${analyte}`);
  const low = num(input.reference_low);
  const high = num(input.reference_high);
  if (low !== undefined && high !== undefined && low > high) invalid(`${field}.reference_low`, 'ค่าอ้างอิงต่ำสุดมากกว่าค่าสูงสุด');
  const flag = flagFor({ value, reference_low: low, reference_high: high, abnormal: typeof input.abnormal === 'boolean' ? input.abnormal : undefined });
  return {
    analyte,
    ...(value !== undefined ? { value_numeric: value } : {}),
    ...(valueText ? { value_text: valueText } : {}),
    ...(text(input.unit) ? { unit: text(input.unit) } : {}),
    ...(low !== undefined ? { reference_low: low } : {}),
    ...(high !== undefined ? { reference_high: high } : {}),
    ...(text(input.reference_text) ? { reference_text: text(input.reference_text) } : {}),
    ...(flag ? { flag } : {}),
  };
}

/** After a correction a request already read goes back in front of a vet. */
function reopened(row: LabRequestRow, at: string): Partial<LabRequestRow> {
  if (row.status !== 'reviewed') return {};
  return { status: 'resulted', resulted_at: at, reviewed_at: undefined, reviewed_by_staff_id: undefined, review_note: undefined };
}

/** Writes a change, bumping the version, with the abnormal count kept in step. */
function save(row: LabRequestRow, patch: Partial<LabRequestRow>) {
  return labRequestOf(labRequests().update(row.id, { ...patch, abnormal_count: abnormalCount({ results: patch.results ?? row.results }) })!);
}

// ---- Requests ------------------------------------------------------------------------------------

route('GET', '/staff/lab-requests', (ctx) => {
  const auth = requireStaff(ctx, 'lab:read');
  const status = ctx.query.get('status');
  if (status && !STATUSES.includes(status)) invalid('status', `status ต้องเป็น ${STATUSES.join(', ')}`);
  const petId = ctx.query.get('pet_id');
  const requestedBy = ctx.query.get('requested_by');
  const by = requestedBy === 'me' ? auth.staffId : requestedBy;
  const branch = ctx.query.get('branch');
  if (branch && !visible(ctx, branch)) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่มีสิทธิ์ในสาขานี้');

  const rows = labRequests()
    .where(
      (row) =>
        visible(ctx, row.branch_id) &&
        (!status || row.status === status) &&
        (!petId || row.pet_id === petId) &&
        (!by || row.requested_by_staff_id === by) &&
        (!branch || row.branch_id === branch),
    )
    .sort((a, b) => b.requested_at.localeCompare(a.requested_at))
    .map(labRequestOf);
  return page(rows, ctx.query);
});

route('POST', '/staff/lab-requests', (ctx) => {
  const auth = requireStaff(ctx, 'lab:request');
  const body = ctx.body ?? {};
  const pet = petOf(text(body.pet_id));
  if (!pet) notFound('สัตว์เลี้ยง');
  if (pet.is_deceased) stateConflict('สัตว์เลี้ยงตัวนี้ถูกบันทึกว่าเสียชีวิตแล้ว ส่งตรวจไม่ได้');

  const consultationId = text(body.consultation_id);
  const consultation = consultationId ? consultations().get(consultationId) : undefined;
  if (consultationId && (!consultation || consultation.pet_id !== pet.id)) notFound('เคสการตรวจ');

  const open = branches().where((row) => row.is_active !== false && !row.deleted_at);
  const branchId: string | undefined =
    consultation?.branch_id ?? text(body.branch_id) ?? (open.length === 1 ? open[0]!.id : auth.branches.length === 1 ? auth.branches[0] : undefined);
  if (!branchId) invalid('branch_id', 'คลินิกมีหลายสาขา ต้องระบุสาขาที่เก็บตัวอย่าง');
  if (!visible(ctx, branchId)) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่มีสิทธิ์ในสาขานี้');

  const tests = (Array.isArray(body.tests) ? body.tests : []) as { name?: string; code?: string }[];
  if (tests.length === 0 || tests.length > 30) fail(409, 'invalid_lab_request', 'ต้องระบุรายการตรวจ 1 ถึง 30 รายการ');
  if (tests.some((test) => !text(test.name))) fail(409, 'invalid_lab_request', 'รายการตรวจต้องมีชื่อ');
  const destination = body.destination === 'external' ? 'external' : 'in_house';
  if (destination === 'external' && !text(body.external_lab_name)) fail(409, 'invalid_lab_request', 'ส่งแล็บภายนอกต้องระบุชื่อแล็บ');

  const row = labRequests().insert({
    id: uuid(),
    version: 1,
    pet_id: pet.id,
    branch_id: branchId,
    ...(consultation ? { consultation_id: consultation.id } : {}),
    destination,
    ...(destination === 'external' ? { external_lab_name: text(body.external_lab_name) } : {}),
    priority: body.priority === 'urgent' ? 'urgent' : 'routine',
    tests: tests.map((test) => ({ name: text(test.name)!, ...(text(test.code) ? { code: text(test.code) } : {}) })),
    ...(text(body.clinical_notes) ? { clinical_notes: text(body.clinical_notes) } : {}),
    requested_by_staff_id: auth.staffId,
    requested_at: nowIso(),
    status: 'pending',
    abnormal_count: 0,
    results: [],
    attachments: [],
  });
  return created(labRequestOf(row));
});

// ---- Results -------------------------------------------------------------------------------------

route('POST', '/staff/lab-requests/:id/results', (ctx) => {
  const auth = requireStaff(ctx, 'lab:write');
  const row = requestFor(ctx, ctx.params.id!);
  const inputs = (Array.isArray(ctx.body?.results) ? ctx.body.results : []) as Partial<LabResultInput>[];
  if (inputs.length === 0 || inputs.length > 100) fail(409, 'invalid_lab_request', 'ต้องส่งผล 1 ถึง 100 ค่าต่อครั้ง');
  if (row.results.length + inputs.length > 300) fail(409, 'invalid_lab_request', 'คำขอตรวจหนึ่งรายการมีผลได้ไม่เกิน 300 ค่า');
  const at = nowIso();
  const added: LabResult[] = inputs.map((input, index) => ({
    id: uuid(),
    ...valueOf(input, `results[${index}]`),
    recorded_at: at,
    recorded_by_staff_id: auth.staffId,
    amended: false,
    previous_values: [],
    voided: false,
  }));
  // Adds to what is there and puts the request back in front of a vet.
  return save(row, {
    results: [...row.results, ...added],
    status: 'resulted',
    resulted_at: at,
    reviewed_at: undefined,
    reviewed_by_staff_id: undefined,
    review_note: undefined,
  });
});

route('POST', '/staff/lab-requests/:id/review', (ctx) => {
  const auth = requireStaff(ctx, 'lab:read');
  const row = requestFor(ctx, ctx.params.id!);
  if (row.status === 'reviewed') return labRequestOf(row);
  if (row.status !== 'resulted') stateConflict('ยังไม่มีผลให้อ่าน');
  const note = text(ctx.body?.note);
  if (note && note.length > 1000) fail(409, 'invalid_lab_request', 'บันทึกของสัตวแพทย์ยาวได้ไม่เกิน 1000 ตัวอักษร');
  return save(row, { status: 'reviewed', reviewed_at: nowIso(), reviewed_by_staff_id: auth.staffId, ...(note ? { review_note: note } : {}) });
});

function resultOf(row: LabRequestRow, resultId: string): LabResult {
  const result = row.results.find((item) => item.id === resultId);
  if (!result) notFound('ผลตรวจ');
  return result;
}

function reasonOf(value: unknown): string {
  const reason = text(value);
  if (!reason) invalid('reason', 'กรุณาระบุเหตุผล');
  if (reason.length > 500) invalid('reason', 'เหตุผลยาวได้ไม่เกิน 500 ตัวอักษร');
  return reason;
}

route('POST', '/staff/lab-requests/:id/results/:resultId/amend', (ctx) => {
  const auth = requireStaff(ctx, 'lab:write');
  const row = requestFor(ctx, ctx.params.id!);
  checkVersion(ctx, row);
  const result = resultOf(row, ctx.params.resultId!);
  if (result.voided) stateConflict('ค่านี้ถูกยกเลิกแล้ว แก้ไขไม่ได้');
  const reason = reasonOf(ctx.body?.reason);
  const corrected = valueOf({ ...ctx.body, analyte: text(ctx.body?.analyte) ?? result.analyte }, 'result');
  const at = nowIso();
  const { id: _id, recorded_at: _at, recorded_by_staff_id: _by, previous_values, amended: _amended, voided: _voided, ...was } = result;
  const revision = {
    analyte: was.analyte,
    ...(was.value_numeric !== undefined ? { value_numeric: was.value_numeric } : {}),
    ...(was.value_text ? { value_text: was.value_text } : {}),
    ...(was.unit ? { unit: was.unit } : {}),
    ...(was.reference_low !== undefined ? { reference_low: was.reference_low } : {}),
    ...(was.reference_high !== undefined ? { reference_high: was.reference_high } : {}),
    ...(was.reference_text ? { reference_text: was.reference_text } : {}),
    ...(was.flag ? { flag: was.flag } : {}),
    replaced_at: at,
    replaced_by_staff_id: auth.staffId,
    reason,
  };
  const next: LabResult = {
    id: result.id,
    recorded_at: result.recorded_at,
    recorded_by_staff_id: result.recorded_by_staff_id,
    ...corrected,
    amended: true,
    amended_at: at,
    amended_by_staff_id: auth.staffId,
    amend_reason: reason,
    previous_values: [...previous_values, revision],
    voided: false,
  };
  return save(row, { results: row.results.map((item) => (item.id === result.id ? next : item)), ...reopened(row, at) });
});

route('POST', '/staff/lab-requests/:id/results/:resultId/void', (ctx) => {
  const auth = requireStaff(ctx, 'lab:write');
  const row = requestFor(ctx, ctx.params.id!);
  checkVersion(ctx, row);
  const result = resultOf(row, ctx.params.resultId!);
  if (result.voided) stateConflict('ค่านี้ถูกยกเลิกไปแล้ว');
  const reason = reasonOf(ctx.body?.reason);
  const at = nowIso();
  const next: LabResult = { ...result, voided: true, voided_at: at, voided_by_staff_id: auth.staffId, void_reason: reason };
  return save(row, { results: row.results.map((item) => (item.id === result.id ? next : item)), ...reopened(row, at) });
});

// ---- Files ---------------------------------------------------------------------------------------

const STORAGE_MARKER = 'storage:';

route('POST', '/staff/lab-requests/:id/attachments/uploads', (ctx) => {
  requireStaff(ctx, 'lab:write');
  const row = requestFor(ctx, ctx.params.id!);
  const type = String(ctx.body?.content_type ?? '');
  const size = Number(ctx.body?.size_bytes);
  if (!FILE_TYPES.includes(type)) invalid('content_type', 'แนบได้เฉพาะไฟล์ PDF, JPEG, PNG หรือ WebP');
  if (!Number.isFinite(size) || size <= 0 || size > MAX_FILE_BYTES) invalid('size_bytes', 'ไฟล์ต้องมีขนาดไม่เกิน 10 MB');
  if (row.attachments.length >= 20) fail(409, 'invalid_lab_attachment', 'คำขอตรวจหนึ่งรายการแนบไฟล์ได้ไม่เกิน 20 ไฟล์');
  const upload = signedUpload({ name: `lab-${row.id}`, content_type: type, size });
  return {
    value: `${STORAGE_MARKER}lab-files/${row.id}/${upload.id}`,
    upload: { url: upload.url, method: 'PUT', headers: upload.headers, expires_at: minutesFromNow(15) },
  };
});

route('POST', '/staff/lab-requests/:id/attachments', (ctx) => {
  const auth = requireStaff(ctx, 'lab:write');
  const row = requestFor(ctx, ctx.params.id!);
  const value = String(ctx.body?.file ?? '');
  const prefix = `${STORAGE_MARKER}lab-files/${row.id}/`;
  if (!value.startsWith(prefix)) fail(409, 'invalid_lab_attachment', 'ไฟล์นี้ไม่ได้อัปโหลดให้คำขอตรวจนี้');
  const fileId = value.slice(prefix.length);
  const stored = getFile(fileId);
  if (!stored) fail(409, 'invalid_lab_attachment', 'ไฟล์ยังส่งไม่ถึงที่เก็บไฟล์ ส่งไฟล์ให้เสร็จก่อนแนบ');
  if (row.attachments.some((attachment) => (attachment as { file_id?: string }).file_id === fileId)) fail(409, 'invalid_lab_attachment', 'แนบไฟล์นี้ไว้แล้ว');
  const name = text(ctx.body?.file_name) ?? stored.name;
  const attachment = {
    id: uuid(),
    file_name: name.slice(0, 200),
    content_type: stored.content_type,
    size_bytes: Number(ctx.body?.size_bytes) || stored.size,
    attached_at: nowIso(),
    attached_by_staff_id: auth.staffId,
    file_id: fileId,
  };
  return save(row, { attachments: [...row.attachments, attachment] });
});

route('DELETE', '/staff/lab-requests/:id/attachments/:attachmentId', (ctx) => {
  requireStaff(ctx, 'lab:write');
  const row = requestFor(ctx, ctx.params.id!);
  checkVersion(ctx, row);
  if (!row.attachments.some((attachment) => attachment.id === ctx.params.attachmentId)) notFound('ไฟล์แนบ');
  return save(row, { attachments: row.attachments.filter((attachment) => attachment.id !== ctx.params.attachmentId) });
});

route('GET', '/staff/lab-requests/:id/attachments/:attachmentId/link', (ctx) => {
  requireStaff(ctx, 'lab:read');
  const row = requestFor(ctx, ctx.params.id!);
  const attachment = row.attachments.find((item) => item.id === ctx.params.attachmentId) as (typeof row.attachments)[number] & { file_id?: string };
  if (!attachment) notFound('ไฟล์แนบ');
  const url = fileUrl(attachment.file_id) ?? labPlaceholder(row, attachment.file_name);
  return { url, expires_at: minutesFromNow(5) };
});

/** A seeded file has no bytes: a one-page summary of the results stands in for the lab's PDF. */
function labPlaceholder(row: LabRequestRow, fileName: string): string {
  const pet = petOf(row.pet_id);
  const lines = [
    `ไฟล์ตัวอย่างในเดโม: ${fileName}`,
    `ผลตรวจของ ${pet?.name ?? row.pet_id} (${pet?.hospital_number ?? ''})`,
    `รายการตรวจ: ${row.tests.map((test) => test.name).join(', ')}`,
    '',
    ...row.results.filter((result) => !result.voided).map((result) => `${result.analyte}: ${result.value_numeric ?? result.value_text ?? ''} ${result.unit ?? ''}${result.flag && result.flag !== 'normal' ? ` (${result.flag})` : ''}`),
  ];
  return `data:text/plain;charset=utf-8,${encodeURIComponent(lines.join('\n'))}`;
}
