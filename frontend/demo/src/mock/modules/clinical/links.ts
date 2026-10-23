import { table, type Row } from '../../core/db';
import { allergiesOf } from '../customers/store';

/**
 * What the clinical module reads from the rest of the clinic: patients, owners, staff, branches,
 * services, stock and the clinic's medical-record rules. Every one is someone else's table, read
 * and never written; the shapes are the owners' (README rule 3), read defensively because those
 * modules are written alongside this one.
 */

export const pets = () => table('pets');
export const customers = () => table('customers');
export const staffMembers = () => table('staff_members');
export const branches = () => table('branches');
export const services = () => table('services');
export const appointments = () => table('appointments');
export const queueTickets = () => table('queue_tickets');

export function petOf(petId: string | undefined): Row | undefined {
  return pets().get(petId);
}

export function customerName(customerId: string | undefined): string | undefined {
  return customers().get(customerId)?.display_name;
}

export function staffOfUser(userId: string): Row | undefined {
  return staffMembers().find((member) => member.user_id === userId);
}

export function vets(): Row[] {
  return staffMembers().where((member) => member.staff_type === 'veterinarian' && member.status !== 'deactivated');
}

/** `dog`, `cat` or `other`, whichever way the registry spells the species. */
export function speciesKind(species: string | undefined): 'dog' | 'cat' | 'other' {
  const value = (species ?? '').toLowerCase();
  if (/dog|สุนัข|หมา/.test(value)) return 'dog';
  if (/cat|แมว/.test(value)) return 'cat';
  return 'other';
}

/** The patient's recorded allergies, as the customers module keeps them. */
export { allergiesOf };

/** An allergy matches a drug when either name contains the other (`PetAllergy.Matches`). */
export function allergyMatches(substance: string, drugName: string): boolean {
  const allergy = substance.trim().toLowerCase();
  const drug = drugName.trim().toLowerCase();
  return Boolean(allergy && drug) && (drug.includes(allergy) || allergy.includes(drug));
}

export interface MedicalRecordRules {
  required_fields: string[];
  owner_visible_sections: string[];
  signer_must_be_case_vet: boolean;
}

const DEFAULT_RULES: MedicalRecordRules = {
  required_fields: [],
  owner_visible_sections: ['vitals', 'diagnosis', 'treatment', 'prescription', 'lab_result', 'attachments'],
  signer_must_be_case_vet: false,
};

/**
 * `PATCH /clinic/settings/medical-record` as the clinic module stores it. Looked for under the
 * names it is likely kept as; the product's defaults when none is there yet.
 */
export function medicalRecordRules(): MedicalRecordRules {
  for (const name of ['clinic_settings', 'medical_record_settings', 'clinic_medical_record']) {
    for (const row of table(name).all()) {
      const candidate = (row.medical_record ?? row.medicalRecord ?? row) as Partial<MedicalRecordRules>;
      if (Array.isArray(candidate.required_fields) || Array.isArray(candidate.owner_visible_sections)) {
        return { ...DEFAULT_RULES, ...candidate } as MedicalRecordRules;
      }
    }
  }
  return DEFAULT_RULES;
}

/** The clinic's name and licence for the drug label. */
export function clinicProfile(): { name: string; license_no?: string } {
  const row = table('clinic_profile').all()[0];
  return { name: row?.name ?? row?.display_name ?? 'Happy Paws Animal Clinic', license_no: row?.license_no };
}

/** Medicines in stock, as the inventory module lists them. */
export function medicines(): Row[] {
  return table('inventory_items').where((item) => item.kind === 'medicine' && item.active !== false);
}

/**
 * Usable units of an item at a branch: the billing module keeps stock as lots, so it is the sum of
 * the lots there not yet expired, judged against the reorder level as the stock list judges it.
 */
export function stockAt(item: Row, branchId: string): { available: number; status: 'ok' | 'low' | 'out' } {
  const today = new Date().toISOString().slice(0, 10);
  const lots = table('inventory_lots').where((lot) => lot.item_id === item.id && lot.branch_id === branchId);
  const branch = (item.branches as Row[] | undefined)?.find((stock) => stock.branch_id === branchId);
  const available = lots.length
    ? lots.filter((lot) => !lot.expires_on || lot.expires_on >= today).reduce((sum, lot) => sum + Number(lot.quantity_on_hand ?? 0), 0)
    : Number(branch?.available ?? 0);
  const status = available <= 0 ? 'out' : available <= Number(item.reorder_level ?? 0) ? 'low' : 'ok';
  return { available, status };
}

/** The stock item a typed drug name stands for: the same name, or the same first word. */
export function matchMedicine(drugName: string): Row | undefined {
  const name = drugName.trim().toLowerCase();
  const first = name.split(/\s+/)[0] ?? name;
  const list = medicines();
  return (
    list.find((item) => String(item.name).toLowerCase() === name) ??
    list.find((item) => String(item.name).toLowerCase().split(/\s+/)[0] === first)
  );
}
