import { day, nowIso, table, type Row } from '../../core/db';
import { created, fail, invalid, matches, notFound, page, requireCustomer, requireStaff, route, stateConflict, type Ctx } from '../../core/http';
import { raisePackageInvoice } from './billing-bridge';
import { couponView } from './coupons';
import { countSegment, lapseOf, lastVisitOf, nameOf, normaliseCriteria, vaccineDues } from './insights';
import {
  addPoints,
  balanceOf,
  loyaltyView,
  packagesOfCustomer,
  packageView,
  portalPackageView,
} from './loyalty';
import { activeCustomers, coupons, customer, daysBetween, homeBranchOf, idempotency, packages, pet, petsOf, segments } from './store';

/**
 * CRM (docs/contract-extensions.md § 25, § 33): segments, coupons, packages, loyalty points and
 * their settings, and the due-back list. Each read works its numbers out from the current data.
 */

// ---- Idempotency ------------------------------------------------------------------------------------

/** The answer given the first time a key was used, so a retried sale is not sold twice. */
function once<T>(ctx: Ctx, scope: string, make: () => T): T {
  const key = ctx.idempotencyKey ? `${scope}:${ctx.idempotencyKey}` : undefined;
  if (key) {
    const seen = idempotency().find((r) => r.key === key);
    if (seen) return seen.answer as T;
  }
  const answer = make();
  if (key) idempotency().insert({ key, answer: answer as unknown as Row, created_at: nowIso() });
  return answer;
}

function bounded(ctx: Ctx, branchId: string | null | undefined) {
  const auth = ctx.auth!;
  if (branchId && auth.branches.length && !auth.branches.includes(branchId)) {
    fail(403, 'insufficient_scope', 'รายการนี้เป็นของสาขาที่บัญชีนี้ไม่ได้ดูแล');
  }
}

// ---- Segments ---------------------------------------------------------------------------------------

function segmentView(row: Row, recount: boolean) {
  const counts = recount ? { ...countSegment(row.criteria), counted_at: nowIso() } : {};
  return {
    id: row.id,
    name: row.name,
    criteria: row.criteria,
    customer_count: row.customer_count,
    reachable_count: row.reachable_count,
    counted_at: row.counted_at,
    created_by_staff_id: row.created_by_staff_id,
    created_at: row.created_at,
    ...counts,
  };
}

route('GET', '/staff/segments', (ctx) => {
  const auth = requireStaff(ctx, 'crm:read');
  const rows = segments()
    .where((s) => !auth.branches.length || !s.criteria.branch_id || auth.branches.includes(s.criteria.branch_id))
    .sort((a, b) => b.created_at.localeCompare(a.created_at));
  return page(rows.map((s) => segmentView(s, false)), ctx.query);
});

route('GET', '/staff/segments/:id', (ctx) => {
  requireStaff(ctx, 'crm:read');
  const row = segments().get(ctx.params.id!);
  if (!row) notFound('กลุ่มลูกค้า');
  bounded(ctx, row.criteria.branch_id);
  return segmentView(row, true);
});

route('POST', '/staff/segments', (ctx) => {
  const auth = requireStaff(ctx, 'crm:manage');
  const name = String(ctx.body?.name ?? '').trim();
  if (!name) invalid('name', 'กรุณาตั้งชื่อกลุ่ม');
  const criteria = normaliseCriteria(ctx.body?.criteria);
  if (criteria.inactive_days !== null && criteria.visited_within_days !== null && criteria.visited_within_days >= criteria.inactive_days) {
    invalid('criteria.visited_within_days', 'ช่วง "มาภายใน" ต้องสั้นกว่า "ไม่ได้มา" จึงจะมีลูกค้าตรงทั้งสองข้อ');
  }
  if (auth.branches.length && !criteria.branch_id) criteria.branch_id = auth.branches[0]!;
  bounded(ctx, criteria.branch_id);
  const counts = countSegment(criteria);
  const row = segments().insert({
    name,
    criteria,
    ...counts,
    counted_at: nowIso(),
    created_by_staff_id: auth.staffId,
    created_at: nowIso(),
  });
  return created(segmentView(row, false));
});

// ---- Coupons ----------------------------------------------------------------------------------------

route('GET', '/staff/coupons', (ctx) => {
  const auth = requireStaff(ctx, 'crm:read');
  const status = ctx.query.get('status');
  const branch = ctx.query.get('branch');
  const q = ctx.query.get('q');
  const rows = coupons()
    .where((c) => !auth.branches.length || !c.branch_id || auth.branches.includes(c.branch_id))
    .filter((c) => !branch || !c.branch_id || c.branch_id === branch)
    .filter((c) => matches(q, c.code, c.name))
    .sort((a, b) => b.created_at.localeCompare(a.created_at))
    .map(couponView)
    .filter((c) => !status || c.status === status);
  return page(rows, ctx.query);
});

const CODE_CHARS = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';

route('POST', '/staff/coupons', (ctx) => {
  const auth = requireStaff(ctx, 'crm:manage');
  const b = ctx.body ?? {};
  const name = String(b.name ?? '').trim();
  if (!name) invalid('name', 'กรุณาตั้งชื่อคูปอง');
  if ((b.percent === undefined) === (b.amount_satang === undefined)) invalid('percent', 'เลือกลดเป็นเปอร์เซ็นต์หรือเป็นบาทอย่างใดอย่างหนึ่ง');
  if (b.percent !== undefined && (b.percent <= 0 || b.percent > 100)) invalid('percent', 'ส่วนลดต้องมากกว่า 0 และไม่เกิน 100%');
  const validFrom = b.valid_from ?? day(0);
  if (b.valid_until && b.valid_until < validFrom) invalid('valid_until', 'วันหมดอายุต้องไม่ก่อนวันเริ่มใช้');
  let code = String(b.code ?? '').trim().toUpperCase();
  if (!code) {
    do code = Array.from({ length: 8 }, () => CODE_CHARS[Math.floor(Math.random() * CODE_CHARS.length)]).join('');
    while (coupons().find((c) => c.code === code));
  }
  if (coupons().find((c) => c.code === code)) fail(409, 'coupon_code_taken', `รหัส ${code} มีคูปองใช้อยู่แล้ว`, { field: 'code' });
  const branchId = b.branch_id ?? (auth.branches.length ? auth.branches[0] : null);
  bounded(ctx, branchId);
  const row = coupons().insert({
    code,
    name,
    percent: b.percent ?? null,
    amount_satang: b.amount_satang ?? null,
    service_ids: Array.isArray(b.service_ids) ? b.service_ids : [],
    max_redemptions: b.max_redemptions ?? null,
    per_customer_limit: b.per_customer_limit ?? null,
    valid_from: validFrom,
    valid_until: b.valid_until ?? null,
    branch_id: branchId ?? null,
    redeemed_count: 0,
    created_by_staff_id: auth.staffId,
    created_at: nowIso(),
  });
  return created(couponView(row));
});

route('POST', '/staff/coupons/:id/deactivate', (ctx) => {
  const auth = requireStaff(ctx, 'crm:manage');
  const row = coupons().get(ctx.params.id!);
  if (!row) notFound('คูปอง');
  bounded(ctx, row.branch_id);
  if (row.deactivated_at) stateConflict('คูปองนี้ถูกปิดไปแล้ว');
  const reason = String(ctx.body?.reason ?? '').trim();
  const next = coupons().update(row.id, {
    deactivated_at: nowIso(),
    deactivated_by_staff_id: auth.staffId,
    ...(reason ? { deactivation_reason: reason } : {}),
  })!;
  return couponView(next);
});

// ---- Packages ---------------------------------------------------------------------------------------

route('GET', '/staff/packages', (ctx) => {
  requireStaff(ctx, 'crm:read');
  const customerId = ctx.query.get('customer_id') ?? '';
  if (!customer(customerId)) notFound('ลูกค้า');
  const status = ctx.query.get('status');
  return { data: packagesOfCustomer(customerId).filter((p) => !status || p.status === status).map(packageView) };
});

route('POST', '/staff/packages', (ctx) =>
  created(
    once(ctx, 'package', () => {
      const auth = requireStaff(ctx, 'crm:manage');
      const b = ctx.body ?? {};
      const owner = customer(b.customer_id);
      if (!owner) invalid('customer_id', 'ไม่พบลูกค้า');
      if (b.pet_id && pet(b.pet_id)?.primary_customer_id !== owner.id) invalid('pet_id', 'สัตว์ตัวนี้ไม่ใช่ของลูกค้าคนนี้');
      const name = String(b.name ?? '').trim();
      if (!name) invalid('name', 'กรุณาตั้งชื่อแพ็กเกจ');
      if (!Array.isArray(b.service_ids) || b.service_ids.length === 0) invalid('service_ids', 'เลือกบริการที่แพ็กเกจครอบคลุมอย่างน้อยหนึ่งรายการ');
      const uses = Number(b.total_uses);
      if (!Number.isInteger(uses) || uses < 1) invalid('total_uses', 'จำนวนครั้งต้องเป็นจำนวนเต็มตั้งแต่ 1');
      if (b.expires_on && b.expires_on <= day(0)) invalid('expires_on', 'วันหมดอายุต้องอยู่หลังวันนี้');
      const branchId = b.branch_id ?? auth.branches[0] ?? homeBranchOf(owner.id);
      const invoice = raisePackageInvoice({
        customerId: owner.id,
        petId: b.pet_id ?? undefined,
        branchId,
        staffId: auth.staffId,
        description: `แพ็กเกจ ${name} (${uses} ครั้ง)`,
        priceSatang: Number(b.price_satang) || 0,
        note: b.note ?? undefined,
      });
      const row = packages().insert({
        name,
        customer_id: owner.id,
        pet_id: b.pet_id ?? null,
        service_ids: b.service_ids,
        total_uses: uses,
        used_count: 0,
        remaining: uses,
        price_satang: Number(b.price_satang) || 0,
        branch_id: branchId,
        invoice_id: invoice.id,
        purchased_at: nowIso(),
        expires_on: b.expires_on ?? null,
        activated_at: null,
        cancelled_at: null,
        status: Number(b.price_satang) > 0 ? 'pending_payment' : 'active',
        ...(b.note ? { note: b.note } : {}),
      });
      return { package: packageView(row), invoice };
    }),
  ),
);

route('GET', '/portal/packages', (ctx) => {
  const auth = requireCustomer(ctx);
  return { data: packagesOfCustomer(auth.customerId).filter((p) => p.status !== 'cancelled').map(portalPackageView) };
});

// ---- Loyalty ----------------------------------------------------------------------------------------

route('GET', '/staff/loyalty/:customerId', (ctx) => {
  requireStaff(ctx, 'crm:read');
  if (!customer(ctx.params.customerId!)) notFound('ลูกค้า');
  return loyaltyView(ctx.params.customerId!);
});

route('POST', '/staff/loyalty/:customerId/adjustments', (ctx) =>
  created(
    once(ctx, `points:${ctx.params.customerId!}`, () => {
      const auth = requireStaff(ctx, 'crm:manage');
      const customerId = ctx.params.customerId!;
      if (!customer(customerId)) notFound('ลูกค้า');
      const points = Number(ctx.body?.points);
      const reason = String(ctx.body?.reason ?? '').trim();
      if (!Number.isInteger(points) || points === 0) invalid('points', 'ระบุจำนวนแต้มที่ไม่ใช่ 0');
      if (!reason) invalid('reason', 'กรุณาระบุเหตุผล');
      const before = balanceOf(customerId);
      if (points < 0 && before.points_balance + points < 0) {
        fail(409, 'insufficient_points', `ลูกค้ามีแต้มคงเหลือ ${before.points_balance.toLocaleString('th-TH')} แต้ม หักเกินกว่านั้นไม่ได้`);
      }
      const entry = addPoints(customerId, 'adjust', points, { note: reason, staff_id: auth.staffId });
      const after = balanceOf(customerId);
      const { customer_id: _c, ...wire } = entry;
      return { entry: wire, points_balance: after.points_balance, lifetime_points: after.lifetime_points };
    }),
  ),
);

// `GET/PUT /clinic/settings/loyalty` are the clinic module's: the rules are clinic settings.

// ---- Due back ---------------------------------------------------------------------------------------

route('GET', '/staff/customers/due-back', (ctx) => {
  requireStaff(ctx, 'crm:read');
  const reason = ctx.query.get('reason') === 'lapsed' ? 'lapsed' : 'vaccine_overdue';
  const branch = ctx.query.get('branch');
  const today = day(0);
  const rows: Row[] = [];

  if (reason === 'vaccine_overdue') {
    const byCustomer = new Map<string, ReturnType<typeof vaccineDues>>();
    // Overdue within the last six months: older than that is a lapsed customer, not a late vaccine.
    // A pet already booked in is coming back anyway.
    const booked = new Set(table('appointments').where((a) => a.starts_at > nowIso() && ['booked', 'confirmed'].includes(a.status)).map((a) => a.pet_id));
    for (const due of vaccineDues().filter((d) => d.due_on < today && daysBetween(d.due_on, today) <= 180 && !booked.has(d.pet_id))) {
      byCustomer.set(due.customer_id, [...(byCustomer.get(due.customer_id) ?? []), due]);
    }
    for (const [customerId, dues] of byCustomer) {
      if (!customer(customerId) || (branch && homeBranchOf(customerId) !== branch)) continue;
      const pets = new Map<string, { pet_id: string; name: string; species: string; overdue_vaccines: Array<{ vaccine_name: string; due_on: string }> }>();
      for (const d of dues) {
        const p = pet(d.pet_id);
        const entry = pets.get(d.pet_id) ?? { pet_id: d.pet_id, name: p?.name ?? '', species: p?.species ?? '', overdue_vaccines: [] as Array<{ vaccine_name: string; due_on: string }> };
        entry.overdue_vaccines.push({ vaccine_name: d.vaccine_name, due_on: d.due_on });
        pets.set(d.pet_id, entry);
      }
      const earliest = dues.map((d) => d.due_on).sort()[0]!;
      const last = lastVisitOf(customerId);
      rows.push({
        id: customerId,
        customer_id: customerId,
        customer_name: nameOf(customerId),
        reason,
        pets: [...pets.values()],
        ...(last ? { last_visit_on: last } : {}),
        due_on: earliest,
        days_overdue: daysBetween(earliest, today),
      });
    }
  } else {
    for (const c of activeCustomers()) {
      if (branch && homeBranchOf(c.id) !== branch) continue;
      const lapse = lapseOf(c.id);
      if (!lapse) continue;
      rows.push({
        id: c.id,
        customer_id: c.id,
        customer_name: c.display_name,
        reason,
        pets: petsOf(c.id).map((p) => ({ pet_id: p.id, name: p.name, species: p.species, overdue_vaccines: [] })),
        ...lapse,
      });
    }
  }

  rows.sort((a, b) => b.due_on.localeCompare(a.due_on));
  return page(rows.map(({ id: _id, ...r }) => r), ctx.query);
});

// ---- Referrals console ------------------------------------------------------------------------------

/**
 * `GET /staff/referrals?customer=` (§ 39): who referred whom, newest first. The rows are the
 * customers module's (`referrals`, kept in this answer's shape); names are read fresh.
 */
route('GET', '/staff/referrals', (ctx) => {
  requireStaff(ctx, 'crm:read');
  const who = ctx.query.get('customer');
  const party = (p: Row) => ({ customer_id: p.customer_id, ...(nameOf(p.customer_id) !== p.customer_id ? { name: nameOf(p.customer_id) } : p.name ? { name: p.name } : {}) });
  const rows = table('referrals')
    .where((r) => !who || r.referrer?.customer_id === who || r.friend?.customer_id === who)
    .sort((a, b) => String(b.created_at).localeCompare(String(a.created_at)))
    .map(({ id, ...r }) => ({ id, ...r, referrer: party(r.referrer), friend: party(r.friend) }));
  const result = page(rows, ctx.query);
  return { data: result.data, page: { next_cursor: result.page.next_cursor ?? null } };
});
