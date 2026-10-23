import { table, type Row } from '../../core/db';
import { fail, type Auth, type Ctx } from '../../core/http';
import type { Invoice } from '@/features/billing/types/billing.types';
import type { ItemKind, PurchaseOrder, StockMovement } from '@/features/inventory/types/inventory.types';
import type { ReportExport } from '@/features/reporting/types/report-export.types';

/**
 * The billing module's tables and the reads it makes of other modules' tables.
 *
 * Invoices, purchase orders and movements are stored in the exact shape their reads answer with
 * (README rule 3). An inventory item is stored without its stock: the stock is the sum of its lots,
 * kept in `inventory_lots`, and `itemView()` in inventory.ts assembles the API's shape from both.
 */

// ---- Own tables ----------------------------------------------------------------------------------

export type InvoiceRow = Invoice &
  Row & {
    /** Internal: the appointment the bill was raised for, so a seed or a close never bills it twice. */
    appointment_id?: string;
  };

export interface ItemRow extends Row {
  version: number;
  name: string;
  kind: ItemKind;
  unit: string;
  unit_price_satang: number;
  reorder_level: number;
  created_at: string;
  active: boolean;
  retired_at?: string;
  /** Internal: what the shelf holds it as, for the seed and the reports. */
  category?: 'vaccine' | 'drug' | 'food' | 'supply';
  /** Internal: the last price paid per unit, for a receipt that leaves the cost out. */
  cost_satang?: number;
}

export interface LotRow extends Row {
  item_id: string;
  branch_id: string;
  lot_number?: string;
  expires_on?: string;
  quantity_on_hand: number;
  received_at: string;
}

export type MovementRow = StockMovement & Row;
export type PurchaseOrderRow = PurchaseOrder & Row;
export type ExportRow = ReportExport & Row & { requested_by_staff_id: string };

export const invoices = () => table<InvoiceRow>('invoices');
export const items = () => table<ItemRow>('inventory_items');
export const lots = () => table<LotRow>('inventory_lots');
export const movements = () => table<MovementRow>('stock_movements');
export const purchaseOrders = () => table<PurchaseOrderRow>('purchase_orders');
export const reportExports = () => table<ExportRow>('report_exports');

/** Idempotency keys already answered: the same key on a retry gets the first answer's row back. */
const replays = () => table<Row & { scope: string; result_id: string }>('billing_replays');

export function replayed(scope: string, key: string | undefined): string | undefined {
  if (!key) return undefined;
  return replays().find((row) => row.id === `${scope}:${key}`)?.result_id;
}

export function remember(scope: string, key: string | undefined, resultId: string): void {
  if (key) replays().insert({ id: `${scope}:${key}`, scope, result_id: resultId });
}

// ---- Other modules' tables (read only) -----------------------------------------------------------

export const branches = (): Row[] => table('branches').where((row) => row.is_active !== false && !row.deleted_at);
export const branchName = (id: string | null | undefined) => (id ? table('branches').get(id)?.name : undefined);
export const customerOf = (id: string | null | undefined) => (id ? table('customers').get(id) : undefined);
export const petOf = (id: string | null | undefined) => (id ? table('pets').get(id) : undefined);
export const serviceOf = (id: string | null | undefined) => (id ? table('services').get(id) : undefined);
export const staffOf = (id: string | null | undefined) => (id ? table('staff_members').get(id) : undefined);
export const staffName = (id: string | null | undefined) => staffOf(id)?.display_name as string | undefined;

export const isVet = (member: Row): boolean => member.staff_type === 'veterinarian' || !!(member.roles as string[] | undefined)?.includes('veterinarian');

// ---- Days ----------------------------------------------------------------------------------------

const bangkokDay = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Bangkok', year: 'numeric', month: '2-digit', day: '2-digit' });
const bangkokHour = new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Bangkok', hour: '2-digit', hourCycle: 'h23' });

/** The clinic day (Asia/Bangkok) an instant falls on, as YYYY-MM-DD: what the product calls `clinicDay`. */
export function clinicDayOf(iso: string | Date | null | undefined): string {
  if (!iso) return '';
  const date = typeof iso === 'string' ? new Date(iso) : iso;
  return Number.isNaN(date.getTime()) ? '' : bangkokDay.format(date);
}

export const clinicToday = () => clinicDayOf(new Date());

export function clinicHourOf(iso: string): number {
  return Number(bangkokHour.format(new Date(iso)));
}

/** A clinic day `days` after another, both YYYY-MM-DD. */
export function addDays(dayText: string, days: number): string {
  const date = new Date(`${dayText}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

export function daysBetween(from: string, to: string): number {
  return Math.round((Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / 86_400_000);
}

// ---- Access --------------------------------------------------------------------------------------

/** Whether the caller holds at least one of the scopes. */
export function holdsAny(auth: Auth, ...scopes: string[]): boolean {
  return scopes.some((scope) => {
    const [area] = scope.split(':');
    return auth.scopes.includes('*') || auth.scopes.includes(scope) || auth.scopes.includes(`${area}:*`);
  });
}

/**
 * The branches a read covers: the one asked for, which must be one of the caller's, or every
 * branch the caller may see. A staff membership bound to no branch sees them all.
 */
export function branchesFor(auth: Auth, asked: string | null | undefined): string[] {
  const all = branches().map((branch) => branch.id);
  const mine = auth.branches.length ? all.filter((id) => auth.branches.includes(id)) : all;
  if (asked) {
    if (auth.branches.length && !auth.branches.includes(asked)) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่ได้ประจำสาขานี้');
    return [asked];
  }
  return mine;
}

/** The branch a write lands on: the one sent, else the caller's first, else the clinic's first. */
export function writeBranch(auth: Auth, sent: string | null | undefined): string {
  const [first] = branchesFor(auth, sent || null);
  if (!first) fail(422, 'validation_failed', 'ยังไม่มีสาขาให้บันทึก', { field: 'branch_id' });
  return first;
}

export function queryText(ctx: Ctx, key: string): string | undefined {
  const value = ctx.query.get(key)?.trim();
  return value ? value : undefined;
}
