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
 * `POST /staff/prescriptions/{id}/dispense`. `stock_deducted` is false until the Inventory module
 * exists: the prescription is marked dispensed, but no stock is cut and nothing is billed.
 */
export interface DispenseResult {
  prescription: PrescriptionItem;
  label: DispenseLabel;
  stock_deducted: boolean;
}
