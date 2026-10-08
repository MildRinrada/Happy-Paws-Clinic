/**
 * The shapes of "Inventory" on the wire. Snake_case as the API sends them; absent keys are null.
 * Money is whole satang, quantities are whole units of the item's `unit`.
 */

export type ItemKind = 'medicine' | 'supply';

/** Judged on usable units (lots not yet expired) against the item's reorder level. */
export type StockStatus = 'ok' | 'low' | 'out';

export interface StockLot {
  id: string;
  lot_number?: string;
  /** A clinic calendar day, `YYYY-MM-DD`. */
  expires_on?: string;
  quantity_on_hand: number;
  expired: boolean;
}

export interface OpenOrder {
  purchase_order_id: string;
  number: string;
  supplier_name: string;
  outstanding: number;
  expected_on?: string;
}

/** One branch's stock of one item. */
export interface BranchStock {
  branch_id: string;
  on_hand: number;
  available: number;
  status: StockStatus;
  on_order: number;
  next_expiry?: string;
  lots: StockLot[];
  open_orders: OpenOrder[];
}

/** A row of `GET /staff/inventory/items`: "ยาและเวชภัณฑ์, คงเหลือรายสาขา". */
export interface InventoryItem {
  id: string;
  version: number;
  name: string;
  kind: ItemKind;
  unit: string;
  unit_price_satang: number;
  reorder_level: number;
  created_at: string;
  on_hand: number;
  available: number;
  branches: BranchStock[];
}

export interface ItemsQuery {
  q?: string;
  status?: string;
  branch?: string;
}

export type MovementKind = 'receive' | 'issue' | 'transfer_out' | 'transfer_in' | 'adjustment' | 'dispense';

export interface StockMovement {
  id: string;
  type: MovementKind;
  item_id: string;
  branch_id: string;
  lot_id: string;
  lot_number?: string;
  expires_on?: string;
  /** Signed: positive in, negative out. */
  quantity: number;
  balance_after: number;
  occurred_at: string;
  recorded_by_staff_id: string;
  reference?: string;
  note?: string;
  unit_cost_satang?: number;
  purchase_order_id?: string;
  prescription_id?: string;
  transfer_id?: string;
}

export type PurchaseOrderStatus = 'ordered' | 'partially_received' | 'received';

export interface PurchaseOrderLine {
  id: string;
  item_id: string;
  item_name: string;
  unit: string;
  quantity: number;
  received_quantity: number;
  outstanding: number;
  unit_cost_satang: number;
  line_total_satang: number;
}

export interface PurchaseOrder {
  id: string;
  version: number;
  number: string;
  branch_id: string;
  supplier_name: string;
  status: PurchaseOrderStatus;
  expected_on?: string;
  note?: string;
  ordered_at: string;
  ordered_by_staff_id: string;
  total_cost_satang: number;
  lines: PurchaseOrderLine[];
}

/** The answer of a movement or an adjustment: the rows written and the stock afterwards. */
export interface StockChange {
  item: InventoryItem;
  movements: StockMovement[];
  purchase_order?: PurchaseOrder;
}

export type AlertKind = 'expired' | 'out_of_stock' | 'near_expiry' | 'low_stock';

/** A row of `GET /staff/inventory/alerts`: "ใกล้หมด ใกล้หมดอายุ ล็อตที่ต้องเรียกคืน". */
export interface InventoryAlert {
  kind: AlertKind;
  item_id: string;
  item_name: string;
  unit: string;
  branch_id: string;
  available: number;
  reorder_level: number;
  on_order: number;
  lot_id?: string;
  lot_number?: string;
  expires_on?: string;
  lot_quantity?: number;
  days_to_expiry?: number;
}

/** A catalogue entry that does not exist yet, created by the receipt or the order that names it. */
export interface NewItemInput {
  name: string;
  kind: ItemKind;
  unit: string;
  unit_price_satang: number;
  reorder_level: number;
}

export interface MovementInput {
  type: 'receive' | 'issue' | 'transfer';
  branch_id?: string;
  to_branch_id?: string;
  item_id?: string;
  new_item?: NewItemInput;
  quantity: number;
  lot_id?: string;
  lot_number?: string;
  expires_on?: string;
  unit_cost_satang?: number;
  purchase_order_id?: string;
  reference?: string;
  note?: string;
}

export interface AdjustmentInput {
  branch_id?: string;
  item_id: string;
  lot_id?: string;
  counted_quantity: number;
  reason: string;
}

export interface PurchaseOrderLineInput {
  item_id?: string;
  new_item?: NewItemInput;
  quantity: number;
  unit_cost_satang: number;
}

export interface PurchaseOrderInput {
  branch_id?: string;
  supplier_name: string;
  expected_on?: string;
  note?: string;
  lines: PurchaseOrderLineInput[];
}
