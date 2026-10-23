import type { StaffPolicyList } from '@/features/privacy/types/staff-privacy-policy.types';
import type { ConsentPurpose } from '@/features/portal/types/privacy.types';
import { day, nowIso } from '../../core/db';
import { checkVersion, created, fail, invalid, notFound, requireCustomer, requireStaff, route, stateConflict, type Ctx } from '../../core/http';
import { checkSections, currentPolicy, LABEL, LEGACY_LABEL, policies, publicPolicy, type PolicyRow } from './privacy';
import { TEMPLATE_SECTIONS, TEMPLATE_VERSION } from './privacy-template';
import { CONSENT_PURPOSES, consentsOf, customers, dataRequests, recordConsent, type DataRequestRow } from './store';

/**
 * The privacy notice (public and console), consents and data-subject requests under PDPA:
 * `/privacy-policy*`, `/staff/privacy-policy/versions*`, `/staff/customers/{id}/consents`,
 * `/staff/data-requests*`, and the portal's `/portal/consents` and `/portal/data-requests`.
 */

// ---- The public notice ----------------------------------------------------------------------------

route('GET', '/privacy-policy', () => publicPolicy(currentPolicy()));

route('GET', '/privacy-policy/versions/:label', (ctx) => {
  const row = policies().find((entry) => entry.status === 'published' && entry.label === ctx.params.label);
  if (!row) notFound('ประกาศฉบับนี้');
  return publicPolicy(row);
});

// ---- The console ----------------------------------------------------------------------------------

function versionOr404(id: string): PolicyRow {
  const row = policies().get(id);
  if (!row) notFound('ประกาศฉบับนี้');
  return row;
}

function requireIfMatch(ctx: Ctx, row: PolicyRow) {
  if (ctx.ifMatch === undefined) fail(428, 'precondition_required', 'ต้องส่ง If-Match');
  checkVersion(ctx, row);
}

route('GET', '/staff/privacy-policy/versions', (ctx) => {
  requireStaff(ctx, 'settings:read');
  const rows = policies()
    .all()
    .sort((a, b) => Number(a.status === 'published') - Number(b.status === 'published') || (b.published_at ?? b.created_at).localeCompare(a.published_at ?? a.created_at));
  const fromTemplate = rows.filter((row) => row.created_from === 'template').map((row) => row.template_version ?? '');
  const answer: StaffPolicyList = {
    data: rows,
    ...(currentPolicy() ? { current_version_id: currentPolicy()!.id } : {}),
    template: { version: TEMPLATE_VERSION, newer_available: !fromTemplate.some((version) => version >= TEMPLATE_VERSION) },
  };
  return answer;
});

route('POST', '/staff/privacy-policy/versions', (ctx) => {
  const auth = requireStaff(ctx, 'settings:write');
  const source = ctx.body?.source;
  const now = nowIso();
  const base = { status: 'draft' as const, is_current: false, legal_review_done: false, created_by_staff_id: auth.staffId, created_at: now, updated_at: now, version: 1 };
  if (source === 'template') {
    return created(policies().insert({ ...base, created_from: 'template', template_version: TEMPLATE_VERSION, sections: TEMPLATE_SECTIONS.map((section) => ({ ...section })) }));
  }
  if (source === 'current' || source === 'version') {
    if (source === 'version' && !ctx.body?.version_id) invalid('version_id', 'ต้องระบุ version_id ของฉบับที่จะคัดลอก');
    const from = source === 'current' ? currentPolicy() : versionOr404(String(ctx.body.version_id));
    if (!from) invalid('source', 'คลินิกยังไม่ได้เผยแพร่ประกาศฉบับใด ให้สร้างร่างจาก template หรือจากร่างที่มีอยู่');
    return created(policies().insert({ ...base, created_from: 'version', based_on_version_id: from.id, sections: from.sections.map((section) => ({ ...section })) }));
  }
  invalid('source', 'source ต้องเป็น template, current หรือ version');
});

route('PATCH', '/staff/privacy-policy/versions/:id', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const row = versionOr404(ctx.params.id!);
  requireIfMatch(ctx, row);
  if (row.status !== 'draft') stateConflict('ฉบับที่เผยแพร่แล้วแก้ไขไม่ได้ ให้สร้างร่างใหม่จากฉบับนี้');
  return policies().update(row.id, { sections: checkSections(ctx.body?.sections) });
});

route('POST', '/staff/privacy-policy/versions/:id/publish', (ctx) => {
  const auth = requireStaff(ctx, 'settings:danger');
  const row = versionOr404(ctx.params.id!);
  requireIfMatch(ctx, row);
  if (row.status !== 'draft') stateConflict('ฉบับนี้เผยแพร่ไปแล้ว');
  const label = String(ctx.body?.label ?? '').trim();
  const effective = String(ctx.body?.effective_date ?? '').trim();
  const errors: Record<string, string[]> = {};
  if (!label) errors.label = ['ต้องระบุเลขฉบับ'];
  else if (label.length > 32 || !LABEL.test(label)) {
    errors.label = ['เลขฉบับใช้ได้แค่ตัวอักษรภาษาอังกฤษ ตัวเลข จุด ขีดล่าง และขีดกลาง ขึ้นต้นด้วยตัวอักษรหรือตัวเลข ยาวไม่เกิน 32 ตัว เช่น 2026-11'];
  } else if (label === LEGACY_LABEL) errors.label = ['เลขฉบับ 2026-10 ใช้กับประกาศเดิมที่เขียนไว้ในโค้ดหน้าเว็บแล้ว กรุณาใช้เลขอื่น'];
  if (!effective) errors.effective_date = ['ต้องระบุวันที่มีผล'];
  else if (!/^\d{4}-\d{2}-\d{2}$/.test(effective)) errors.effective_date = ['วันที่มีผลต้องอยู่ในรูป yyyy-MM-dd'];
  else if (effective < day(0)) errors.effective_date = ['วันที่มีผลต้องเป็นวันนี้หรือหลังจากนี้'];
  if (Object.keys(errors).length) fail(422, 'validation_failed', 'ข้อมูลที่ส่งมาไม่ถูกต้อง', { details: { errors } });
  if (policies().find((entry) => entry.status === 'published' && entry.label === label)) {
    fail(409, 'privacy_policy_label_taken', 'มีฉบับที่เผยแพร่แล้วใช้เลขนี้อยู่', { field: 'label', details: { label } });
  }
  checkSections(row.sections);
  const now = nowIso();
  for (const previous of policies().where((entry) => entry.is_current)) policies().update(previous.id, { is_current: false });
  return policies().update(row.id, {
    status: 'published',
    is_current: true,
    label,
    effective_date: effective,
    published_at: now,
    published_by_staff_id: auth.staffId,
    legal_review_done: Boolean(ctx.body?.legal_review_done),
  });
});

// ---- Consents -------------------------------------------------------------------------------------

function recordConsents(customerId: string, input: unknown, source: 'portal' | 'counter', userId: string, note?: string) {
  if (!input || typeof input !== 'object') invalid('consents', 'ต้องระบุความยินยอมอย่างน้อยหนึ่งข้อ');
  const entries = Object.entries(input as Record<string, unknown>);
  for (const [purpose, granted] of entries) {
    if (!CONSENT_PURPOSES.includes(purpose as ConsentPurpose)) invalid(`consents.${purpose}`, 'ไม่รู้จักวัตถุประสงค์นี้');
    if (typeof granted !== 'boolean') invalid(`consents.${purpose}`, 'ต้องเป็น true หรือ false');
  }
  for (const [purpose, granted] of entries) recordConsent(customerId, purpose as ConsentPurpose, granted as boolean, { source, userId, note });
  return consentsOf(customerId);
}

function customerOr404(id: string) {
  const row = customers().get(id);
  if (!row) notFound('ลูกค้า');
  return row;
}

route('GET', '/staff/customers/:id/consents', (ctx) => {
  requireStaff(ctx, 'customer:read');
  return consentsOf(customerOr404(ctx.params.id!).id);
});

route('POST', '/staff/customers/:id/consents', (ctx) => {
  const auth = requireStaff(ctx, 'customer:write');
  const customer = customerOr404(ctx.params.id!);
  return recordConsents(customer.id, ctx.body?.consents, 'counter', auth.userId, ctx.body?.note ? String(ctx.body.note) : undefined);
});

route('GET', '/portal/consents', (ctx) => consentsOf(requireCustomer(ctx).customerId));

route('PUT', '/portal/consents', (ctx) => {
  const auth = requireCustomer(ctx);
  return recordConsents(auth.customerId, ctx.body?.consents, 'portal', auth.userId);
});

// ---- Data-subject requests ------------------------------------------------------------------------

const REQUEST_TYPES = ['access', 'correction', 'erasure', 'portability'];

function dataRequestDto(row: DataRequestRow) {
  const end = row.closed_at ? new Date(row.closed_at) : new Date();
  return {
    id: row.id,
    version: row.version,
    customer_id: row.customer_id,
    type: row.type,
    status: row.status,
    raised_at: row.raised_at,
    detail: row.detail ?? null,
    closed_at: row.closed_at ?? null,
    closed_by_user_id: row.closed_by_user_id ?? null,
    resolution: row.resolution ?? null,
    open_days: Math.max(0, Math.floor((end.getTime() - new Date(row.raised_at).getTime()) / 86_400_000)),
  };
}

route('POST', '/portal/data-requests', (ctx) => {
  const auth = requireCustomer(ctx);
  const type = String(ctx.body?.type ?? '');
  if (!REQUEST_TYPES.includes(type)) invalid('type', 'ประเภทคำขอไม่ถูกต้อง');
  const detail = String(ctx.body?.detail ?? '').trim().slice(0, 2000);
  if (dataRequests().find((row) => row.customer_id === auth.customerId && row.type === type && row.status === 'open')) {
    stateConflict('คุณมีคำขอประเภทนี้ที่คลินิกกำลังดำเนินการอยู่แล้ว');
  }
  const row = dataRequests().insert({ version: 1, customer_id: auth.customerId, type: type as DataRequestRow['type'], status: 'open', raised_at: nowIso(), ...(detail ? { detail } : {}) });
  return created({ id: row.id, status: row.status });
});

route('GET', '/staff/data-requests', (ctx) => {
  requireStaff(ctx, 'customer:read');
  const status = ctx.query.get('status');
  const customerId = ctx.query.get('customer_id');
  return dataRequests()
    .where((row) => (!status || row.status === status) && (!customerId || row.customer_id === customerId))
    .sort((a, b) => Number(b.status === 'open') - Number(a.status === 'open') || b.raised_at.localeCompare(a.raised_at))
    .map(dataRequestDto);
});

route('POST', '/staff/data-requests/:id/close', (ctx) => {
  const auth = requireStaff(ctx, 'customer:write');
  const row = dataRequests().get(ctx.params.id!);
  if (!row) notFound('คำขอนี้');
  checkVersion(ctx, row);
  if (row.status !== 'open') stateConflict('คำขอนี้ปิดไปแล้ว');
  const outcome = String(ctx.body?.outcome ?? '');
  const resolution = String(ctx.body?.resolution ?? '').trim();
  if (outcome !== 'completed' && outcome !== 'refused') invalid('outcome', 'ผลต้องเป็น completed หรือ refused');
  if (!resolution) invalid('resolution', 'กรุณาบันทึกสิ่งที่ทำหรือเหตุผลที่ปฏิเสธ');
  return dataRequestDto(dataRequests().update(row.id, { status: outcome, resolution, closed_at: nowIso(), closed_by_user_id: auth.userId })!);
});
