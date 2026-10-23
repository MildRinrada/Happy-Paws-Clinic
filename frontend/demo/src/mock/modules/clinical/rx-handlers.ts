import type { DispenseLabel, DispenseResult } from '@/features/prescriptions/types/prescription.types';
import { day, nowIso } from '../../core/db';
import { matches, notFound, page, requireStaff, route, stateConflict, type Ctx } from '../../core/http';
import { branches, clinicProfile, customerName, medicines, petOf, staffMembers, stockAt } from './links';
import { consultations, prescriptionItem, prescriptions, syncPrescriptions, type PrescriptionRow } from './store';
import { dispenseFromStock } from './visits';

/** The pharmacy counter: a patient's medication history, the dispense queue, the drug picker and the dispense. */

function visible(ctx: Ctx, branchId: string): boolean {
  const mine = ctx.auth?.branches ?? [];
  return mine.length === 0 || mine.includes(branchId);
}

route('GET', '/staff/prescriptions', (ctx) => {
  requireStaff(ctx, 'rx:read');
  const petId = ctx.query.get('pet_id');
  const dispensed = ctx.query.get('dispensed');
  if (petId && !petOf(petId)) notFound('สัตว์เลี้ยง');
  // One patient's whole history crosses branches; the clinic's list is the caller's branches only.
  const rows = prescriptions()
    .where(
      (row) =>
        (petId ? row.pet_id === petId : visible(ctx, row.branch_id)) &&
        (dispensed === null || dispensed === '' || (dispensed === 'true') === Boolean(row.dispensed_at)),
    )
    .sort((a, b) => b.prescribed_at.localeCompare(a.prescribed_at));
  const histories = new Map<string, typeof rows>();
  const historyOf = (id: string) => {
    if (!histories.has(id)) histories.set(id, prescriptions().where((row) => row.pet_id === id));
    return histories.get(id)!;
  };
  const answer = page(rows, ctx.query, 50);
  return { ...answer, data: answer.data.map((row) => prescriptionItem(row, historyOf(row.pet_id))) };
});

route('GET', '/staff/prescriptions/drug-options', (ctx) => {
  const auth = requireStaff(ctx, 'rx:write');
  const q = ctx.query.get('q') ?? '';
  const limit = Math.max(1, Math.min(50, Number(ctx.query.get('limit')) || 20));
  const asked = ctx.query.get('branch');
  const open = branches().where((row) => row.is_active !== false && !row.deleted_at);
  const branchId = asked || auth.branches[0] || open[0]?.id || '';
  const data = medicines()
    .filter((item) => matches(q, item.name))
    .sort((a, b) => String(a.name).localeCompare(String(b.name)))
    .slice(0, limit)
    .map((item) => {
      const stock = stockAt(item, branchId);
      return {
        item_id: item.id,
        name: item.name,
        unit: item.unit,
        unit_price_satang: Number(item.unit_price_satang ?? 0),
        available: stock.available,
        status: stock.status,
      };
    });
  return { branch_id: branchId, data };
});

route('POST', '/staff/prescriptions/:id/dispense', (ctx) => {
  const auth = requireStaff(ctx, 'rx:dispense');
  const row = prescriptions().get(ctx.params.id!);
  if (!row || !visible(ctx, row.branch_id)) notFound('ใบสั่งยา');
  // Once only: what stops one bag being handed out twice from two counters.
  if (row.dispensed_at) stateConflict('ใบสั่งยานี้จ่ายยาไปแล้ว');

  const at = nowIso();
  const consultation = consultations().get(row.consultation_id);
  const outcome = dispenseFromStock({ prescription: row, branchId: row.branch_id, staffId: auth.staffId, consultation });
  const updated = prescriptions().update(row.id, {
    dispensed_at: at,
    dispensed_by_staff_id: auth.staffId,
    stock_deducted: outcome.stock.status === 'deducted',
    ...(outcome.stock.item_id ? { inventory_item_id: outcome.stock.item_id } : {}),
    ...(outcome.charge ? { charged_unit_price_satang: outcome.charge.unit_price_satang } : {}),
  })!;
  syncPrescriptions(row.consultation_id);

  const result: DispenseResult = {
    prescription: prescriptionItem(updated),
    label: labelOf(updated),
    stock_deducted: outcome.stock.status === 'deducted',
    stock: outcome.stock,
    ...(outcome.charge ? { charge: outcome.charge } : {}),
  };
  return result;
});

/** "พิมพ์ฉลาก": the lines on the bag's label. */
function labelOf(row: PrescriptionRow): DispenseLabel {
  const clinic = clinicProfile();
  const pet = petOf(row.pet_id);
  const prescriber = staffMembers().get(row.prescribed_by_staff_id);
  return {
    clinic_name: clinic.name,
    ...(clinic.license_no ? { clinic_license_no: clinic.license_no } : {}),
    branch_name: branches().get(row.branch_id)?.name ?? row.branch_id,
    pet_name: pet?.name ?? row.pet_id,
    hospital_number: pet?.hospital_number ?? '',
    owner_name: customerName(row.customer_id) ?? customerName(pet?.primary_customer_id) ?? '',
    drug_name: row.drug_name,
    dose: row.dose,
    frequency: row.frequency,
    duration_days: row.duration_days,
    quantity: row.quantity,
    ...(row.instructions ? { instructions: row.instructions } : {}),
    prescribed_by: prescriber?.display_name ?? row.prescribed_by_staff_id,
    ...(prescriber?.license_no ? { prescriber_license_no: prescriber.license_no } : {}),
    dispensed_on: day(0),
    prescription_id: row.id,
  };
}
