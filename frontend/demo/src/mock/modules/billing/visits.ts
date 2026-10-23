import { nowIso, table, type Row } from '../../core/db';
import type { InvoiceLineInput } from '@/features/billing/types/billing.types';
import { addInvoiceLine, createInvoice, invoiceForVisit } from './invoices';
import { ensureInventory } from './catalogue';
import { availableAt, consumeStock, findItem } from './inventory';
import { items, type InvoiceRow, type ItemRow } from './store';

/**
 * Where a case meets billing and the stock room: the bill raised when a case closes
 * (`ConsultationBilling`) and the stock and charge of a dispense (`StockDispenser` and
 * `MedicineBilling`). The clinical module finds these by name among this module's exports.
 */

interface CaseLike {
  id: string;
  status: string;
  branch_id: string;
  customer_id: string;
  pet_id: string;
  appointment_id?: string;
  treatments: Array<{ id: string; service_id?: string; name: string; quantity: number; unit_price_satang: number }>;
}

/** A dispensed prescription's charge as a bill line: `medicine`, naming the prescription. */
function medicineLine(prescription: Row, item: ItemRow | undefined, unitPrice: number): InvoiceLineInput & { source_id: string } {
  return {
    kind: 'medicine',
    description: item ? `${item.name}${prescription.dose ? ` (${prescription.dose})` : ''}` : String(prescription.drug_name),
    quantity: Math.max(1, Number(prescription.quantity) || 1),
    unit_price_satang: unitPrice,
    source_id: prescription.id,
  };
}

/** The case's dispensed, priced prescriptions, read fresh from the clinical module's table. */
function chargedPrescriptions(consultationId: string): Row[] {
  return table('prescriptions')
    .where((row) => row.consultation_id === consultationId && row.dispensed_at && row.charged_unit_price_satang !== undefined && row.charged_unit_price_satang !== null)
    .sort((a, b) => String(a.dispensed_at).localeCompare(String(b.dispensed_at)));
}

/**
 * "ออกบิล" when a case closes: every treatment (a catalogue service as a `service` line, which a
 * package may cover; anything else as `treatment`) and the medicine already dispensed. Answers the
 * bill, the one already raised for the case, or null when there is nothing to charge.
 */
export function raiseConsultationInvoice(consultation: CaseLike, staffId: string, at = nowIso()): InvoiceRow | null {
  const existing = invoiceForVisit({ consultationId: consultation.id });
  if (existing) return existing;

  const medicine = chargedPrescriptions(consultation.id);
  if (consultation.treatments.length === 0 && medicine.length === 0) return null;

  const lines: Array<InvoiceLineInput & { source_id?: string }> = [
    ...consultation.treatments.map((treatment) =>
      treatment.service_id
        ? { kind: 'service' as const, description: treatment.name, quantity: treatment.quantity, unit_price_satang: treatment.unit_price_satang, source_id: treatment.service_id }
        : { kind: 'treatment' as const, description: treatment.name, quantity: treatment.quantity, unit_price_satang: treatment.unit_price_satang, source_id: treatment.id },
    ),
    ...medicine.map((row) => medicineLine(row, items().get(row.inventory_item_id), Number(row.charged_unit_price_satang))),
  ];

  return createInvoice({
    customerId: consultation.customer_id,
    branchId: consultation.branch_id,
    staffId,
    petId: consultation.pet_id,
    consultationId: consultation.id,
    appointmentId: consultation.appointment_id,
    lines,
    issuedAt: at,
  });
}

/** The stock item a prescription is for: the one it names, else the same name or first word. */
export function itemForPrescription(prescription: Row): ItemRow | undefined {
  const named = prescription.inventory_item_id ? items().get(prescription.inventory_item_id) : undefined;
  if (named) return named;
  const name = String(prescription.drug_name ?? '').trim().toLowerCase();
  if (!name) return undefined;
  const first = name.split(/\s+/)[0]!;
  const medicines = items().where((item) => item.kind === 'medicine' && item.active);
  return (
    medicines.find((item) => item.name.toLowerCase() === name) ??
    medicines.find((item) => item.name.toLowerCase().split(/\s+/)[0] === first) ??
    findItem(name)
  );
}

export interface DispenseOutcome {
  stock: {
    status: 'deducted' | 'not_matched' | 'insufficient';
    item_id?: string;
    item_name?: string;
    unit?: string;
    available?: number;
    lots?: { lot_id: string; lot_number?: string; expires_on?: string; quantity: number }[];
  };
  charge?: { placement: 'invoiced' | 'on_close' | 'separate_invoice'; unit_price_satang: number; amount_satang: number; invoice_id?: string; invoice_number?: string };
}

/**
 * "ตัดสต๊อก" and the charge at dispense. Stock comes off first-expiry-first; a dispense is never
 * refused for stock (the drug is in the vet's hand), it is reported `insufficient` and nothing is
 * taken. The charge, at the item's price now, waits for the close while the case is open, goes on
 * the case's open bill after it, or gets a bill of its own when that one has taken money.
 */
export function dispenseMedicine(input: { prescription: Row; branchId: string; staffId: string; consultation?: CaseLike }): DispenseOutcome {
  ensureInventory();
  const { prescription, branchId, staffId } = input;
  const item = itemForPrescription(prescription);
  if (!item) return { stock: { status: 'not_matched' } };

  const quantity = Math.max(1, Math.floor(Number(prescription.quantity) || 1));
  const available = availableAt(item.id, branchId);
  const base = { item_id: item.id, item_name: item.name, unit: item.unit };
  let stock: DispenseOutcome['stock'];
  if (available < quantity) {
    stock = { status: 'insufficient', ...base, available };
  } else {
    const written = consumeStock(item.id, quantity, `จ่ายยาตามใบสั่ง ${prescription.drug_name ?? ''}`.trim(), { branchId, staffId, prescriptionId: prescription.id, type: 'dispense' });
    stock = {
      status: 'deducted',
      ...base,
      available: available - quantity,
      lots: written.map((movement) => ({
        lot_id: movement.lot_id,
        ...(movement.lot_number ? { lot_number: movement.lot_number } : {}),
        ...(movement.expires_on ? { expires_on: movement.expires_on } : {}),
        quantity: -movement.quantity,
      })),
    };
  }

  const unitPrice = item.unit_price_satang;
  const amount = unitPrice * quantity;
  const consultation = input.consultation ?? (prescription.consultation_id ? (table('consultations').get(prescription.consultation_id) as CaseLike | undefined) : undefined);
  if (!consultation) return { stock };
  if (consultation.status !== 'closed') return { stock, charge: { placement: 'on_close', unit_price_satang: unitPrice, amount_satang: amount } };

  const line = medicineLine(prescription, item, unitPrice);
  const existing = invoiceForVisit({ consultationId: consultation.id });
  if (existing && existing.status === 'open') {
    const bill = addInvoiceLine(existing.id, line);
    return { stock, charge: { placement: 'invoiced', unit_price_satang: unitPrice, amount_satang: amount, invoice_id: bill.id, invoice_number: bill.number } };
  }
  const bill = createInvoice({
    customerId: consultation.customer_id,
    branchId: consultation.branch_id,
    staffId,
    petId: consultation.pet_id,
    ...(existing ? { note: `ค่ายาที่จ่ายหลังบิล ${existing.number} ของเคสนี้ปิดยอดแล้ว` } : { consultationId: consultation.id }),
    lines: [line],
  });
  return {
    stock,
    charge: { placement: existing ? 'separate_invoice' : 'invoiced', unit_price_satang: unitPrice, amount_satang: amount, invoice_id: bill.id, invoice_number: bill.number },
  };
}
