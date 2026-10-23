import { nextNumber, nowIso, table, uuid, type Row } from '../../core/db';
import { emit } from '../../core/bus';
import { fail, invalid, stateConflict } from '../../core/http';
import { send } from '../../core/outbox';
import { releaseCouponRedemption } from '../crm/coupons';
import { usablePackageFor, usePackage } from '../crm/loyalty';
import type {
  InvoiceDiscount,
  InvoiceLine,
  InvoiceLineInput,
  InvoiceLineKind,
  InvoicePayment,
  InvoiceRefund,
  PaymentMethod,
} from '@/features/billing/types/billing.types';
import { clinicDayOf, customerOf, invoices, type InvoiceRow } from './store';

/**
 * The invoice aggregate: totals, the status machine (open → partially_paid → paid → refunded, and
 * void) and every change of money. The handlers and the other modules' helpers all go through
 * here, so a bill raised by a closing case and one raised at the counter add up the same way.
 */

// ---- Listeners -----------------------------------------------------------------------------------

/** `payment` is any tender; the bill's `status` says whether it is now settled. */
export type InvoiceEvent = 'created' | 'line_added' | 'discounted' | 'payment' | 'refunded' | 'voided' | 'receipt';
type Listener = (event: InvoiceEvent, invoice: InvoiceRow) => void;
const listeners = new Set<Listener>();

/**
 * Subscribes to every change of a bill, synchronously, after it is stored. The CRM module can
 * give loyalty points on a `payment` that settles a bill, the portal can notify on `created`. Answers the unsubscribe.
 */
export function onInvoiceEvent(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function announce(event: InvoiceEvent, invoice: InvoiceRow): InvoiceRow {
  for (const listener of listeners) {
    try {
      listener(event, invoice);
    } catch (error) {
      console.error('[mock] invoice listener failed', error);
    }
  }
  emit('billing', { type: `invoice.${event}`, invoice_id: invoice.id, customer_id: invoice.customer_id, branch_id: invoice.branch_id });
  return invoice;
}

// ---- Totals --------------------------------------------------------------------------------------

const sum = (values: number[]) => values.reduce((total, value) => total + value, 0);

/** Every figure the bill carries, from its lines and its money, and the status they put it in. */
export function withTotals<T extends InvoiceRow>(invoice: T): T {
  const subtotal = sum(invoice.lines.map((line) => line.line_total_satang));
  const discount = sum(invoice.discounts.map((entry) => entry.amount_satang));
  const total = Math.max(0, subtotal - discount);
  const paid = sum(invoice.payments.map((entry) => entry.amount_satang));
  const refunded = sum(invoice.refunds.map((entry) => entry.amount_satang));

  let status = invoice.status;
  if (status !== 'void') {
    if (refunded > 0) status = 'refunded';
    else if (paid >= total && (paid > 0 || invoice.lines.length > 0)) status = 'paid';
    else if (paid > 0) status = 'partially_paid';
    else status = 'open';
  }

  return {
    ...invoice,
    status,
    subtotal_satang: subtotal,
    discount_satang: discount,
    total_satang: total,
    paid_satang: paid,
    refunded_satang: refunded,
    balance_satang: status === 'void' ? 0 : Math.max(0, total - paid),
  };
}

function save(invoice: InvoiceRow): InvoiceRow {
  const next = withTotals({ ...invoice, version: invoice.version + 1 });
  return invoices().put(next);
}

function yyyymm(iso: string): string {
  return clinicDayOf(iso).slice(0, 7).replace('-', '');
}

/** `INV-202610-0042`: one running series per month, as the backend's document sequence. */
export function nextInvoiceNumber(issuedAt: string): string {
  const period = yyyymm(issuedAt);
  return `INV-${period}-${String(nextNumber(`INV-${period}`)).padStart(4, '0')}`;
}

function nextReceiptNumber(issuedAt: string): string {
  const period = yyyymm(issuedAt);
  return `RCP-${period}-${String(nextNumber(`RCP-${period}`)).padStart(4, '0')}`;
}

const KINDS: InvoiceLineKind[] = ['service', 'treatment', 'medicine', 'other'];

export function toLine(input: InvoiceLineInput & { source_id?: string }, index = 0): InvoiceLine {
  const description = String(input.description ?? '').trim();
  if (!description) invalid(`lines[${index}].description`, 'กรุณาระบุรายการ');
  const quantity = Number(input.quantity);
  if (!Number.isInteger(quantity) || quantity <= 0) invalid(`lines[${index}].quantity`, 'จำนวนต้องเป็นจำนวนเต็มมากกว่า 0');
  const price = Number(input.unit_price_satang);
  if (!Number.isInteger(price) || price < 0) invalid(`lines[${index}].unit_price_satang`, 'ราคาต้องไม่ติดลบ');
  const kind = input.kind && KINDS.includes(input.kind) ? input.kind : 'other';
  return {
    id: uuid(),
    kind,
    description,
    quantity,
    unit_price_satang: price,
    line_total_satang: quantity * price,
    ...(input.source_id ? { source_id: input.source_id } : {}),
  };
}

// ---- Reads ---------------------------------------------------------------------------------------

/** A customer's bills, newest first. Void bills included; filter on `status` if they are not wanted. */
export function invoicesOfCustomer(customerId: string): InvoiceRow[] {
  return invoices()
    .where((invoice) => invoice.customer_id === customerId)
    .sort((a, b) => b.issued_at.localeCompare(a.issued_at));
}

/** What a customer still owes across their open and partly paid bills, in satang. */
export function outstandingSatangOf(customerId: string): number {
  return sum(
    invoices()
      .where((invoice) => invoice.customer_id === customerId && (invoice.status === 'open' || invoice.status === 'partially_paid'))
      .map((invoice) => invoice.balance_satang),
  );
}

/** The bill raised for a case or an appointment, if there is one already. */
export function invoiceForVisit(visit: { consultationId?: string; appointmentId?: string }): InvoiceRow | undefined {
  return invoices().find(
    (invoice) =>
      invoice.status !== 'void' &&
      ((!!visit.consultationId && invoice.consultation_id === visit.consultationId) ||
        (!!visit.appointmentId && invoice.appointment_id === visit.appointmentId)),
  );
}

// ---- Writes --------------------------------------------------------------------------------------

export interface NewInvoice {
  customerId: string;
  branchId: string;
  /** The staff member raising it: the vet closing the case, or the cashier. */
  staffId: string;
  lines: Array<InvoiceLineInput & { source_id?: string }>;
  petId?: string;
  consultationId?: string;
  appointmentId?: string;
  note?: string;
  /** When it was raised; now unless a seed says otherwise. */
  issuedAt?: string;
  /** Let the customer's packages cover service lines (on unless a seed is writing history). */
  usePackages?: boolean;
}

/**
 * A service line one of the customer's packages pays for goes on at zero and spends a use, as the
 * backend's `AddServiceLineAsync` does. Lines with no catalogue service are left as they are.
 */
function coverByPackages(lines: InvoiceLine[], customerId: string, petId?: string): InvoiceLine[] {
  return lines.flatMap((line) => {
    if (line.kind !== 'service' || !line.source_id || line.unit_price_satang === 0) return [line];
    let covered = 0;
    let name = '';
    while (covered < line.quantity) {
      const pack = usablePackageFor(customerId, line.source_id, petId);
      if (!pack || !usePackage(pack.id)) break;
      name = String(pack.name ?? 'แพ็กเกจ');
      covered += 1;
    }
    if (covered === 0) return [line];
    const free: InvoiceLine = { ...line, id: uuid(), quantity: covered, unit_price_satang: 0, line_total_satang: 0, description: `${line.description} (ใช้สิทธิ์${name})` };
    const rest = line.quantity - covered;
    return rest > 0 ? [free, { ...line, quantity: rest, line_total_satang: rest * line.unit_price_satang }] : [free];
  });
}

/**
 * Raises a bill. Used by `POST /staff/invoices`, by the clinical module when a case closes, and by
 * the seed. A case or appointment that already has a live bill gets that bill back, with the new
 * lines added, rather than a second one.
 */
export function createInvoice(input: NewInvoice): InvoiceRow {
  if (!customerOf(input.customerId)) fail(422, 'validation_failed', 'ไม่พบลูกค้า', { field: 'customer_id' });
  if (!input.lines?.length) invalid('lines', 'บิลต้องมีอย่างน้อยหนึ่งรายการ');
  const typed = input.lines.map((line, index) => toLine(line, index));
  const lines = input.usePackages === false ? typed : coverByPackages(typed, input.customerId, input.petId);

  const existing = invoiceForVisit(input);
  if (existing && (existing.status === 'open' || existing.status === 'partially_paid')) {
    return announce('line_added', save({ ...existing, lines: [...existing.lines, ...lines] }));
  }

  const issuedAt = input.issuedAt ?? nowIso();
  const invoice = withTotals<InvoiceRow>({
    id: uuid(),
    version: 1,
    number: nextInvoiceNumber(issuedAt),
    status: 'open',
    branch_id: input.branchId,
    customer_id: input.customerId,
    ...(input.petId ? { pet_id: input.petId } : {}),
    ...(input.consultationId ? { consultation_id: input.consultationId } : {}),
    ...(input.appointmentId ? { appointment_id: input.appointmentId } : {}),
    issued_at: issuedAt,
    issued_by_staff_id: input.staffId,
    ...(input.note ? { note: input.note } : {}),
    subtotal_satang: 0,
    discount_satang: 0,
    total_satang: 0,
    paid_satang: 0,
    refunded_satang: 0,
    balance_satang: 0,
    lines,
    discounts: [],
    payments: [],
    refunds: [],
  });
  return announce('created', invoices().insert(invoice));
}

export function requireInvoice(id: string): InvoiceRow {
  const invoice = invoices().get(id);
  if (!invoice) fail(404, 'not_found', 'ไม่พบบิล');
  return invoice;
}

function requireCollectible(invoice: InvoiceRow, action: string) {
  if (invoice.status === 'void') stateConflict(`บิลนี้ถูกยกเลิกแล้ว ${action}ไม่ได้`);
  if (invoice.status === 'paid' || invoice.status === 'refunded') stateConflict(`บิลนี้ปิดยอดแล้ว ${action}ไม่ได้`);
}

/** Adds a charge to a bill still taking money, as a dispense does. Answers the new bill. */
export function addInvoiceLine(invoiceId: string, line: InvoiceLineInput & { source_id?: string }): InvoiceRow {
  const invoice = requireInvoice(invoiceId);
  requireCollectible(invoice, 'เพิ่มรายการ');
  return announce('line_added', save({ ...invoice, lines: [...invoice.lines, toLine(line)] }));
}

export function addDiscount(
  invoice: InvoiceRow,
  entry: { amountSatang: number; percent?: number; reason: string; approvedBy: string; appliedBy: string; at?: string },
): InvoiceRow {
  requireCollectible(invoice, 'ให้ส่วนลด');
  const amount = Math.floor(entry.amountSatang);
  if (!(amount > 0)) invalid('amount_satang', 'ส่วนลดต้องมากกว่า 0');
  if (invoice.discount_satang + amount > invoice.subtotal_satang) invalid('amount_satang', 'ส่วนลดรวมเกินยอดก่อนลด');
  if (invoice.total_satang - amount < invoice.paid_satang) invalid('amount_satang', 'ส่วนลดทำให้ยอดสุทธิต่ำกว่าที่ชำระแล้ว');
  const discount: InvoiceDiscount = {
    id: uuid(),
    amount_satang: amount,
    ...(entry.percent !== undefined ? { percent: entry.percent } : {}),
    reason: entry.reason,
    approved_by_staff_id: entry.approvedBy,
    applied_by_staff_id: entry.appliedBy,
    applied_at: entry.at ?? nowIso(),
  };
  return announce('discounted', save({ ...invoice, discounts: [...invoice.discounts, discount] }));
}

const METHODS: PaymentMethod[] = ['cash', 'card', 'promptpay'];

export function takePayment(
  invoice: InvoiceRow,
  entry: { method: PaymentMethod; amountSatang: number; reference?: string; staffId: string; at?: string },
): InvoiceRow {
  requireCollectible(invoice, 'รับชำระ');
  if (!METHODS.includes(entry.method)) invalid('method', 'วิธีชำระไม่ถูกต้อง');
  const amount = Number(entry.amountSatang);
  if (!Number.isInteger(amount) || amount <= 0) invalid('amount_satang', 'ยอดชำระต้องมากกว่า 0');
  if (amount > invoice.balance_satang) {
    fail(409, 'state_conflict', `ยอดชำระเกินยอดค้าง (ค้าง ${(invoice.balance_satang / 100).toLocaleString('th-TH', { minimumFractionDigits: 2 })} บาท)`, {
      field: 'amount_satang',
    });
  }
  const payment: InvoicePayment = {
    id: uuid(),
    method: entry.method,
    amount_satang: amount,
    ...(entry.reference ? { reference: entry.reference } : {}),
    collected_by_staff_id: entry.staffId,
    collected_at: entry.at ?? nowIso(),
  };
  const next = save({ ...invoice, payments: [...invoice.payments, payment] });
  return announce('payment', next);
}

export function giveRefund(
  invoice: InvoiceRow,
  entry: { amountSatang: number; reason: string; method?: PaymentMethod; paymentId?: string; staffId: string; at?: string },
): InvoiceRow {
  if (invoice.status === 'void') stateConflict('บิลนี้ถูกยกเลิกแล้ว');
  if (invoice.paid_satang <= 0) stateConflict('บิลนี้ยังไม่ได้รับเงิน จึงคืนเงินไม่ได้');
  const amount = Number(entry.amountSatang);
  if (!Number.isInteger(amount) || amount <= 0) invalid('amount_satang', 'ยอดคืนต้องมากกว่า 0');
  if (!entry.reason?.trim()) invalid('reason', 'กรุณาระบุเหตุผลการคืนเงิน');
  const refundable = invoice.paid_satang - invoice.refunded_satang;
  if (amount > refundable) fail(409, 'state_conflict', 'ยอดคืนเกินยอดที่รับไว้', { field: 'amount_satang' });

  const tender = entry.paymentId ? invoice.payments.find((payment) => payment.id === entry.paymentId) : undefined;
  if (entry.paymentId && !tender) invalid('payment_id', 'ไม่พบรายการชำระนี้ในบิล');
  if (tender) {
    const back = invoice.refunds.filter((refund) => refund.payment_id === tender.id).reduce((total, refund) => total + refund.amount_satang, 0);
    if (amount > tender.amount_satang - back) fail(409, 'state_conflict', 'ยอดคืนเกินยอดของรายการชำระนี้', { field: 'amount_satang' });
  }
  const method = entry.method ?? tender?.method ?? (invoice.payments.length === 1 ? invoice.payments[0]!.method : 'cash');

  const refund: InvoiceRefund = {
    id: uuid(),
    ...(tender ? { payment_id: tender.id } : {}),
    method,
    amount_satang: amount,
    reason: entry.reason.trim(),
    refunded_by_staff_id: entry.staffId,
    refunded_at: entry.at ?? nowIso(),
  };
  const next = save({ ...invoice, refunds: [...invoice.refunds, refund] });
  // A bill refunded in full gives its coupon use back, as a void does.
  if (next.refunded_satang >= next.paid_satang) releaseCouponRedemption(invoice.id);
  return announce('refunded', next);
}

export function voidBill(invoice: InvoiceRow, reason: string, at = nowIso()): InvoiceRow {
  if (invoice.status === 'void') return invoice;
  if (invoice.status !== 'open' || invoice.payments.length > 0) stateConflict('ยกเลิกได้เฉพาะบิลที่ยังไม่รับเงิน หลังรับเงินแล้วให้คืนเงินแทน');
  if (!reason?.trim()) invalid('reason', 'กรุณาระบุเหตุผลการยกเลิก');
  const voided = save({ ...invoice, status: 'void', voided_at: at, void_reason: reason.trim() });
  releaseCouponRedemption(invoice.id);
  return announce('voided', voided);
}

/**
 * The receipt, once per bill: asking again answers the one already issued. The customer is sent
 * their copy, as "ส่งเข้า LINE" would.
 */
export function issueReceipt(
  invoice: InvoiceRow,
  tax?: { taxId: string; legalName: string; billingAddress: string },
  options: { at?: string; notify?: boolean } = {},
): { invoice: InvoiceRow; created: boolean } {
  if (invoice.receipt) return { invoice, created: false };
  if (invoice.status === 'void') stateConflict('บิลนี้ถูกยกเลิกแล้ว');
  if (invoice.payments.length === 0) stateConflict('ออกใบเสร็จได้หลังรับชำระเงินแล้ว');
  if (tax) {
    if (!/^\d{13}$/.test(tax.taxId.replace(/\D/g, ''))) invalid('tax_id', 'เลขประจำตัวผู้เสียภาษีต้องเป็นตัวเลข 13 หลัก');
    if (!tax.legalName.trim()) invalid('legal_name', 'กรุณาระบุชื่อในใบกำกับภาษี');
    if (!tax.billingAddress.trim()) invalid('billing_address', 'กรุณาระบุที่อยู่');
  }
  const issuedAt = options.at ?? nowIso();
  const next = save({
    ...invoice,
    receipt: {
      id: uuid(),
      number: nextReceiptNumber(issuedAt),
      is_tax_invoice: !!tax,
      amount_satang: invoice.paid_satang - invoice.refunded_satang,
      ...(tax ? { tax_id: tax.taxId.replace(/\D/g, ''), legal_name: tax.legalName.trim(), billing_address: tax.billingAddress.trim() } : {}),
      issued_at: issuedAt,
    },
  });

  const phone = (customerOf(invoice.customer_id) as Row | undefined)?.phone as string | undefined;
  if (options.notify !== false && phone) {
    const amount = (next.receipt!.amount_satang / 100).toLocaleString('th-TH', { minimumFractionDigits: 2 });
    send({
      channel: 'sms',
      to: phone,
      body: `Happy Paws Clinic: ${tax ? 'ใบกำกับภาษี' : 'ใบเสร็จ'}เลขที่ ${next.receipt!.number} ยอด ${amount} บาท ขอบคุณที่ใช้บริการค่ะ`,
    });
  }
  return { invoice: announce('receipt', next), created: true };
}

/** Bills the analytics read: everything but void ones, optionally at a set of branches. */
export function liveInvoices(branchIds?: string[]): InvoiceRow[] {
  return table<InvoiceRow>('invoices').where((invoice) => invoice.status !== 'void' && (!branchIds || branchIds.includes(invoice.branch_id)));
}
