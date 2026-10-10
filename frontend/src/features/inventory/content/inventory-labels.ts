import type { BadgeTone } from '@/components/ui/badge';
import type { AlertKind, ItemKind, MovementKind, PurchaseOrderStatus, StockStatus } from '../types/inventory.types';

/** The words for the values the API sends as codes. */

export const kindLabels: Record<ItemKind, string> = {
  medicine: 'ยา',
  supply: 'เวชภัณฑ์',
};

export const stockStatusLabels: Record<StockStatus, { label: string; tone: BadgeTone }> = {
  ok: { label: 'พอใช้', tone: 'green' },
  low: { label: 'ใกล้หมด', tone: 'amber' },
  out: { label: 'หมด', tone: 'red' },
};

export const movementLabels: Record<MovementKind, string> = {
  receive: 'รับเข้า',
  issue: 'จ่ายออก',
  transfer_out: 'โอนออก',
  transfer_in: 'โอนเข้า',
  adjustment: 'ปรับยอด',
  dispense: 'จ่ายยา',
};

export const alertLabels: Record<AlertKind, { label: string; tone: BadgeTone }> = {
  expired: { label: 'หมดอายุแล้ว ต้องเก็บออก', tone: 'red' },
  out_of_stock: { label: 'หมด', tone: 'red' },
  near_expiry: { label: 'ใกล้หมดอายุ', tone: 'amber' },
  low_stock: { label: 'ใกล้หมด', tone: 'amber' },
};

export const orderStatusLabels: Record<PurchaseOrderStatus, string> = {
  ordered: 'สั่งแล้ว รอของ',
  partially_received: 'รับของบางส่วน',
  received: 'รับครบแล้ว',
  cancelled: 'ยกเลิกแล้ว',
};
