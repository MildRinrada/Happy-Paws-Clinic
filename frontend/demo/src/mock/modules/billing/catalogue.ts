import { at, table, uuid } from '../../core/db';
import { addDays, branches, clinicToday, items, lots, movements, purchaseOrders, type ItemRow, type LotRow, type MovementRow } from './store';
import { nextOrderNumber } from './inventory';

/**
 * The clinic's stock room as the demo opens it: about sixty vaccines, drugs, foods and supplies,
 * with lots at each branch, thirty-odd days of receipts and use, a few items low or out, lots near
 * or past expiry, and purchase orders in every state.
 *
 * It is planted on first use rather than only by the seed, so the clinical module's seed (which
 * runs before this one) can already tie prescriptions to items with `findItem()` / `drugOptions()`.
 */

type Category = 'vaccine' | 'drug' | 'food' | 'supply';

interface Entry {
  name: string;
  category: Category;
  unit: string;
  /** Selling price in baht. */
  price: number;
  reorder: number;
  /** Units used per day at the main branch. */
  rate: number;
  /** How the shelf looks today: plenty, low, out; `near` adds a lot near expiry, `expired` a lot past it. */
  shape?: 'low' | 'out' | 'near' | 'expired' | 'idle';
}

const CATALOGUE: Entry[] = [
  { name: 'วัคซีนรวมสุนัข DHPPi+L (Nobivac)', category: 'vaccine', unit: 'โดส', price: 650, reorder: 15, rate: 2.2 },
  { name: 'วัคซีนพิษสุนัขบ้า Rabisin', category: 'vaccine', unit: 'โดส', price: 350, reorder: 15, rate: 2.5, shape: 'near' },
  { name: 'วัคซีนรวมแมว 3 โรค (Felocell 3)', category: 'vaccine', unit: 'โดส', price: 600, reorder: 12, rate: 1.6 },
  { name: 'วัคซีนรวมแมว 4 โรค (Purevax RCPCh)', category: 'vaccine', unit: 'โดส', price: 750, reorder: 10, rate: 0.9, shape: 'low' },
  { name: 'วัคซีนลิวคีเมียแมว (Purevax FeLV)', category: 'vaccine', unit: 'โดส', price: 900, reorder: 6, rate: 0.4 },
  { name: 'วัคซีนไข้หวัดสุนัข (Nobivac KC)', category: 'vaccine', unit: 'โดส', price: 550, reorder: 8, rate: 0.5, shape: 'expired' },
  { name: 'วัคซีนพิษสุนัขบ้า (Nobivac Rabies)', category: 'vaccine', unit: 'โดส', price: 350, reorder: 10, rate: 0.8 },
  { name: 'Amoxicillin 250 mg', category: 'drug', unit: 'เม็ด', price: 15, reorder: 200, rate: 28 },
  { name: 'Amoxicillin-Clavulanate 250 mg (Synulox)', category: 'drug', unit: 'เม็ด', price: 25, reorder: 150, rate: 22, shape: 'low' },
  { name: 'Doxycycline 100 mg', category: 'drug', unit: 'เม็ด', price: 12, reorder: 120, rate: 14 },
  { name: 'Enrofloxacin 50 mg (Baytril)', category: 'drug', unit: 'เม็ด', price: 20, reorder: 100, rate: 9 },
  { name: 'Metronidazole 200 mg', category: 'drug', unit: 'เม็ด', price: 8, reorder: 150, rate: 18, shape: 'near' },
  { name: 'Cephalexin 500 mg', category: 'drug', unit: 'แคปซูล', price: 12, reorder: 120, rate: 12 },
  { name: 'Meloxicam 1.5 mg/ml (Metacam) ยาน้ำ', category: 'drug', unit: 'ขวด', price: 450, reorder: 6, rate: 0.6 },
  { name: 'Carprofen 50 mg (Rimadyl)', category: 'drug', unit: 'เม็ด', price: 35, reorder: 100, rate: 10 },
  { name: 'Prednisolone 5 mg', category: 'drug', unit: 'เม็ด', price: 5, reorder: 200, rate: 20 },
  { name: 'Famotidine 20 mg', category: 'drug', unit: 'เม็ด', price: 6, reorder: 100, rate: 8 },
  { name: 'Omeprazole 20 mg', category: 'drug', unit: 'แคปซูล', price: 10, reorder: 80, rate: 6 },
  { name: 'Maropitant 16 mg (Cerenia)', category: 'drug', unit: 'เม็ด', price: 120, reorder: 20, rate: 1.4, shape: 'out' },
  { name: 'Ivermectin 1% ชนิดฉีด', category: 'drug', unit: 'ขวด', price: 380, reorder: 3, rate: 0.15 },
  { name: 'NexGard สุนัข 4-10 kg', category: 'drug', unit: 'เม็ด', price: 390, reorder: 24, rate: 2.4 },
  { name: 'NexGard Spectra สุนัข 7.5-15 kg', category: 'drug', unit: 'เม็ด', price: 480, reorder: 24, rate: 2.1, shape: 'low' },
  { name: 'Bravecto สุนัข 10-20 kg', category: 'drug', unit: 'เม็ด', price: 1350, reorder: 10, rate: 0.7 },
  { name: 'Revolution Plus แมว 2.5-5 kg', category: 'drug', unit: 'หลอด', price: 420, reorder: 18, rate: 1.5 },
  { name: 'Frontline Plus สุนัข 10-20 kg', category: 'drug', unit: 'หลอด', price: 290, reorder: 18, rate: 1.1 },
  { name: 'Drontal Plus ยาถ่ายพยาธิสุนัข', category: 'drug', unit: 'เม็ด', price: 90, reorder: 40, rate: 3.2 },
  { name: 'Milbemax ยาถ่ายพยาธิแมว', category: 'drug', unit: 'เม็ด', price: 120, reorder: 30, rate: 2.0, shape: 'near' },
  { name: 'Apoquel 5.4 mg', category: 'drug', unit: 'เม็ด', price: 65, reorder: 60, rate: 6 },
  { name: 'Cyclosporine 50 mg (Atopica)', category: 'drug', unit: 'แคปซูล', price: 95, reorder: 30, rate: 1.5 },
  { name: 'Furosemide 40 mg', category: 'drug', unit: 'เม็ด', price: 5, reorder: 100, rate: 6 },
  { name: 'Pimobendan 2.5 mg (Vetmedin)', category: 'drug', unit: 'เม็ด', price: 45, reorder: 60, rate: 4 },
  { name: "Ringer's Lactate 500 ml", category: 'drug', unit: 'ถุง', price: 120, reorder: 20, rate: 2.6 },
  { name: 'NSS 0.9% 500 ml', category: 'drug', unit: 'ถุง', price: 100, reorder: 20, rate: 2.2 },
  { name: 'Tobramycin ยาหยอดตา 0.3%', category: 'drug', unit: 'ขวด', price: 180, reorder: 6, rate: 0.5, shape: 'expired' },
  { name: 'Otomax ยาหยอดหู', category: 'drug', unit: 'หลอด', price: 420, reorder: 6, rate: 0.6 },
  { name: 'Gabapentin 100 mg', category: 'drug', unit: 'แคปซูล', price: 15, reorder: 80, rate: 5 },
  { name: 'Tramadol 50 mg', category: 'drug', unit: 'แคปซูล', price: 10, reorder: 80, rate: 7, shape: 'low' },
  { name: 'Vitamin B complex ชนิดฉีด', category: 'drug', unit: 'ขวด', price: 250, reorder: 3, rate: 0.2 },
  { name: 'Propofol 1% 20 ml', category: 'drug', unit: 'ขวด', price: 450, reorder: 5, rate: 0.5 },
  { name: 'Cefazolin 1 g ชนิดฉีด', category: 'drug', unit: 'ขวด', price: 160, reorder: 10, rate: 0.8 },
  { name: 'Royal Canin Gastrointestinal สุนัข 2 kg', category: 'food', unit: 'ถุง', price: 1250, reorder: 4, rate: 0.35 },
  { name: 'Royal Canin Renal แมว 2 kg', category: 'food', unit: 'ถุง', price: 1390, reorder: 4, rate: 0.3, shape: 'low' },
  { name: "Hill's i/d สุนัข กระป๋อง 370 g", category: 'food', unit: 'กระป๋อง', price: 135, reorder: 12, rate: 1.4 },
  { name: "Hill's c/d แมว 1.5 kg", category: 'food', unit: 'ถุง', price: 1150, reorder: 4, rate: 0.25 },
  { name: 'Royal Canin Recovery กระป๋อง 195 g', category: 'food', unit: 'กระป๋อง', price: 95, reorder: 24, rate: 2.2 },
  { name: 'Royal Canin Urinary S/O แมว 1.5 kg', category: 'food', unit: 'ถุง', price: 1050, reorder: 4, rate: 0.3 },
  { name: 'Royal Canin Hypoallergenic สุนัข 2 kg', category: 'food', unit: 'ถุง', price: 1350, reorder: 3, rate: 0.15, shape: 'idle' },
  { name: 'Pro Plan Veterinary HA แมว 1.3 kg', category: 'food', unit: 'ถุง', price: 990, reorder: 3, rate: 0.15 },
  { name: 'ไซริงค์ 3 ml', category: 'supply', unit: 'ชิ้น', price: 5, reorder: 200, rate: 24 },
  { name: 'เข็มฉีดยา 23G', category: 'supply', unit: 'ชิ้น', price: 3, reorder: 200, rate: 26 },
  { name: 'ถุงมือไนไตร (กล่อง 100 ชิ้น)', category: 'supply', unit: 'กล่อง', price: 250, reorder: 6, rate: 0.5 },
  { name: 'ผ้าก๊อซ 3x3 นิ้ว (ห่อ 10 ชิ้น)', category: 'supply', unit: 'ห่อ', price: 30, reorder: 50, rate: 5 },
  { name: 'คอลลาร์กันเลีย ไซซ์ M', category: 'supply', unit: 'ชิ้น', price: 180, reorder: 8, rate: 0.5 },
  { name: 'คอลลาร์กันเลีย ไซซ์ S', category: 'supply', unit: 'ชิ้น', price: 150, reorder: 8, rate: 0.6, shape: 'out' },
  { name: 'IV Catheter 22G', category: 'supply', unit: 'ชิ้น', price: 45, reorder: 40, rate: 3 },
  { name: 'ชุดให้น้ำเกลือ', category: 'supply', unit: 'ชุด', price: 60, reorder: 30, rate: 2.5 },
  { name: 'ผ้าพันแผล Vetrap 4 นิ้ว', category: 'supply', unit: 'ม้วน', price: 90, reorder: 20, rate: 1.2 },
  { name: 'แผ่นรองซับ (ห่อ 10 แผ่น)', category: 'supply', unit: 'ห่อ', price: 120, reorder: 10, rate: 0.8 },
  { name: 'ไหมเย็บ Nylon 3-0', category: 'supply', unit: 'ซอง', price: 85, reorder: 20, rate: 0.9 },
  { name: 'ชุดตรวจ FIV/FeLV (SNAP Combo)', category: 'supply', unit: 'ชุด', price: 650, reorder: 10, rate: 0.6, shape: 'near' },
  { name: 'ชุดตรวจพยาธิเม็ดเลือด 4Dx', category: 'supply', unit: 'ชุด', price: 850, reorder: 10, rate: 0.9 },
  { name: 'ชุดตรวจพาร์โว (Parvo Ag)', category: 'supply', unit: 'ชุด', price: 450, reorder: 8, rate: 0.4 },
  // What the vets' usual cases prescribe besides the above (clinical/cases.ts), so a dispense finds its shelf.
  { name: 'Robenacoxib 6 mg (Onsior)', category: 'drug', unit: 'เม็ด', price: 45, reorder: 30, rate: 1.2 },
  { name: 'Metoclopramide น้ำ 1 mg/ml', category: 'drug', unit: 'ขวด', price: 120, reorder: 5, rate: 0.3 },
  { name: 'Mirtazapine เจลทาใบหู (Mirataz)', category: 'drug', unit: 'หลอด', price: 950, reorder: 4, rate: 0.2 },
  { name: 'L-Lysine เจลสำหรับแมว', category: 'drug', unit: 'หลอด', price: 320, reorder: 6, rate: 0.3 },
  { name: 'Dextromethorphan 15 mg ยาแก้ไอ', category: 'drug', unit: 'เม็ด', price: 4, reorder: 60, rate: 3 },
  { name: 'โพรไบโอติกสำหรับสัตว์ (FortiFlora)', category: 'drug', unit: 'ซอง', price: 35, reorder: 40, rate: 2.5 },
  { name: 'Epi-Otic น้ำยาล้างหู 125 ml', category: 'drug', unit: 'ขวด', price: 450, reorder: 6, rate: 0.4 },
  { name: 'Chlorhexidine 2% แชมพูยา 250 ml', category: 'drug', unit: 'ขวด', price: 380, reorder: 6, rate: 0.3 },
  { name: 'Glucosamine อาหารเสริมบำรุงข้อ', category: 'drug', unit: 'เม็ด', price: 12, reorder: 90, rate: 4 },
  { name: 'Critical Care อาหารเหลวสัตว์ป่วย 141 g', category: 'food', unit: 'ถุง', price: 690, reorder: 4, rate: 0.2 },
];

export const SUPPLIERS = ['บริษัท สัตวแพทย์ฟาร์มา จำกัด', 'บริษัท เวทซัพพลาย (ประเทศไทย) จำกัด', 'บริษัท เพ็ทฟู้ด ดิสทริบิวชั่น จำกัด'];
const supplierFor = (category: Category) => (category === 'food' ? SUPPLIERS[2]! : category === 'supply' ? SUPPLIERS[1]! : SUPPLIERS[0]!);

/** A small repeatable random series, so each day's seed looks the same on a reload. */
export function seeded(seed: number) {
  let state = seed >>> 0 || 1;
  return () => {
    state = (state * 1664525 + 1013904223) >>> 0;
    return state / 4294967296;
  };
}

function lotNumber(random: () => number, prefix: string) {
  return `${prefix}${String(Math.floor(random() * 90000) + 10000)}`;
}

let planting = false;

/** Plants the stock room once, when the first branch exists. Safe to call from anywhere, any time. */
export function ensureInventory(): void {
  if (planting || items().count() > 0) return;
  const open = branches();
  if (!open.length) return;
  planting = true;
  try {
    plant(open.map((branch) => branch.id));
  } finally {
    planting = false;
  }
}

function storekeeper(): string {
  const members = table('staff_members').all();
  return (members.find((member) => (member.roles as string[] | undefined)?.includes('clinic_owner')) ?? members[0])?.id ?? '';
}

function plant(branchIds: string[]) {
  const random = seeded(4242);
  const today = clinicToday();
  const staffId = storekeeper();
  const [main] = branchIds;

  const rows = items().insertMany(
    CATALOGUE.map<Omit<ItemRow, 'id'>>((entry) => ({
      version: 1,
      name: entry.name,
      kind: entry.category === 'vaccine' || entry.category === 'drug' ? 'medicine' : 'supply',
      unit: entry.unit,
      unit_price_satang: entry.price * 100,
      reorder_level: entry.reorder,
      created_at: at(-420, '10:00'),
      active: true,
      category: entry.category,
      cost_satang: Math.round(entry.price * 100 * (entry.category === 'food' ? 0.72 : 0.58)),
    })),
  );

  const lotRows: LotRow[] = [];
  const moveRows: MovementRow[] = [];
  const receivedFor = new Map<string, { lotId: string; quantity: number; at: string }>();

  rows.forEach((item, index) => {
    const entry = CATALOGUE[index]!;
    branchIds.forEach((branchId, branchIndex) => {
      // The second branch is smaller: half the catalogue, a third of the use.
      if (branchIndex > 0 && index % 2 === 1) return;
      const scale = branchIndex === 0 ? 1 : 0.35;
      const rate = entry.shape === 'idle' ? 0 : entry.rate * scale;
      const shape = branchIndex === 0 ? entry.shape : undefined;
      const prefix = entry.category === 'vaccine' ? 'V' : entry.category === 'food' ? 'F' : entry.category === 'supply' ? 'S' : 'L';
      const events: Array<Omit<MovementRow, 'id' | 'balance_after'>> = [];

      // Use over the last month, written as one movement per few days: what the ledger needs for
      // the forecast's averages without filling the browser's storage.
      const step = branchIndex > 0 ? 14 : rate >= 3 ? 7 : 11;
      const batched: Array<{ day: number; quantity: number }> = [];
      for (let back = 30 - Math.floor(random() * 3); back >= 1; back -= step) {
        const quantity = Math.round(rate * step * (0.7 + random() * 0.6));
        if (quantity > 0) batched.push({ day: -back, quantity });
      }
      const used = batched.reduce((total, use) => total + use.quantity, 0);

      const remaining =
        shape === 'out' ? 0 : shape === 'low' ? Math.max(1, Math.floor(entry.reorder * 0.6)) : Math.ceil(entry.reorder * (2 + random() * 2.5) * scale) + 2;
      const farExpiry = addDays(today, entry.category === 'food' ? 200 + Math.floor(random() * 120) : 300 + Math.floor(random() * 300));

      const mainLot: LotRow = {
        id: uuid(),
        item_id: item.id,
        branch_id: branchId,
        lot_number: lotNumber(random, prefix),
        expires_on: farExpiry,
        quantity_on_hand: remaining,
        received_at: at(-34, '10:30'),
      };
      lotRows.push(mainLot);
      receivedFor.set(`${item.id}:${branchId}`, { lotId: mainLot.id, quantity: remaining + used, at: mainLot.received_at });
      events.push({
        type: branchIndex === 0 ? 'receive' : 'transfer_in',
        item_id: item.id,
        branch_id: branchId,
        lot_id: mainLot.id,
        lot_number: mainLot.lot_number,
        expires_on: mainLot.expires_on,
        quantity: remaining + used,
        occurred_at: mainLot.received_at,
        recorded_by_staff_id: staffId,
        ...(branchIndex === 0 ? { unit_cost_satang: item.cost_satang, reference: `INV-SUP-${String(4100 + index)}` } : { note: 'โอนจากสาขาหลัก' }),
      });
      for (const use of batched) {
        events.push({
          type: entry.category === 'supply' || entry.category === 'food' ? 'issue' : 'dispense',
          item_id: item.id,
          branch_id: branchId,
          lot_id: mainLot.id,
          lot_number: mainLot.lot_number,
          quantity: -use.quantity,
          occurred_at: at(use.day, `${String(10 + Math.floor(random() * 8)).padStart(2, '0')}:${random() < 0.5 ? '15' : '45'}`),
          recorded_by_staff_id: staffId,
          ...(entry.category === 'food' ? { note: 'ขายหน้าร้าน' } : entry.category === 'supply' ? { note: 'เบิกใช้ในห้องตรวจ' } : {}),
        });
      }

      if (shape === 'near' || shape === 'expired') {
        const expires = shape === 'near' ? addDays(today, 18 + Math.floor(random() * 40)) : addDays(today, -(3 + Math.floor(random() * 10)));
        const quantity = shape === 'near' ? Math.max(3, Math.ceil(entry.reorder * 0.5)) : Math.max(2, Math.ceil(entry.reorder * 0.2));
        const old: LotRow = {
          id: uuid(),
          item_id: item.id,
          branch_id: branchId,
          lot_number: lotNumber(random, prefix),
          expires_on: expires,
          quantity_on_hand: quantity,
          received_at: at(shape === 'near' ? -120 : -330, '11:00'),
        };
        lotRows.push(old);
        events.push({
          type: 'receive',
          item_id: item.id,
          branch_id: branchId,
          lot_id: old.id,
          lot_number: old.lot_number,
          expires_on: old.expires_on,
          quantity,
          occurred_at: old.received_at,
          recorded_by_staff_id: staffId,
          unit_cost_satang: item.cost_satang,
        });
      }

      // The running balance of the item at the branch, in time order, as the ledger keeps it.
      let balance = 0;
      for (const event of events.sort((a, b) => a.occurred_at.localeCompare(b.occurred_at))) {
        balance += event.quantity;
        moveRows.push({ ...event, id: uuid(), balance_after: balance } as MovementRow);
      }
    });
  });

  // A count last week that found two syringes short.
  const syringe = rows.find((item) => item.name.startsWith('ไซริงค์'));
  const syringeLot = syringe && lotRows.find((lot) => lot.item_id === syringe.id && lot.branch_id === main);
  if (syringe && syringeLot) {
    syringeLot.quantity_on_hand = Math.max(0, syringeLot.quantity_on_hand - 2);
    const balance = lotRows.filter((lot) => lot.item_id === syringe.id && lot.branch_id === main).reduce((total, lot) => total + lot.quantity_on_hand, 0);
    moveRows.push({
      id: uuid(),
      type: 'adjustment',
      item_id: syringe.id,
      branch_id: main!,
      lot_id: syringeLot.id,
      lot_number: syringeLot.lot_number,
      expires_on: syringeLot.expires_on,
      quantity: -2,
      balance_after: balance,
      occurred_at: at(-1, '18:30'),
      recorded_by_staff_id: staffId,
      note: 'นับสต๊อกประจำสัปดาห์ พบขาด 2 ชิ้น',
    });
  }

  lots().insertMany(lotRows);
  movements().insertMany(moveRows);
  plantOrders(rows, main!, staffId, receivedFor);
}

function plantOrders(rows: ItemRow[], branchId: string, staffId: string, receivedFor: Map<string, { lotId: string; quantity: number; at: string }>) {
  const today = clinicToday();
  const pick = (start: string) => rows.find((item) => item.name.startsWith(start))!;
  const line = (item: ItemRow, quantity: number, received: number, cancelled = 0) => ({
    id: uuid(),
    item_id: item.id,
    item_name: item.name,
    unit: item.unit,
    quantity,
    received_quantity: received,
    outstanding: quantity - received - cancelled,
    unit_cost_satang: item.cost_satang ?? 0,
    line_total_satang: quantity * (item.cost_satang ?? 0),
    cancelled_quantity: cancelled,
  });
  const order = (
    daysAgo: number,
    supplier: string,
    status: 'ordered' | 'partially_received' | 'received' | 'cancelled',
    lines: ReturnType<typeof line>[],
    extra: Record<string, unknown> = {},
  ) => {
    const orderedAt = at(-daysAgo, '09:40');
    const row = purchaseOrders().insert({
      version: 1,
      number: nextOrderNumber(orderedAt),
      branch_id: branchId,
      supplier_name: supplier,
      status,
      ordered_at: orderedAt,
      ordered_by_staff_id: staffId,
      total_cost_satang: lines.reduce((total, entry) => total + entry.line_total_satang, 0),
      lines,
      received_cost_satang: lines.reduce((total, entry) => total + entry.received_quantity * entry.unit_cost_satang, 0),
      ...extra,
    });
    return row;
  };

  // Delivered in full when the shelves were last filled: the opening receipts name it.
  const delivered = [pick('Amoxicillin 250'), pick('Doxycycline'), pick('Prednisolone')].filter(Boolean);
  const full = order(
    36,
    SUPPLIERS[0]!,
    'received',
    delivered.map((item) => {
      const received = receivedFor.get(`${item.id}:${branchId}`)!;
      return line(item, received.quantity, received.quantity);
    }),
    { expected_on: addDays(today, -34), note: 'สั่งเติมสต๊อกประจำเดือน' },
  );
  for (const item of delivered) {
    const received = receivedFor.get(`${item.id}:${branchId}`)!;
    const movement = movements().find((row) => row.lot_id === received.lotId && row.type === 'receive');
    if (movement) movements().update(movement.id, { purchase_order_id: full.id });
  }

  // Waiting for the low items, due in a few days.
  const synulox = pick('Amoxicillin-Clavulanate');
  const spectra = pick('NexGard Spectra');
  const tramadol = pick('Tramadol');
  order(2, SUPPLIERS[0]!, 'ordered', [line(synulox, 300, 0), line(spectra, 48, 0), line(tramadol, 200, 0)], { expected_on: addDays(today, 3), note: 'ด่วน ของใกล้หมด' });

  // Food arrived in part; the rest is on back order.
  const renal = pick('Royal Canin Renal');
  const gi = pick('Royal Canin Gastrointestinal');
  order(9, SUPPLIERS[2]!, 'partially_received', [line(renal, 10, 0), line(gi, 8, 8)], { expected_on: addDays(today, 5), note: 'Renal ค้างส่ง ซัพพลายเออร์แจ้งของเข้าสัปดาห์หน้า' });

  // Called off: the clinic found a better price.
  const collar = pick('คอลลาร์กันเลีย ไซซ์ S');
  order(15, SUPPLIERS[1]!, 'cancelled', [line(collar, 30, 0, 30)], {
    expected_on: addDays(today, -8),
    cancelled_at: at(-12, '14:10'),
    cancelled_by_staff_id: staffId,
    cancel_reason: 'ได้ราคาที่ดีกว่าจากผู้ขายอื่น',
  });
}
