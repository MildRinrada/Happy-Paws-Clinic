import { table, nowIso, type Row } from '../../core/db';
import { accepted, checkVersion, created, fail, invalid, notFound, page, Reply, requireStaff, route, stateConflict, type Auth, type Ctx } from '../../core/http';
import type { Granularity } from '@/features/analytics/types/analytics.types';
import type { InvoiceStatus, PaymentMethod } from '@/features/billing/types/billing.types';
import type { PurchaseOrderStatus, StockStatus } from '@/features/inventory/types/inventory.types';
import type { ReportFormat, ReportKind } from '@/features/reporting/types/report-export.types';
import {
  appointmentStats,
  queueStats,
  retentionAnalytics,
  revenueAnalytics,
  serviceAnalytics,
  staffPerformance,
  todayDashboard,
  workloadStats,
  type Span,
} from './analytics';
import { couponDiscountFor, recordCouponRedemption } from '../crm/coupons';
import { redeemPoints } from '../crm/loyalty';
import { ensureInventory } from './catalogue';
import { addDiscount, createInvoice, giveRefund, issueReceipt, requireInvoice, takePayment, voidBill, type NewInvoice } from './invoices';
import {
  adjustStock,
  cancelPurchaseOrder,
  createPurchaseOrder,
  itemView,
  recordMovement,
  stockAlerts,
  stockForecast,
  updateItem,
} from './inventory';
import { advance, READ_SCOPE } from './reports';
import {
  branchesFor,
  daysBetween,
  holdsAny,
  invoices,
  items,
  purchaseOrders,
  queryText,
  remember,
  replayed,
  reportExports,
  staffOf,
  writeBranch,
  type InvoiceRow,
} from './store';

/**
 * Billing (`/staff/invoices*`), the stock room (`/staff/inventory*`), the dashboard and reports
 * (`/staff/dashboard/today`, `/staff/stats/*`, `/staff/analytics/*`) and report exports
 * (`/staff/reports/exports*`). Scopes are the backend's, route by route.
 */

// ---- Billing -------------------------------------------------------------------------------------

const STATUSES: InvoiceStatus[] = ['open', 'partially_paid', 'paid', 'refunded', 'void'];

route('GET', '/staff/invoices', (ctx) => {
  const auth = requireStaff(ctx, 'invoice:write');
  const scope = branchesFor(auth, queryText(ctx, 'branch'));
  const status = queryText(ctx, 'status');
  if (status && !STATUSES.includes(status as InvoiceStatus)) invalid('status', 'สถานะไม่ถูกต้อง');
  const customerId = queryText(ctx, 'customer_id');
  const from = queryText(ctx, 'from');
  const to = queryText(ctx, 'to');
  const rows = invoices()
    .where(
      (invoice) =>
        scope.includes(invoice.branch_id) &&
        (!status || invoice.status === status) &&
        (!customerId || invoice.customer_id === customerId) &&
        (!from || invoice.issued_at >= new Date(from).toISOString()) &&
        (!to || invoice.issued_at < new Date(to).toISOString()),
    )
    .sort((a, b) => b.issued_at.localeCompare(a.issued_at));
  return page(rows.map(publicInvoice), ctx.query);
});

route('GET', '/staff/invoices/:id', (ctx) => {
  const auth = requireStaff(ctx, 'invoice:write');
  const invoice = invoices().get(ctx.params.id);
  if (!invoice) notFound('บิล');
  branchesFor(auth, invoice.branch_id);
  return withETag(invoice);
});

route('POST', '/staff/invoices', (ctx) => {
  const auth = requireStaff(ctx, 'invoice:write');
  const again = replayed('invoice', ctx.idempotencyKey);
  if (again && invoices().get(again)) return created(publicInvoice(invoices().get(again)!));

  const body = ctx.body ?? {};
  if (!body.customer_id) invalid('customer_id', 'กรุณาเลือกลูกค้า');
  if (!Array.isArray(body.lines) || body.lines.length === 0) invalid('lines', 'บิลต้องมีอย่างน้อยหนึ่งรายการ');
  const pet = body.pet_id ? table('pets').get(body.pet_id) : undefined;
  if (body.pet_id && !pet) invalid('pet_id', 'ไม่พบสัตว์เลี้ยง');

  const input: NewInvoice = {
    customerId: body.customer_id,
    branchId: writeBranch(auth, body.branch_id),
    staffId: auth.staffId,
    lines: body.lines,
    petId: body.pet_id || undefined,
    consultationId: body.consultation_id || undefined,
    note: typeof body.note === 'string' && body.note.trim() ? body.note.trim() : undefined,
  };
  const invoice = createInvoice(input);
  remember('invoice', ctx.idempotencyKey, invoice.id);
  return created(publicInvoice(invoice));
});

route('POST', '/staff/invoices/:id/void', (ctx) => {
  requireStaff(ctx, 'invoice:write');
  const invoice = requireInvoice(ctx.params.id!);
  checkVersion(ctx, invoice);
  return withETag(voidBill(invoice, String(ctx.body?.reason ?? '')));
});

route('POST', '/staff/invoices/:id/payments', (ctx) => {
  const auth = requireStaff(ctx);
  if (!holdsAny(auth, 'payment:collect', 'invoice:collect')) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่มีสิทธิ์รับชำระ (payment:collect)');
  const again = replayed('payment', ctx.idempotencyKey);
  if (again) return withETag(requireInvoice(again));

  const invoice = requireInvoice(ctx.params.id!);
  checkVersion(ctx, invoice);
  const body = ctx.body ?? {};
  const next = takePayment(invoice, {
    method: body.method as PaymentMethod,
    amountSatang: Number(body.amount_satang),
    reference: typeof body.reference === 'string' && body.reference.trim() ? body.reference.trim() : undefined,
    staffId: auth.staffId,
  });
  remember('payment', ctx.idempotencyKey, next.id);
  return withETag(next);
});

route('POST', '/staff/invoices/:id/refunds', (ctx) => {
  const auth = requireStaff(ctx, 'payment:refund');
  const again = replayed('refund', ctx.idempotencyKey);
  if (again) return withETag(requireInvoice(again));

  const invoice = requireInvoice(ctx.params.id!);
  checkVersion(ctx, invoice);
  const body = ctx.body ?? {};
  const next = giveRefund(invoice, {
    amountSatang: Number(body.amount_satang),
    reason: String(body.reason ?? ''),
    method: body.method || undefined,
    paymentId: body.payment_id || undefined,
    staffId: auth.staffId,
  });
  remember('refund', ctx.idempotencyKey, next.id);
  return withETag(next);
});

route('POST', '/staff/invoices/:id/receipt', (ctx) => {
  requireStaff(ctx, 'receipt:issue');
  const invoice = requireInvoice(ctx.params.id!);
  const body = ctx.body ?? {};
  const tax = body.is_tax_invoice
    ? { taxId: String(body.tax_id ?? ''), legalName: String(body.legal_name ?? ''), billingAddress: String(body.billing_address ?? '') }
    : undefined;
  const result = issueReceipt(invoice, tax);
  return new Reply(result.created ? 201 : 200, publicInvoice(result.invoice), { ETag: `"${result.invoice.version}"` });
});

route('POST', '/staff/invoices/:id/discounts', (ctx) => {
  const auth = requireStaff(ctx, 'invoice:discount');
  const invoice = requireInvoice(ctx.params.id!);
  checkVersion(ctx, invoice);
  const body = ctx.body ?? {};
  if (invoice.status !== 'open' && invoice.status !== 'partially_paid') stateConflict('บิลนี้ปิดยอดหรือถูกยกเลิกแล้ว ให้ส่วนลดไม่ได้');
  const room = Math.max(0, Math.min(invoice.subtotal_satang - invoice.discount_satang, invoice.total_satang - invoice.paid_satang));

  if (body.coupon_code) {
    // The CRM module prices the coupon and keeps its redemptions.
    const priced = couponDiscountFor(String(body.coupon_code), invoice);
    if (invoice.discounts.some((entry) => entry.reason.startsWith(`คูปอง ${priced.coupon.code} `))) fail(409, 'coupon_not_applicable', 'ใช้คูปองนี้กับบิลนี้ไปแล้ว');
    const amount = Math.min(priced.amount_satang, room);
    if (amount <= 0) fail(409, 'coupon_not_applicable', 'บิลนี้ไม่มียอดเหลือให้ลด');
    const next = addDiscount(invoice, {
      amountSatang: amount,
      percent: priced.coupon.percent ?? undefined,
      reason: priced.reason,
      approvedBy: (priced.coupon.created_by_staff_id as string | undefined) ?? auth.staffId,
      appliedBy: auth.staffId,
    });
    recordCouponRedemption({ couponId: priced.coupon.id, customerId: invoice.customer_id, invoiceId: invoice.id, invoiceNumber: invoice.number, amountSatang: amount });
    return withETag(next);
  }

  if (body.points !== undefined && body.points !== null) {
    const points = Number(body.points);
    if (!Number.isInteger(points) || points <= 0) invalid('points', 'จำนวนแต้มต้องเป็นจำนวนเต็มมากกว่า 0');
    if (points * 100 > room) invalid('points', `แลกได้ไม่เกิน ${Math.floor(room / 100).toLocaleString('th-TH')} แต้มสำหรับบิลนี้`);
    // The CRM module checks the balance (409 insufficient_points) and writes the ledger entry.
    const redeemed = redeemPoints(invoice.customer_id, points, invoice);
    return withETag(addDiscount(invoice, { amountSatang: Math.min(redeemed.amount_satang, room), reason: `แลกแต้มสะสม ${points.toLocaleString('th-TH')} แต้ม`, approvedBy: auth.staffId, appliedBy: auth.staffId }));
  }

  const reason = String(body.reason ?? '').trim();
  if (!reason) invalid('reason', 'ส่วนลดต้องมีเหตุผล');
  const approver = String(body.approved_by_staff_id ?? '').trim();
  if (!approver) invalid('approved_by_staff_id', 'ส่วนลดต้องมีผู้อนุมัติ');
  if (!staffOf(approver)) invalid('approved_by_staff_id', 'ไม่พบผู้อนุมัติในรายชื่อพนักงาน');
  const hasAmount = body.amount_satang !== undefined && body.amount_satang !== null;
  const hasPercent = body.percent !== undefined && body.percent !== null;
  if (hasAmount === hasPercent) invalid('amount_satang', 'ระบุจำนวนเงินหรือเปอร์เซ็นต์อย่างใดอย่างหนึ่ง');
  const percent = hasPercent ? Number(body.percent) : undefined;
  if (percent !== undefined && !(percent > 0 && percent <= 100)) invalid('percent', 'เปอร์เซ็นต์ต้องอยู่ระหว่าง 0 ถึง 100');
  const amount = percent !== undefined ? Math.floor((invoice.subtotal_satang * percent) / 100) : Number(body.amount_satang);
  if (!Number.isInteger(amount) || amount <= 0) invalid(hasPercent ? 'percent' : 'amount_satang', 'ส่วนลดต้องมากกว่า 0');
  return withETag(addDiscount(invoice, { amountSatang: amount, percent, reason, approvedBy: approver, appliedBy: auth.staffId }));
});

/** What the cash desk reads: the stored bill without the module's internal fields. */
function publicInvoice(invoice: InvoiceRow) {
  const { appointment_id: _appointment, ...rest } = invoice;
  return rest;
}

function withETag(invoice: InvoiceRow): Reply {
  return new Reply(200, publicInvoice(invoice), { ETag: `"${invoice.version}"` });
}

// ---- Inventory -----------------------------------------------------------------------------------

const STOCK_STATUSES: StockStatus[] = ['ok', 'low', 'out'];

route('GET', '/staff/inventory/items', (ctx) => {
  ensureInventory();
  const auth = requireStaff(ctx, 'inventory:read');
  const scope = branchesFor(auth, queryText(ctx, 'branch'));
  const q = queryText(ctx, 'q')?.toLowerCase();
  const status = queryText(ctx, 'status');
  if (status && !STOCK_STATUSES.includes(status as StockStatus)) invalid('status', 'สถานะไม่ถูกต้อง');
  const data = items()
    .where((item) => !q || item.name.toLowerCase().includes(q))
    .sort((a, b) => Number(b.active) - Number(a.active) || a.name.localeCompare(b.name, 'th'))
    .map((item) => itemView(item, scope))
    .filter((item) => !status || item.branches.some((branch) => branch.status === status));
  return { data };
});

route('PATCH', '/staff/inventory/items/:id', (ctx) => {
  const auth = requireStaff(ctx, 'inventory:write');
  const item = items().get(ctx.params.id);
  if (!item) notFound('รายการสินค้า');
  checkVersion(ctx, item);
  const next = updateItem(item, ctx.body ?? {});
  return itemView(next, branchesFor(auth, null));
});

route('POST', '/staff/inventory/movements', (ctx) => {
  ensureInventory();
  const auth = requireStaff(ctx, 'inventory:write');
  const again = replayed('movement', ctx.idempotencyKey);
  if (again) {
    const [itemId, ...ids] = again.split(',');
    const item = items().get(itemId);
    if (item) return { item: itemView(item, branchesFor(auth, null)), movements: ids.map((id) => table('stock_movements').get(id)).filter(Boolean) };
  }
  const body = ctx.body ?? {};
  if (!['receive', 'issue', 'transfer'].includes(body.type)) invalid('type', 'ประเภทไม่ถูกต้อง');
  const branchId = writeBranch(auth, body.branch_id);
  const result = recordMovement({
    type: body.type,
    branchId,
    toBranchId: body.to_branch_id,
    itemId: body.item_id,
    newItem: body.new_item,
    quantity: Number(body.quantity),
    lotId: body.lot_id,
    lotNumber: body.lot_number?.trim() || undefined,
    expiresOn: body.expires_on || undefined,
    unitCostSatang: body.unit_cost_satang,
    purchaseOrderId: body.purchase_order_id || undefined,
    reference: body.reference,
    note: body.note,
    staffId: auth.staffId,
  });
  remember('movement', ctx.idempotencyKey, [result.item.id, ...result.movements.map((movement) => movement.id)].join(','));
  const shown = [...new Set([branchId, ...(body.to_branch_id ? [body.to_branch_id as string] : [])])];
  return { item: itemView(result.item, shown), movements: result.movements, ...(result.order ? { purchase_order: result.order } : {}) };
});

route('POST', '/staff/inventory/adjustments', (ctx) => {
  ensureInventory();
  const auth = requireStaff(ctx, 'inventory:adjust');
  const body = ctx.body ?? {};
  const branchId = writeBranch(auth, body.branch_id);
  const result = adjustStock({
    branchId,
    itemId: body.item_id,
    lotId: body.lot_id || undefined,
    counted: Number(body.counted_quantity),
    reason: String(body.reason ?? ''),
    staffId: auth.staffId,
  });
  return { item: itemView(result.item, [branchId]), movements: result.movements };
});

route('GET', '/staff/inventory/alerts', (ctx) => {
  ensureInventory();
  const auth = requireStaff(ctx, 'inventory:read');
  const scope = branchesFor(auth, queryText(ctx, 'branch'));
  const within = ctx.query.get('within_days') ? Number(ctx.query.get('within_days')) : 90;
  if (!Number.isInteger(within) || within < 1 || within > 365) invalid('within_days', 'ช่วงวันต้องอยู่ระหว่าง 1 ถึง 365');
  return { data: stockAlerts(scope, within) };
});

route('GET', '/staff/inventory/forecast', (ctx) => {
  ensureInventory();
  const auth = requireStaff(ctx, 'inventory:read');
  const [branchId] = branchesFor(auth, queryText(ctx, 'branch'));
  if (!branchId) invalid('branch', 'กรุณาเลือกสาขา');
  const cover = ctx.query.get('cover_days') ? Number(ctx.query.get('cover_days')) : 30;
  if (!Number.isInteger(cover) || cover < 1 || cover > 180) invalid('cover_days', 'จำนวนวันต้องอยู่ระหว่าง 1 ถึง 180');
  return stockForecast(branchId, cover);
});

const ORDER_STATUSES: PurchaseOrderStatus[] = ['ordered', 'partially_received', 'received', 'cancelled'];

route('GET', '/staff/inventory/purchase-orders', (ctx) => {
  ensureInventory();
  const auth = requireStaff(ctx, 'inventory:purchase');
  const scope = branchesFor(auth, queryText(ctx, 'branch'));
  const status = queryText(ctx, 'status');
  if (status && !ORDER_STATUSES.includes(status as PurchaseOrderStatus)) invalid('status', 'สถานะไม่ถูกต้อง');
  const supplier = queryText(ctx, 'supplier')?.toLowerCase();
  const rows = purchaseOrders()
    .where((order) => scope.includes(order.branch_id) && (!status || order.status === status) && (!supplier || order.supplier_name.toLowerCase().includes(supplier)))
    .sort((a, b) => b.ordered_at.localeCompare(a.ordered_at));
  return page(rows, ctx.query, 20);
});

route('POST', '/staff/inventory/purchase-orders', (ctx) => {
  ensureInventory();
  const auth = requireStaff(ctx, 'inventory:purchase');
  const again = replayed('purchase_order', ctx.idempotencyKey);
  if (again && purchaseOrders().get(again)) return created(purchaseOrders().get(again));
  const body = ctx.body ?? {};
  const order = createPurchaseOrder({
    branchId: writeBranch(auth, body.branch_id),
    supplierName: body.supplier_name,
    expectedOn: body.expected_on || undefined,
    note: body.note,
    lines: Array.isArray(body.lines) ? body.lines : [],
    staffId: auth.staffId,
  });
  remember('purchase_order', ctx.idempotencyKey, order.id);
  return created(order);
});

route('POST', '/staff/inventory/purchase-orders/:id/cancel', (ctx) => {
  const auth = requireStaff(ctx, 'inventory:purchase');
  const order = purchaseOrders().get(ctx.params.id);
  if (!order) notFound('ใบสั่งซื้อ');
  checkVersion(ctx, order);
  return cancelPurchaseOrder(order, auth.staffId, ctx.body?.reason);
});

// ---- Dashboard and reports -----------------------------------------------------------------------

const DAY = /^\d{4}-\d{2}-\d{2}$/;

/** `from`, `to`, `branch` and `granularity` of a ranged read, checked as the backend checks them. */
function spanOf(auth: Auth, from: string | null | undefined, to: string | null | undefined, branch: string | null | undefined, granularity?: string | null): Span {
  if (!from || !DAY.test(from)) invalid('from', 'กรุณาระบุวันเริ่มต้น (YYYY-MM-DD)');
  if (!to || !DAY.test(to)) invalid('to', 'กรุณาระบุวันสิ้นสุด (YYYY-MM-DD)');
  if (from > to) invalid('to', 'วันสิ้นสุดต้องไม่ก่อนวันเริ่มต้น');
  if (daysBetween(from, to) > 366) invalid('to', 'ช่วงรายงานยาวได้ไม่เกิน 1 ปี');
  const g = granularity || 'day';
  if (!['day', 'week', 'month'].includes(g)) invalid('granularity', 'ช่วงย่อยไม่ถูกต้อง');
  return { from, to, branchIds: branchesFor(auth, branch), ...(branch ? { branchId: branch } : {}), granularity: g as Granularity };
}

const ranged = (ctx: Ctx, auth: Auth) => spanOf(auth, ctx.query.get('from'), ctx.query.get('to'), queryText(ctx, 'branch'), ctx.query.get('granularity'));

route('GET', '/staff/dashboard/today', (ctx) => {
  const auth = requireStaff(ctx, 'dashboard:read');
  const branch = queryText(ctx, 'branch');
  return todayDashboard(auth, branchesFor(auth, branch), branch);
});

route('GET', '/staff/stats/appointments', (ctx) => appointmentStats(ranged(ctx, requireStaff(ctx, 'stats:read'))));
route('GET', '/staff/stats/queue', (ctx) => queueStats(ranged(ctx, requireStaff(ctx, 'stats:read'))));
route('GET', '/staff/stats/workload', (ctx) => workloadStats(ranged(ctx, requireStaff(ctx, 'stats:read'))));
route('GET', '/staff/analytics/revenue', (ctx) => revenueAnalytics(ranged(ctx, requireStaff(ctx, 'analytics:read'))));
route('GET', '/staff/analytics/services', (ctx) => serviceAnalytics(ranged(ctx, requireStaff(ctx, 'analytics:read'))));
route('GET', '/staff/analytics/retention', (ctx) => retentionAnalytics(ranged(ctx, requireStaff(ctx, 'analytics:read'))));
route('GET', '/staff/analytics/staff-performance', (ctx) => staffPerformance(ranged(ctx, requireStaff(ctx, 'analytics:hr'))));

const FORMATS: ReportFormat[] = ['csv', 'xlsx', 'pdf'];

route('POST', '/staff/reports/exports', (ctx) => {
  const auth = requireStaff(ctx, 'report:export');
  const body = ctx.body ?? {};
  const report = body.report as ReportKind;
  if (!(report in READ_SCOPE)) invalid('report', 'ไม่รู้จักรายงานนี้');
  if (!FORMATS.includes(body.format)) invalid('format', 'รูปแบบไฟล์ไม่ถูกต้อง');
  if (!holdsAny(auth, READ_SCOPE[report])) fail(403, 'insufficient_scope', `บัญชีนี้ไม่มีสิทธิ์ดูรายงานนี้ (${READ_SCOPE[report]})`);
  const span = spanOf(auth, body.from, body.to, body.branch_id || null);
  const job = reportExports().insert({
    report,
    format: body.format,
    status: 'queued',
    from: span.from,
    to: span.to,
    ...(span.branchId ? { branch_id: span.branchId } : {}),
    branch_ids: span.branchIds,
    created_at: nowIso(),
    requested_by_staff_id: auth.staffId,
  });
  return accepted(withoutOwner(job));
});

route('GET', '/staff/reports/exports/:id', (ctx) => {
  const auth = requireStaff(ctx, 'report:export');
  const job = reportExports().get(ctx.params.id);
  if (!job || job.requested_by_staff_id !== auth.staffId) notFound('งานส่งออก');
  const span: Span = { from: job.from, to: job.to, branchIds: job.branch_ids ?? [], ...(job.branch_id ? { branchId: job.branch_id } : {}), granularity: job.report === 'retention' || daysBetween(job.from, job.to) > 62 ? 'month' : 'day' };
  return withoutOwner(advance(job, span));
});

function withoutOwner(job: Row) {
  const { requested_by_staff_id: _owner, ...rest } = job;
  return rest;
}
