import { at, table, type Row } from '../../core/db';
import { DEMO_ACCOUNTS } from '../../core/demo-accounts';
import type { InvoiceLineInput, PaymentMethod } from '@/features/billing/types/billing.types';
import { ensureInventory, seeded } from './catalogue';
import { addDiscount, createInvoice, giveRefund, invoiceForVisit, issueReceipt, takePayment, voidBill } from './invoices';
import { itemForPrescription } from './visits';
import { branches, clinicDayOf, clinicToday, invoices, isVet, items, petOf, serviceOf, type InvoiceRow, type ItemRow } from './store';

/**
 * Bills for the visits the other seeds planted, and the stock room.
 *
 * Every completed appointment gets its bill (or the one the clinical seed already raised for its
 * case is settled): most paid, some with a receipt, a few partly paid or still owed, one refunded,
 * one voided. Today's finished visits are partly still waiting at the counter. Older months get a
 * thinner history of visits and counter sales, so the revenue and retention reports have a past.
 * The demo customer has paid bills and exactly one outstanding. Nothing here hard-codes another
 * module's id: everything is looked up, and an empty table only means fewer bills.
 */
export const order = 40;

type Line = InvoiceLineInput & { source_id?: string };

interface Plan {
  at: string;
  customerId: string;
  petId?: string;
  branchId: string;
  staffId: string;
  lines: Line[];
  appointmentId?: string;
  consultationId?: string;
  existing?: InvoiceRow;
  today: boolean;
  note?: string;
}

export function seed() {
  ensureInventory();
  const open = branches();
  if (!open.length) return;

  const random = seeded(Number(clinicToday().replace(/-/g, '')) % 100_000);
  const pick = <T>(list: T[]): T => list[Math.floor(random() * list.length)]!;
  const chance = (p: number) => random() < p;

  const staff = table('staff_members').where((member) => member.status !== 'deactivated');
  const foundOwner = staff.find((member) => member.user_id === DEMO_ACCOUNTS.owner.user_id) ?? staff[0];
  if (!foundOwner) return;
  const owner: Row = foundOwner;
  const desk: Row = staff.find((member) => member.user_id === DEMO_ACCOUNTS.reception.user_id) ?? staff.find((member) => /reception|cashier/.test(String(member.staff_type))) ?? owner;
  const vets = staff.filter(isVet);

  const stock = items().all();
  const item = (start: string) => stock.find((row) => row.name.startsWith(start));
  const medicine = (row: ItemRow | undefined, quantity: number): Line[] =>
    row ? [{ kind: 'medicine', description: row.name, quantity, unit_price_satang: row.unit_price_satang }] : [];
  const product = (row: ItemRow | undefined, quantity = 1): Line[] =>
    row ? [{ kind: row.kind === 'medicine' ? 'medicine' : 'other', description: row.name, quantity, unit_price_satang: row.unit_price_satang }] : [];

  const antibiotics = ['Amoxicillin 250', 'Amoxicillin-Clavulanate', 'Doxycycline', 'Cephalexin', 'Metronidazole', 'Enrofloxacin'];
  const supportive = ['Prednisolone', 'Famotidine', 'Omeprazole', 'Carprofen', 'Apoquel', 'Gabapentin'];

  /** What a visit for this service usually adds to its bill besides the service itself. */
  function extrasFor(service: Row | undefined, pet: Row | undefined): Line[] {
    const isCat = /cat|แมว/i.test(String(pet?.species ?? ''));
    switch (service?.category_code) {
      case 'vaccine':
        return chance(0.4) ? product(item(isCat ? 'Milbemax' : pick(['Drontal Plus', 'NexGard สุนัข', 'Frontline']))) : [];
      case 'checkup': {
        const lines: Line[] = [];
        if (chance(0.6)) lines.push(...medicine(item(pick(antibiotics)), pick([10, 14, 14, 20])));
        if (chance(0.35)) lines.push(...medicine(item(pick(supportive)), pick([7, 10, 14])));
        if (chance(0.3)) lines.push({ kind: 'treatment', description: pick(['ฉีดยาลดอักเสบ', 'ฉีดยาแก้อาเจียน', 'ทำแผลและล้างแผล', 'ให้สารน้ำใต้ผิวหนัง']), quantity: 1, unit_price_satang: pick([25000, 30000, 35000, 45000]) });
        return lines;
      }
      case 'dental':
        return [...medicine(item('Amoxicillin-Clavulanate'), 14), ...medicine(item('Carprofen'), 7), { kind: 'treatment', description: 'ยาสลบและเฝ้าระวังระหว่างทำฟัน', quantity: 1, unit_price_satang: 120000 }];
      case 'surgery':
        return [
          { kind: 'treatment', description: 'ยาสลบและเฝ้าระวังระหว่างผ่าตัด', quantity: 1, unit_price_satang: 150000 },
          { kind: 'treatment', description: 'ค่าวัสดุสิ้นเปลืองห้องผ่าตัด', quantity: 1, unit_price_satang: 35000 },
          ...medicine(item('Cephalexin'), 14),
          ...medicine(item('Carprofen'), 7),
          ...product(item(isCat ? 'คอลลาร์กันเลีย ไซซ์ S' : 'คอลลาร์กันเลีย ไซซ์ M')),
        ];
      case 'lab':
        return chance(0.5) ? product(item(isCat ? 'ชุดตรวจ FIV' : 'ชุดตรวจพยาธิเม็ดเลือด')) : [];
      default:
        return [];
    }
  }

  function serviceLine(service: Row | undefined): Line[] {
    return service ? [{ kind: 'service', description: String(service.name), quantity: 1, unit_price_satang: Number(service.price_satang) || 0, source_id: service.id }] : [];
  }

  /**
   * A closed case's bill, as its close would have raised it: its treatments (the first one billed as
   * the visit's service, so the service report can count it) and the dispensed medicine tied to a
   * stock item, at the price it was charged at. Unmatched drugs are not charged, as in the product.
   */
  function caseLines(consultation: Row, service: Row | undefined): Line[] {
    const treatments = (consultation.treatments as Row[] | undefined) ?? [];
    const lines: Line[] = treatments.map((treatment, index) =>
      treatment.service_id || (index === 0 && service)
        ? { kind: 'service', description: String(treatment.name), quantity: Number(treatment.quantity) || 1, unit_price_satang: Number(treatment.unit_price_satang) || 0, source_id: (treatment.service_id as string | undefined) ?? service!.id }
        : { kind: 'treatment', description: String(treatment.name), quantity: Number(treatment.quantity) || 1, unit_price_satang: Number(treatment.unit_price_satang) || 0, source_id: treatment.id },
    );
    for (const prescription of table('prescriptions').where((row) => row.consultation_id === consultation.id && row.dispensed_at)) {
      const stocked = itemForPrescription(prescription);
      if (!stocked) continue;
      // The price the dispense recorded (the clinical seed names the item and its price), else the shelf's.
      const price = Number(prescription.charged_unit_price_satang ?? stocked.unit_price_satang);
      lines.push({ kind: 'medicine', description: stocked.name, quantity: Math.max(1, Number(prescription.quantity) || 1), unit_price_satang: price, source_id: prescription.id });
    }
    return lines.length ? lines : serviceLine(service);
  }

  const plans: Plan[] = [];
  const today = clinicToday();

  // ---- Bills for completed visits (the appointments seed's) ----
  const visits = table('appointments')
    .where((row) => row.status === 'completed')
    .sort((a, b) => String(a.starts_at).localeCompare(String(b.starts_at)));
  for (const visit of visits) {
    const consultation = table('consultations').find((row) => row.appointment_id === visit.id);
    const existing = invoiceForVisit({ appointmentId: visit.id, consultationId: consultation?.id });
    const service = serviceOf(visit.service_id);
    const lines = existing ? [] : consultation?.status === 'closed' ? caseLines(consultation, service) : [...serviceLine(service), ...extrasFor(service, petOf(visit.pet_id))];
    if (!existing && !lines.length) continue;
    plans.push({
      at: existing?.issued_at ?? (consultation?.status === 'closed' ? consultation.closed_at : visit.ends_at),
      customerId: visit.customer_id,
      petId: visit.pet_id,
      branchId: visit.branch_id,
      staffId: visit.staff_id ?? desk.id,
      lines,
      appointmentId: visit.id,
      consultationId: consultation?.status === 'closed' ? consultation.id : undefined,
      existing,
      today: clinicDayOf(visit.starts_at) === today,
    });
  }

  // ---- Cases seen without a booking (the demo customer's older illnesses, yesterday's lab cases) ----
  for (const consultation of table('consultations').where((row) => row.status === 'closed' && !row.appointment_id && row.closed_at)) {
    if (invoiceForVisit({ consultationId: consultation.id })) continue;
    const lines = caseLines(consultation, undefined);
    if (!lines.length) continue;
    plans.push({
      at: consultation.closed_at,
      customerId: consultation.customer_id,
      petId: consultation.pet_id,
      branchId: consultation.branch_id,
      staffId: consultation.vet_staff_id ?? desk.id,
      lines,
      consultationId: consultation.id,
      today: clinicDayOf(consultation.opened_at) === today,
    });
  }

  // ---- Older months, and counter sales ----
  const customers = table('customers').where((row) => row.is_active !== false && !row.merged_into_customer_id && !row.deleted_at);
  const petsOf = (customerId: string) => table('pets').where((pet) => pet.primary_customer_id === customerId && !pet.is_deceased);
  const regulars = customers.filter((customer) => petsOf(customer.id).length > 0);
  // No stray bill after someone's last visit on the calendar: the CRM reads a paid bill as a visit,
  // and the customers the appointments seed has stop coming must stay lapsed.
  const lastSeen = new Map<string, string>();
  for (const row of table('appointments').where((visit) => !['cancelled', 'no_show'].includes(visit.status))) {
    if ((lastSeen.get(row.customer_id) ?? '') < row.starts_at) lastSeen.set(row.customer_id, row.starts_at);
  }
  const regularsOn = (iso: string) => regulars.filter((customer) => !lastSeen.has(customer.id) || lastSeen.get(customer.id)! >= iso);
  const services = table('services').where((row) => row.is_active !== false && ['checkup', 'vaccine', 'lab', 'grooming'].includes(row.category_code));
  if (regulars.length && services.length) {
    for (let back = 150; back >= 31; back -= 1) {
      if (!chance(0.28)) continue;
      const when = at(-back, `${10 + Math.floor(random() * 8)}:${pick(['05', '20', '35', '50'])}`);
      const around = regularsOn(when);
      if (!around.length) continue;
      const customer = pick(around);
      const pet = pick(petsOf(customer.id));
      const service = pick(services);
      plans.push({
        at: when,
        customerId: customer.id,
        petId: pet.id,
        branchId: chance(0.7) ? open[0]!.id : pick(open).id,
        staffId: vets.length ? pick(vets).id : desk.id,
        lines: [...serviceLine(service), ...extrasFor(service, pet)],
        today: false,
      });
    }
  }
  const foods = stock.filter((row) => row.category === 'food');
  if (regulars.length && foods.length) {
    for (let back = 29; back >= 1; back -= 1) {
      if (!chance(0.4)) continue;
      const food = pick(foods);
      const when = at(-back, `${11 + Math.floor(random() * 7)}:${pick(['10', '25', '40', '55'])}`);
      const around = regularsOn(when);
      if (!around.length) continue;
      plans.push({
        at: when,
        customerId: pick(around).id,
        branchId: open[0]!.id,
        staffId: desk.id,
        lines: product(food, chance(0.3) ? 2 : 1),
        today: false,
        note: 'ขายหน้าร้าน',
      });
    }
  }

  // ---- Write them in time order, so the running numbers follow the calendar ----
  plans.sort((a, b) => a.at.localeCompare(b.at));
  const minutesAfter = (iso: string, minutes: number) => new Date(Date.parse(iso) + minutes * 60_000).toISOString();
  const now = Date.now();
  const tender = (): PaymentMethod => (random() < 0.42 ? 'promptpay' : random() < 0.6 ? 'cash' : 'card');
  const reference = (method: PaymentMethod) => (method === 'cash' ? undefined : method === 'card' ? `APPR${Math.floor(100000 + random() * 899999)}` : `PP${Math.floor(10_000_000 + random() * 89_999_999)}`);

  for (const plan of plans) {
    try {
      settle(plan);
    } catch (error) {
      console.warn('[mock] billing seed skipped a bill', error);
    }
  }

  function settle(plan: Plan) {
    let invoice =
      plan.existing ??
      createInvoice({
        customerId: plan.customerId,
        branchId: plan.branchId,
        staffId: plan.staffId,
        petId: plan.petId,
        appointmentId: plan.appointmentId,
        consultationId: plan.consultationId,
        note: plan.note,
        lines: plan.lines,
        issuedAt: plan.at,
        usePackages: false,
      });
    if (invoice.payments.length || invoice.status === 'void') return;

    const paidAt = minutesAfter(invoice.issued_at, 4 + Math.floor(random() * 15));
    if (plan.today) {
      // Today: the visits that ended a while ago have paid; the latest are still at the counter.
      if (Date.parse(paidAt) > now - 20 * 60_000 || chance(0.35)) return;
    }

    if (chance(0.07) && invoice.subtotal_satang >= 50000) {
      const percent = pick([5, 10]);
      invoice = addDiscount(invoice, {
        amountSatang: Math.floor((invoice.subtotal_satang * percent) / 100),
        percent,
        reason: pick(['ลูกค้าประจำ', 'ส่วนลดสมาชิกบ้านหลายตัว', 'ชดเชยรอคิวนาน']),
        approvedBy: owner.id,
        appliedBy: desk.id,
        at: paidAt,
      });
    }

    const roll = random();
    if (!plan.today && roll < 0.025) return; // still owed
    if (!plan.today && roll < 0.055 && invoice.total_satang >= 60000) {
      const part = Math.floor((invoice.total_satang * (0.4 + random() * 0.2)) / 10000) * 10000;
      if (part > 0) invoice = takePayment(invoice, { method: 'cash', amountSatang: part, staffId: desk.id, at: paidAt });
      return;
    }
    if (chance(0.06) && invoice.total_satang >= 80000) {
      const first = Math.floor(invoice.total_satang / 2 / 10000) * 10000;
      invoice = takePayment(invoice, { method: 'cash', amountSatang: first, staffId: desk.id, at: paidAt });
      const method = pick<PaymentMethod>(['promptpay', 'card']);
      invoice = takePayment(invoice, { method, amountSatang: invoice.balance_satang, reference: reference(method), staffId: desk.id, at: minutesAfter(paidAt, 1) });
    } else if (invoice.total_satang > 0) {
      const method = tender();
      invoice = takePayment(invoice, { method, amountSatang: invoice.balance_satang, reference: reference(method), staffId: desk.id, at: paidAt });
    }
    if (invoice.payments.length && chance(0.72)) invoice = issueReceipt(invoice, undefined, { at: minutesAfter(paidAt, 1), notify: false }).invoice;
  }

  shapeDemoCustomer({ desk: desk.id, vets: vets.map((vet) => vet.id), branchId: open[0]!.id, serviceLine, services, extrasFor, random });
  plantExceptions(desk.id, owner.id);
}

/** One refund and one voided bill, so those states can be opened from the list. */
function plantExceptions(deskId: string, ownerId: string) {
  const past = invoices()
    .where((invoice) => invoice.status === 'paid' && invoice.issued_at < at(-3, '00:00') && invoice.lines.some((line) => line.kind === 'medicine'))
    .sort((a, b) => b.issued_at.localeCompare(a.issued_at));
  const refundable = past[2] ?? past[0];
  if (refundable) {
    const line = refundable.lines.find((entry) => entry.kind === 'medicine')!;
    giveRefund(refundable, {
      amountSatang: Math.min(line.line_total_satang, refundable.paid_satang),
      reason: `ลูกค้าคืน ${line.description} ที่ยังไม่ได้เปิดใช้`,
      paymentId: refundable.payments[0]!.id,
      staffId: ownerId,
      at: new Date(Date.parse(refundable.issued_at) + 26 * 3_600_000).toISOString(),
    });
  }

  const source = invoices().where((invoice) => invoice.status === 'paid' && invoice.issued_at < at(-1, '00:00')).sort((a, b) => b.issued_at.localeCompare(a.issued_at))[4];
  if (source) {
    const duplicate = createInvoice({
      customerId: source.customer_id,
      branchId: source.branch_id,
      staffId: deskId,
      petId: source.pet_id,
      lines: source.lines.map(({ kind, description, quantity, unit_price_satang }) => ({ kind, description, quantity, unit_price_satang })),
      issuedAt: new Date(Date.parse(source.issued_at) + 2 * 60_000).toISOString(),
      usePackages: false,
    });
    voidBill(duplicate, `ออกบิลซ้ำกับ ${source.number}`, new Date(Date.parse(duplicate.issued_at) + 3 * 60_000).toISOString());
  }
}

/** The demo customer's account: at least three paid bills and exactly one outstanding. */
function shapeDemoCustomer(context: {
  desk: string;
  vets: string[];
  branchId: string;
  serviceLine: (service: Row | undefined) => Line[];
  services: Row[];
  extrasFor: (service: Row | undefined, pet: Row | undefined) => Line[];
  random: () => number;
}) {
  const customer = table('customers').find((row) => row.user_id === DEMO_ACCOUNTS.customer.user_id);
  if (!customer) return;
  const pets = table('pets').where((pet) => pet.primary_customer_id === customer.id && !pet.is_deceased);
  const mine = () => invoices().where((invoice) => invoice.customer_id === customer.id && invoice.status !== 'void');
  const vet = context.vets[0] ?? context.desk;

  const raise = (daysAgo: number, time: string, serviceCode: string, pet: Row | undefined) => {
    const service = context.services.find((row) => row.category_code === serviceCode) ?? context.services[0];
    const lines = [...context.serviceLine(service), ...context.extrasFor(service, pet)];
    if (!lines.length) return undefined;
    return createInvoice({ customerId: customer.id, branchId: context.branchId, staffId: vet, petId: pet?.id, lines, issuedAt: at(-daysAgo, time), usePackages: false });
  };

  // Paid history, if the visits did not already leave enough of it.
  const plan: Array<[number, string, string]> = [
    [96, '10:20', 'vaccine'],
    [61, '14:05', 'checkup'],
    [33, '11:40', 'vaccine'],
  ];
  for (const [index, [daysAgo, time, code]] of plan.entries()) {
    if (mine().filter((invoice) => invoice.status === 'paid').length >= 3) break;
    const invoice = raise(daysAgo, time, code, pets[index % Math.max(1, pets.length)]);
    if (!invoice) continue;
    const paidAt = new Date(Date.parse(invoice.issued_at) + 9 * 60_000).toISOString();
    const paid = takePayment(invoice, { method: 'promptpay', amountSatang: invoice.balance_satang, reference: `PP${Math.floor(10_000_000 + context.random() * 89_999_999)}`, staffId: context.desk, at: paidAt });
    issueReceipt(paid, undefined, { at: paidAt, notify: false });
  }

  // Exactly one bill still owed, recent and partly paid: settle every other (an old one owed reads
  // as a mistake on the portal's banner), part-pay the one kept, or raise one last week.
  const owed = mine()
    .filter((invoice) => invoice.status === 'open' || invoice.status === 'partially_paid')
    .sort((a, b) => b.issued_at.localeCompare(a.issued_at));
  const today = clinicToday();
  const monthAgo = at(-30, '00:00');
  const keep = owed.find((invoice) => clinicDayOf(invoice.issued_at) < today && invoice.issued_at >= monthAgo && invoice.total_satang >= 20000);
  for (const invoice of owed) {
    if (invoice === keep) continue;
    takePayment(invoice, { method: 'cash', amountSatang: invoice.balance_satang, staffId: context.desk, at: new Date(Date.parse(invoice.issued_at) + 12 * 60_000).toISOString() });
  }
  if (keep && keep.paid_satang === 0) {
    const part = Math.max(10000, Math.floor(keep.total_satang / 2 / 10000) * 10000);
    if (part < keep.total_satang) takePayment(keep, { method: 'cash', amountSatang: part, staffId: context.desk, at: new Date(Date.parse(keep.issued_at) + 10 * 60_000).toISOString() });
  }
  if (!keep) {
    const invoice = raise(6, '16:10', 'checkup', pets[0]);
    if (invoice) {
      const part = Math.max(10000, Math.floor(invoice.total_satang / 2 / 10000) * 10000);
      if (part < invoice.total_satang) takePayment(invoice, { method: 'cash', amountSatang: part, staffId: context.desk, at: new Date(Date.parse(invoice.issued_at) + 10 * 60_000).toISOString() });
    }
  }
}
