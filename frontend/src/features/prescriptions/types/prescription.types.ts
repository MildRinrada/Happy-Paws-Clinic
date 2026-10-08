/**
 * One prescription in a patient's medication history: `GET /staff/prescriptions?pet_id=`,
 * "ยาที่เคยจ่ายทั้งหมด, ตรวจซ้ำซ้อน".
 *
 * `duplicate_of` is the contract's "ตรวจซ้ำซ้อน": the ids of the patient's other prescriptions of
 * the same drug whose courses overlap this one, empty when there are none. The backend matches on
 * the drug name's first word, so two brand names for one molecule are not caught.
 */
export interface PrescriptionItem {
  id: string;
  version: number;
  pet_id: string;
  consultation_id: string;
  drug_name: string;
  dose: string;
  frequency: string;
  duration_days: number;
  quantity: number;
  instructions?: string;
  allergy_acknowledgement?: string;
  prescribed_at: string;
  prescribed_by_staff_id: string;
  course_ends_at: string;
  /** True while the course is still running. */
  active: boolean;
  dispensed_at?: string;
  dispensed_by_staff_id?: string;
  duplicate_of: string[];
  /**
   * The inventory item the drug is tied to: picked from stock when written (a contract extension,
   * docs/contract-extensions.md § 20), or matched by name at dispense. Absent for a typed drug not
   * yet dispensed, and for one that matched nothing.
   */
  inventory_item_id?: string;
  /** Once dispensed: whether the quantity came off the branch's stock. */
  stock_deducted?: boolean;
  /** Once dispensed: the unit price charged, in satang. Absent when nothing was charged. */
  charged_unit_price_satang?: number;
}

/** "พิมพ์ฉลาก": the lines on the label stuck to the bag, as data. The screen lays them out. */
export interface DispenseLabel {
  clinic_name: string;
  clinic_license_no?: string;
  branch_name: string;
  pet_name: string;
  hospital_number: string;
  owner_name: string;
  drug_name: string;
  dose: string;
  frequency: string;
  duration_days: number;
  quantity: number;
  instructions?: string;
  prescribed_by: string;
  prescriber_license_no?: string;
  /** A clinic calendar day, `YYYY-MM-DD`. */
  dispensed_on: string;
  prescription_id: string;
}

/**
 * What the dispense did to stock. `not_matched`: no inventory item has this drug's name, so nothing
 * was taken and nothing charged. `insufficient`: the item matched but the branch's books show fewer
 * usable units, so stock was left alone (the drug is still charged).
 */
export type DispenseStockStatus = 'deducted' | 'not_matched' | 'insufficient';

export interface DispenseStock {
  status: DispenseStockStatus;
  item_id?: string;
  item_name?: string;
  unit?: string;
  /** Usable units at the branch before the dispense. */
  available?: number;
  lots?: { lot_id: string; lot_number?: string; expires_on?: string; quantity: number }[];
}

/**
 * Where the medicine's charge went. `invoiced`: on the case's bill. `on_close`: the case is still
 * open, it lands on the bill its close raises. `separate_invoice`: the case's bill had already taken
 * money, so the medicine got a bill of its own.
 */
export interface DispenseCharge {
  placement: 'invoiced' | 'on_close' | 'separate_invoice';
  unit_price_satang: number;
  amount_satang: number;
  invoice_id?: string;
  invoice_number?: string;
}

/**
 * `POST /staff/prescriptions/{id}/dispense`: the prescription now dispensed, its label, what was
 * taken off the shelf, and what was charged. `charge` is absent when nothing was.
 */
export interface DispenseResult {
  prescription: PrescriptionItem;
  label: DispenseLabel;
  stock_deducted: boolean;
  stock: DispenseStock;
  charge?: DispenseCharge;
}
