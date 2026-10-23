import { nextNumber, nowIso, uuid } from '../../core/db';
import { fail, invalid, stateConflict } from '../../core/http';
import type {
  AlertKind,
  BranchStock,
  InventoryAlert,
  InventoryItem,
  MovementKind,
  NewItemInput,
  OpenOrder,
  PurchaseOrderLine,
  StockLot,
  StockStatus,
} from '@/features/inventory/types/inventory.types';
import type { InventoryForecast, InventoryForecastItem } from '@/features/inventory/types/forecast.types';
import type { DrugOption } from '@/features/prescriptions/types/drug-option.types';
import {
  addDays,
  branches,
  clinicDayOf,
  clinicToday,
  daysBetween,
  items,
  lots,
  movements,
  purchaseOrders,
  type ItemRow,
  type LotRow,
  type MovementRow,
  type PurchaseOrderRow,
} from './store';

/**
 * The stock room: items, their lots at each branch, the movement ledger, purchase orders, and the
 * reads made from them (alerts, forecast). Stock is only ever changed by writing a movement here,
 * so the ledger and the shelf never disagree.
 */

// ---- Reads ---------------------------------------------------------------------------------------

export const isExpired = (lot: { expires_on?: string }, today = clinicToday()) => !!lot.expires_on && lot.expires_on < today;

function lotsAt(itemId: string, branchId: string): LotRow[] {
  return lots()
    .where((lot) => lot.item_id === itemId && lot.branch_id === branchId)
    .sort((a, b) => (a.expires_on ?? '9999').localeCompare(b.expires_on ?? '9999') || a.received_at.localeCompare(b.received_at));
}

function openOrdersFor(itemId: string, branchId: string): OpenOrder[] {
  return purchaseOrders()
    .where((order) => order.branch_id === branchId && (order.status === 'ordered' || order.status === 'partially_received'))
    .flatMap((order) =>
      order.lines
        .filter((line) => line.item_id === itemId && line.outstanding > 0)
        .map((line) => ({
          purchase_order_id: order.id,
          number: order.number,
          supplier_name: order.supplier_name,
          outstanding: line.outstanding,
          ...(order.expected_on ? { expected_on: order.expected_on } : {}),
        })),
    );
}

function statusOf(available: number, reorder: number): StockStatus {
  if (available <= 0) return 'out';
  return available <= reorder ? 'low' : 'ok';
}

/** One branch's stock of one item, judged on usable units (lots not yet expired). */
export function branchStock(item: ItemRow, branchId: string): BranchStock {
  const today = clinicToday();
  const held = lotsAt(item.id, branchId).filter((lot) => lot.quantity_on_hand > 0);
  const onHand = held.reduce((total, lot) => total + lot.quantity_on_hand, 0);
  const usable = held.filter((lot) => !isExpired(lot, today));
  const available = usable.reduce((total, lot) => total + lot.quantity_on_hand, 0);
  const orders = openOrdersFor(item.id, branchId);
  const nextExpiry = usable.find((lot) => lot.expires_on)?.expires_on;
  return {
    branch_id: branchId,
    on_hand: onHand,
    available,
    status: statusOf(available, item.reorder_level),
    on_order: orders.reduce((total, order) => total + order.outstanding, 0),
    ...(nextExpiry ? { next_expiry: nextExpiry } : {}),
    lots: held.map<StockLot>((lot) => ({
      id: lot.id,
      ...(lot.lot_number ? { lot_number: lot.lot_number } : {}),
      ...(lot.expires_on ? { expires_on: lot.expires_on } : {}),
      quantity_on_hand: lot.quantity_on_hand,
      expired: isExpired(lot, today),
    })),
    open_orders: orders,
  };
}

/** The API's item: the catalogue row with its stock at the branches asked for. */
export function itemView(item: ItemRow, branchIds: string[]): InventoryItem {
  const stock = branchIds.map((branchId) => branchStock(item, branchId));
  return {
    id: item.id,
    version: item.version,
    name: item.name,
    kind: item.kind,
    unit: item.unit,
    unit_price_satang: item.unit_price_satang,
    reorder_level: item.reorder_level,
    created_at: item.created_at,
    on_hand: stock.reduce((total, entry) => total + entry.on_hand, 0),
    available: stock.reduce((total, entry) => total + entry.available, 0),
    branches: stock,
    active: item.active,
    ...(item.retired_at ? { retired_at: item.retired_at } : {}),
  };
}

export function requireItem(id: string | undefined, field = 'item_id'): ItemRow {
  const item = id ? items().get(id) : undefined;
  if (!item) fail(422, 'validation_failed', 'ไม่พบรายการสินค้านี้', { field, details: { errors: { [field]: ['ไม่พบรายการสินค้านี้'] } } });
  return item;
}

/** An item by id, or by a name that contains the text asked (case-insensitive): "amoxicillin". */
export function findItem(idOrName: string): ItemRow | undefined {
  const byId = items().get(idOrName);
  if (byId) return byId;
  const needle = idOrName.trim().toLowerCase();
  return items().find((item) => item.name.toLowerCase() === needle) ?? items().find((item) => item.name.toLowerCase().includes(needle));
}

/** Usable units of an item at a branch. */
export function availableAt(itemId: string, branchId: string): number {
  const item = items().get(itemId);
  return item ? branchStock(item, branchId).available : 0;
}

/**
 * The prescriber's picker (`GET /staff/prescriptions/drug-options`): active medicines by name with
 * the selling price and stock at one branch. For the clinical module.
 */
export function drugOptions(branchId: string, q?: string, limit = 20): DrugOption[] {
  const needle = (q ?? '').trim().toLowerCase();
  return items()
    .where((item) => item.active && item.kind === 'medicine' && (!needle || item.name.toLowerCase().includes(needle)))
    .sort((a, b) => a.name.localeCompare(b.name, 'th'))
    .slice(0, limit)
    .map((item) => {
      const stock = branchStock(item, branchId);
      return { item_id: item.id, name: item.name, unit: item.unit, unit_price_satang: item.unit_price_satang, available: stock.available, status: stock.status };
    });
}

// ---- The ledger ----------------------------------------------------------------------------------

function onHandAt(itemId: string, branchId: string): number {
  return lots()
    .where((lot) => lot.item_id === itemId && lot.branch_id === branchId)
    .reduce((total, lot) => total + lot.quantity_on_hand, 0);
}

interface Entry {
  type: MovementKind;
  item: ItemRow;
  branchId: string;
  lot: LotRow;
  quantity: number;
  staffId: string;
  at?: string;
  extra?: Partial<MovementRow>;
}

/** Changes one lot and writes the movement saying so, with the item's balance at the branch after. */
function post(entry: Entry): MovementRow {
  lots().update(entry.lot.id, { quantity_on_hand: entry.lot.quantity_on_hand + entry.quantity });
  const fresh = lots().get(entry.lot.id)!;
  const row: MovementRow = {
    id: uuid(),
    type: entry.type,
    item_id: entry.item.id,
    branch_id: entry.branchId,
    lot_id: fresh.id,
    ...(fresh.lot_number ? { lot_number: fresh.lot_number } : {}),
    ...(fresh.expires_on ? { expires_on: fresh.expires_on } : {}),
    quantity: entry.quantity,
    balance_after: onHandAt(entry.item.id, entry.branchId),
    occurred_at: entry.at ?? nowIso(),
    recorded_by_staff_id: entry.staffId,
    ...stripUndefined(entry.extra ?? {}),
  };
  return movements().insert(row);
}

function stripUndefined<T extends object>(value: T): Partial<T> {
  return Object.fromEntries(Object.entries(value).filter(([, field]) => field !== undefined && field !== '')) as Partial<T>;
}

/** The lot a receipt lands in: the same lot number and expiry at the branch, or a new lot. */
function lotFor(itemId: string, branchId: string, lotNumber?: string, expiresOn?: string, at = nowIso()): LotRow {
  const existing = lots().find(
    (lot) => lot.item_id === itemId && lot.branch_id === branchId && (lot.lot_number ?? '') === (lotNumber ?? '') && (lot.expires_on ?? '') === (expiresOn ?? ''),
  );
  return existing ?? lots().insert({ item_id: itemId, branch_id: branchId, ...(lotNumber ? { lot_number: lotNumber } : {}), ...(expiresOn ? { expires_on: expiresOn } : {}), quantity_on_hand: 0, received_at: at });
}

/** Takes units out first-expiry-first from usable lots, or from one named lot. */
function takeOut(
  item: ItemRow,
  branchId: string,
  quantity: number,
  type: MovementKind,
  staffId: string,
  options: { lotId?: string; at?: string; extra?: Partial<MovementRow> } = {},
): MovementRow[] {
  const today = clinicToday();
  let sources: LotRow[];
  if (options.lotId) {
    const lot = lots().get(options.lotId);
    if (!lot || lot.item_id !== item.id || lot.branch_id !== branchId) invalid('lot_id', 'ไม่พบล็อตนี้ของรายการนี้ที่สาขานี้');
    if (lot.quantity_on_hand < quantity) fail(409, 'insufficient_stock', `จำนวนในล็อตไม่พอ (มี ${lot.quantity_on_hand} ${item.unit})`, { field: 'quantity' });
    sources = [lot];
  } else {
    sources = lotsAt(item.id, branchId).filter((lot) => lot.quantity_on_hand > 0 && !isExpired(lot, today));
    const available = sources.reduce((total, lot) => total + lot.quantity_on_hand, 0);
    if (available < quantity) fail(409, 'insufficient_stock', `สต๊อกไม่พอ (ใช้ได้ ${available} ${item.unit})`, { field: 'quantity' });
  }

  const written: MovementRow[] = [];
  let left = quantity;
  for (const lot of sources) {
    if (left <= 0) break;
    const take = Math.min(left, lot.quantity_on_hand);
    written.push(post({ type, item, branchId, lot, quantity: -take, staffId, at: options.at, extra: options.extra }));
    left -= take;
  }
  return written;
}

export interface ConsumeOptions {
  branchId?: string;
  staffId?: string;
  /** `dispense` for a prescription (the default when one is named), `issue` otherwise. */
  type?: 'dispense' | 'issue';
  prescriptionId?: string;
  at?: string;
}

/**
 * Takes stock out for use: a dispense against a prescription, or an issue for the clinic's own use.
 * First expiry first across usable lots. Throws `409 insufficient_stock` when there is not enough.
 * Answers the movements written (one per lot touched). For the clinical module.
 */
export function consumeStock(itemId: string, qty: number, reason: string, options: ConsumeOptions = {}): MovementRow[] {
  const item = requireItem(itemId);
  const branchId = options.branchId ?? branches()[0]?.id;
  if (!branchId) fail(422, 'validation_failed', 'ยังไม่มีสาขา');
  const quantity = Math.floor(qty);
  if (!(quantity > 0)) invalid('quantity', 'จำนวนต้องมากกว่า 0');
  const type = options.type ?? (options.prescriptionId ? 'dispense' : 'issue');
  return takeOut(item, branchId, quantity, type, options.staffId ?? '', {
    at: options.at,
    extra: { note: reason, ...(options.prescriptionId ? { prescription_id: options.prescriptionId } : {}) },
  });
}

function createItem(input: NewItemInput, field: string): ItemRow {
  const name = String(input.name ?? '').trim();
  if (!name) invalid(`${field}.name`, 'กรุณาระบุชื่อรายการ');
  if (items().find((item) => item.name.toLowerCase() === name.toLowerCase())) invalid(`${field}.name`, 'มีรายการชื่อนี้อยู่แล้ว');
  if (input.kind !== 'medicine' && input.kind !== 'supply') invalid(`${field}.kind`, 'ประเภทไม่ถูกต้อง');
  if (!String(input.unit ?? '').trim()) invalid(`${field}.unit`, 'กรุณาระบุหน่วย');
  return items().insert({
    version: 1,
    name,
    kind: input.kind,
    unit: input.unit.trim(),
    unit_price_satang: Math.max(0, Math.floor(Number(input.unit_price_satang) || 0)),
    reorder_level: Math.max(0, Math.floor(Number(input.reorder_level) || 0)),
    created_at: nowIso(),
    active: true,
    category: input.kind === 'medicine' ? 'drug' : 'supply',
  });
}

function refreshOrder(order: PurchaseOrderRow, lines: PurchaseOrderLine[], patch: Partial<PurchaseOrderRow> = {}): PurchaseOrderRow {
  const received = lines.reduce((total, line) => total + line.received_quantity * line.unit_cost_satang, 0);
  const allIn = lines.every((line) => line.outstanding === 0);
  const someIn = lines.some((line) => line.received_quantity > 0);
  const status = patch.status ?? (order.status === 'cancelled' ? 'cancelled' : allIn ? 'received' : someIn ? 'partially_received' : 'ordered');
  return purchaseOrders().update(order.id, { ...patch, lines, status, received_cost_satang: received })!;
}

export interface MovementRequest {
  type: 'receive' | 'issue' | 'transfer';
  branchId: string;
  toBranchId?: string;
  itemId?: string;
  newItem?: NewItemInput;
  quantity: number;
  lotId?: string;
  lotNumber?: string;
  expiresOn?: string;
  unitCostSatang?: number;
  purchaseOrderId?: string;
  reference?: string;
  note?: string;
  staffId: string;
  at?: string;
}

/** `POST /staff/inventory/movements`: a receipt, an issue, or a transfer to another branch. */
export function recordMovement(request: MovementRequest): { item: ItemRow; movements: MovementRow[]; order?: PurchaseOrderRow } {
  const quantity = Number(request.quantity);
  if (!Number.isInteger(quantity) || quantity <= 0) invalid('quantity', 'จำนวนต้องเป็นจำนวนเต็มมากกว่า 0');
  const base = { reference: request.reference, note: request.note };

  if (request.type === 'receive') {
    const item = request.itemId ? requireItem(request.itemId) : request.newItem ? createItem(request.newItem, 'new_item') : invalid('item_id', 'กรุณาเลือกรายการ');
    if (!item.active) stateConflict('รายการนี้เลิกใช้แล้ว รับเข้าไม่ได้');
    if (request.expiresOn && !/^\d{4}-\d{2}-\d{2}$/.test(request.expiresOn)) invalid('expires_on', 'วันหมดอายุไม่ถูกต้อง');

    let order: PurchaseOrderRow | undefined;
    if (request.purchaseOrderId) {
      order = purchaseOrders().get(request.purchaseOrderId);
      if (!order || order.branch_id !== request.branchId) invalid('purchase_order_id', 'ไม่พบใบสั่งซื้อนี้ที่สาขานี้');
      if (order.status === 'cancelled' || order.status === 'received') stateConflict('ใบสั่งซื้อนี้ปิดแล้ว');
      const line = order.lines.find((entry) => entry.item_id === item.id);
      if (!line) invalid('purchase_order_id', 'ใบสั่งซื้อนี้ไม่มีรายการนี้');
      if (quantity > line.outstanding) invalid('quantity', `รับเกินจำนวนที่ค้างส่ง (ค้าง ${line.outstanding} ${item.unit})`);
    }
    const orderLine = order?.lines.find((entry) => entry.item_id === item.id);
    const cost = request.unitCostSatang ?? orderLine?.unit_cost_satang ?? item.cost_satang;
    const lot = lotFor(item.id, request.branchId, request.lotNumber, request.expiresOn, request.at);
    const written = post({
      type: 'receive',
      item,
      branchId: request.branchId,
      lot,
      quantity,
      staffId: request.staffId,
      at: request.at,
      extra: { ...base, unit_cost_satang: cost, purchase_order_id: order?.id },
    });
    if (cost !== undefined) items().update(item.id, { cost_satang: cost, version: item.version });

    if (order) {
      const lines = order.lines.map((entry) =>
        entry.item_id === item.id ? { ...entry, received_quantity: entry.received_quantity + quantity, outstanding: entry.outstanding - quantity } : entry,
      );
      order = refreshOrder(order, lines);
    }
    return { item: items().get(item.id)!, movements: [written], order };
  }

  const item = requireItem(request.itemId);
  if (request.type === 'issue') {
    return { item, movements: takeOut(item, request.branchId, quantity, 'issue', request.staffId, { lotId: request.lotId, at: request.at, extra: base }) };
  }

  if (request.type !== 'transfer') invalid('type', 'ประเภทไม่ถูกต้อง');
  const to = request.toBranchId;
  if (!to || !branches().some((branch) => branch.id === to)) invalid('to_branch_id', 'กรุณาเลือกสาขาปลายทาง');
  if (to === request.branchId) invalid('to_branch_id', 'สาขาปลายทางต้องไม่ใช่สาขาเดียวกัน');
  const transferId = uuid();
  const out = takeOut(item, request.branchId, quantity, 'transfer_out', request.staffId, {
    lotId: request.lotId,
    at: request.at,
    extra: { ...base, transfer_id: transferId },
  });
  const into = out.map((movement) =>
    post({
      type: 'transfer_in',
      item,
      branchId: to,
      lot: lotFor(item.id, to, movement.lot_number, movement.expires_on, request.at),
      quantity: -movement.quantity,
      staffId: request.staffId,
      at: request.at,
      extra: { ...base, transfer_id: transferId },
    }),
  );
  return { item, movements: [...out, ...into] };
}

/** `POST /staff/inventory/adjustments`: sets a lot to what was counted, the difference as a movement. */
export function adjustStock(request: {
  branchId: string;
  itemId: string;
  lotId?: string;
  counted: number;
  reason: string;
  staffId: string;
  at?: string;
}): { item: ItemRow; movements: MovementRow[] } {
  const item = requireItem(request.itemId);
  if (!Number.isInteger(request.counted) || request.counted < 0) invalid('counted_quantity', 'จำนวนที่นับได้ต้องเป็นจำนวนเต็มไม่ติดลบ');
  if (!request.reason?.trim()) invalid('reason', 'กรุณาระบุเหตุผลการปรับยอด');
  const held = lotsAt(item.id, request.branchId).filter((lot) => lot.quantity_on_hand > 0);
  let lot: LotRow | undefined;
  if (request.lotId) {
    lot = lots().get(request.lotId);
    if (!lot || lot.item_id !== item.id || lot.branch_id !== request.branchId) invalid('lot_id', 'ไม่พบล็อตนี้ของรายการนี้ที่สาขานี้');
  } else if (held.length > 1) {
    invalid('lot_id', 'รายการนี้มีหลายล็อต กรุณาเลือกล็อตที่นับ');
  } else {
    lot = held[0] ?? lotFor(item.id, request.branchId, undefined, undefined, request.at);
  }
  const difference = request.counted - lot!.quantity_on_hand;
  if (difference === 0) return { item, movements: [] };
  return {
    item,
    movements: [
      post({ type: 'adjustment', item, branchId: request.branchId, lot: lot!, quantity: difference, staffId: request.staffId, at: request.at, extra: { note: request.reason.trim() } }),
    ],
  };
}

export function updateItem(item: ItemRow, changes: { name?: string; unit?: string; unit_price_satang?: number; reorder_level?: number; active?: boolean }): ItemRow {
  const patch: Partial<ItemRow> = {};
  if (changes.name !== undefined) {
    const name = changes.name.trim();
    if (!name) invalid('name', 'กรุณาระบุชื่อรายการ');
    if (items().find((other) => other.id !== item.id && other.name.toLowerCase() === name.toLowerCase())) invalid('name', 'มีรายการชื่อนี้อยู่แล้ว');
    patch.name = name;
  }
  if (changes.unit !== undefined) {
    if (!changes.unit.trim()) invalid('unit', 'กรุณาระบุหน่วย');
    patch.unit = changes.unit.trim();
  }
  if (changes.unit_price_satang !== undefined) {
    if (!Number.isInteger(changes.unit_price_satang) || changes.unit_price_satang < 0) invalid('unit_price_satang', 'ราคาต้องไม่ติดลบ');
    patch.unit_price_satang = changes.unit_price_satang;
  }
  if (changes.reorder_level !== undefined) {
    if (!Number.isInteger(changes.reorder_level) || changes.reorder_level < 0) invalid('reorder_level', 'จุดสั่งซื้อต้องไม่ติดลบ');
    patch.reorder_level = changes.reorder_level;
  }
  if (changes.active !== undefined && changes.active !== item.active) {
    patch.active = changes.active;
    patch.retired_at = changes.active ? undefined : nowIso();
  }
  return items().update(item.id, patch)!;
}

// ---- Purchase orders -----------------------------------------------------------------------------

export function nextOrderNumber(at: string): string {
  const period = clinicDayOf(at).slice(0, 7).replace('-', '');
  return `PO-${period}-${String(nextNumber(`PO-${period}`)).padStart(4, '0')}`;
}

export function createPurchaseOrder(request: {
  branchId: string;
  supplierName: string;
  expectedOn?: string;
  note?: string;
  lines: Array<{ item_id?: string; new_item?: NewItemInput; quantity: number; unit_cost_satang: number }>;
  staffId: string;
  at?: string;
}): PurchaseOrderRow {
  const supplier = String(request.supplierName ?? '').trim();
  if (!supplier) invalid('supplier_name', 'กรุณาระบุซัพพลายเออร์');
  if (!request.lines?.length) invalid('lines', 'ใบสั่งซื้อต้องมีอย่างน้อยหนึ่งรายการ');
  if (request.expectedOn && !/^\d{4}-\d{2}-\d{2}$/.test(request.expectedOn)) invalid('expected_on', 'วันที่ไม่ถูกต้อง');

  const lines = request.lines.map<PurchaseOrderLine>((line, index) => {
    const item = line.item_id ? requireItem(line.item_id, `lines[${index}].item_id`) : line.new_item ? createItem(line.new_item, `lines[${index}].new_item`) : invalid(`lines[${index}].item_id`, 'กรุณาเลือกรายการ');
    if (!item.active) invalid(`lines[${index}].item_id`, 'รายการนี้เลิกใช้แล้ว');
    if (!Number.isInteger(line.quantity) || line.quantity <= 0) invalid(`lines[${index}].quantity`, 'จำนวนต้องมากกว่า 0');
    if (!Number.isInteger(line.unit_cost_satang) || line.unit_cost_satang < 0) invalid(`lines[${index}].unit_cost_satang`, 'ราคาต้องไม่ติดลบ');
    return {
      id: uuid(),
      item_id: item.id,
      item_name: item.name,
      unit: item.unit,
      quantity: line.quantity,
      received_quantity: 0,
      outstanding: line.quantity,
      unit_cost_satang: line.unit_cost_satang,
      line_total_satang: line.quantity * line.unit_cost_satang,
      cancelled_quantity: 0,
    };
  });

  const at = request.at ?? nowIso();
  return purchaseOrders().insert({
    version: 1,
    number: nextOrderNumber(at),
    branch_id: request.branchId,
    supplier_name: supplier,
    status: 'ordered',
    ...(request.expectedOn ? { expected_on: request.expectedOn } : {}),
    ...(request.note?.trim() ? { note: request.note.trim() } : {}),
    ordered_at: at,
    ordered_by_staff_id: request.staffId,
    total_cost_satang: lines.reduce((total, line) => total + line.line_total_satang, 0),
    lines,
    received_cost_satang: 0,
  });
}

export function cancelPurchaseOrder(order: PurchaseOrderRow, staffId: string, reason?: string): PurchaseOrderRow {
  if (order.status === 'cancelled') stateConflict('ใบสั่งซื้อนี้ถูกยกเลิกไปแล้ว');
  if (order.status === 'received') stateConflict('ใบสั่งซื้อนี้รับของครบแล้ว ยกเลิกไม่ได้');
  const lines = order.lines.map((line) => ({ ...line, cancelled_quantity: line.outstanding, outstanding: 0 }));
  return refreshOrder(order, lines, {
    status: 'cancelled',
    cancelled_at: nowIso(),
    cancelled_by_staff_id: staffId,
    ...(reason?.trim() ? { cancel_reason: reason.trim() } : {}),
  });
}

// ---- Alerts --------------------------------------------------------------------------------------

const ALERT_ORDER: AlertKind[] = ['expired', 'out_of_stock', 'near_expiry', 'low_stock'];

/** "ใกล้หมด ใกล้หมดอายุ ล็อตที่ต้องเรียกคืน" at some branches, worst first. */
export function stockAlerts(branchIds: string[], withinDays = 90): InventoryAlert[] {
  const today = clinicToday();
  const horizon = addDays(today, withinDays);
  const out: InventoryAlert[] = [];

  for (const item of items().where((row) => row.active)) {
    for (const branchId of branchIds) {
      const stock = branchStock(item, branchId);
      const everStocked = lots().find((lot) => lot.item_id === item.id && lot.branch_id === branchId);
      if (!everStocked) continue;
      const base = { item_id: item.id, item_name: item.name, unit: item.unit, branch_id: branchId, available: stock.available, reorder_level: item.reorder_level, on_order: stock.on_order };
      for (const lot of stock.lots) {
        if (!lot.expires_on) continue;
        const days = daysBetween(today, lot.expires_on);
        if (lot.expired || lot.expires_on <= horizon) {
          out.push({
            ...base,
            kind: lot.expired ? 'expired' : 'near_expiry',
            lot_id: lot.id,
            ...(lot.lot_number ? { lot_number: lot.lot_number } : {}),
            expires_on: lot.expires_on,
            lot_quantity: lot.quantity_on_hand,
            days_to_expiry: days,
          });
        }
      }
      if (stock.status === 'out') out.push({ ...base, kind: 'out_of_stock' });
      else if (stock.status === 'low') out.push({ ...base, kind: 'low_stock' });
    }
  }

  return out.sort(
    (a, b) =>
      ALERT_ORDER.indexOf(a.kind) - ALERT_ORDER.indexOf(b.kind) ||
      (a.days_to_expiry ?? 0) - (b.days_to_expiry ?? 0) ||
      a.available - b.available ||
      a.item_name.localeCompare(b.item_name, 'th'),
  );
}

// ---- Forecast ------------------------------------------------------------------------------------

export const LEAD_TIME_DAYS = 7;

/** Days until out, and how much to order to cover `coverDays` after delivery, from the branch's own ledger. */
export function stockForecast(branchId: string, coverDays: number): InventoryForecast {
  const today = clinicToday();
  const since30 = addDays(today, -30);
  const since90 = addDays(today, -90);
  const ledger = movements().where((movement) => movement.branch_id === branchId);

  const rows = items()
    .where((item) => item.active)
    .flatMap<InventoryForecastItem>((item) => {
      const own = ledger.filter((movement) => movement.item_id === item.id);
      if (!own.length && !lots().find((lot) => lot.item_id === item.id && lot.branch_id === branchId)) return [];
      const used = own.filter((movement) => movement.type === 'dispense' || movement.type === 'issue');
      const usedSince = (since: string) => used.filter((movement) => clinicDayOf(movement.occurred_at) > since).reduce((total, movement) => total - movement.quantity, 0);
      const used30 = usedSince(since30);
      const used90 = usedSince(since90);
      const first = own.map((movement) => clinicDayOf(movement.occurred_at)).sort()[0];
      const historyDays = first ? Math.min(90, Math.max(1, daysBetween(first, today))) : 0;
      const avg30 = round2(used30 / Math.min(30, Math.max(1, historyDays)));
      const avg90 = round2(used90 / Math.min(90, Math.max(1, historyDays)));
      const rate = Math.max(avg30, avg90);
      const stock = branchStock(item, branchId);
      const daysUntilOut = rate > 0 ? Math.floor(stock.available / rate) : undefined;
      const need = rate > 0 ? Math.ceil(rate * (coverDays + LEAD_TIME_DAYS)) : 0;
      const suggested = Math.max(0, need - stock.available - stock.on_order);
      const confidence = historyDays >= 60 && used.length >= 10 ? 'high' : historyDays >= 21 && used.length >= 4 ? 'medium' : 'low';
      return [
        {
          item_id: item.id,
          name: item.name,
          kind: item.kind,
          unit: item.unit,
          reorder_level: item.reorder_level,
          available: stock.available,
          on_order: stock.on_order,
          used_30d: used30,
          used_90d: used90,
          avg_daily_30d: avg30,
          avg_daily_90d: avg90,
          daily_rate: rate,
          ...(daysUntilOut !== undefined ? { days_until_out: daysUntilOut, runs_out_on: addDays(today, daysUntilOut) } : {}),
          suggested_order_qty: suggested,
          history_days: historyDays,
          confidence,
        },
      ];
    })
    .sort((a, b) => (a.days_until_out ?? Infinity) - (b.days_until_out ?? Infinity) || a.name.localeCompare(b.name, 'th'));

  return { branch_id: branchId, as_of: today, cover_days: coverDays, lead_time_days: LEAD_TIME_DAYS, data: rows };
}

const round2 = (value: number) => Math.round(value * 100) / 100;

// Re-exported for the seed, which writes history through the same ledger.
export { lotFor as lotForSeed, post as postForSeed };
