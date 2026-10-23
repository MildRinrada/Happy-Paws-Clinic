import type { Consultation, Finding as ExamFinding, Vitals } from '@/features/consultations/types/consultation.types';
import type { LabResult } from '@/features/laboratory/types/lab.types';
import { consultationPage } from '@/features/consultations/content/consultation-page';
import { resultPanels } from '@/features/laboratory/content/result-panels';
import { DEMO_ACCOUNTS } from '../../core/demo-accounts';
import { at, day, nowIso, table, uuid, type Row } from '../../core/db';
import { ensureInventory } from '../billing/api';
import { recordVaccinationDose } from '../customers/store';
import { CASES, caseByKey, EXAM_SYSTEMS, SICK_VISITS, vaccinePlan, type CaseSpec, type Species } from './cases';
import { allergiesOf, allergyMatches, matchMedicine, speciesKind } from './links';
import { flagFor } from './lab-handlers';
import { consultations, courseEndsAt, hashOf, labRequests, medicalRecords, prescriptions, syncPrescriptions, treatmentTemplates } from './store';
import { TREATMENT_TEMPLATES } from './templates';

/**
 * The clinic's clinical history. Every completed appointment (other than grooming) gets the case
 * the vet wrote up, its signed record, the drugs handed over and the lab work sent; the vaccines
 * in each patient's book get the visit they were given at; today's patients in the room have
 * their case open; and the demo customer's three animals have the fullest files.
 *
 * Reads the register (customers, 10), the staff (clinic, 0) and the calendar (appointments, 20),
 * all planted before this, and the stock room (billing, 40), which it has planted first through
 * `ensureInventory()` so each drug handed over names its item and the price it was charged at.
 */
export const order = 30;

const MINUTE = 60_000;

/** A small seeded generator, so the history reads the same after every reset. */
function generator(seed: number) {
  let state = seed >>> 0;
  return () => {
    state = (state + 0x6d2b79f5) >>> 0;
    let value = state;
    value = Math.imul(value ^ (value >>> 15), value | 1);
    value ^= value + Math.imul(value ^ (value >>> 7), value | 61);
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
  };
}

const rand = generator(30_2026);
const between = (low: number, high: number, digits = 1) => Number((low + rand() * (high - low)).toFixed(digits));
const pick = <T>(list: T[]): T => list[Math.floor(rand() * list.length)]!;
const plus = (iso: string, minutes: number) => new Date(Date.parse(iso) + minutes * MINUTE).toISOString();
/** An id for a part of a case (a reading, a finding, a line): unique within it, short for browser storage. */
const partId = () => rand().toString(36).slice(2, 10);
const daysAgo = (iso: string) => Math.floor((Date.parse(at(0, '23:59')) - Date.parse(iso)) / 86_400_000);

interface Plan {
  pet: Row;
  branchId: string;
  vetId: string;
  openedAt: string;
  spec: CaseSpec;
  appointment?: Row;
  ticketId?: string;
  /** Vaccines given at this visit, by name. */
  vaccines?: string[];
  /** Still in the room: the case stays a draft, part-written. */
  open?: boolean;
  /** Send the case's lab work, with this status. */
  lab?: 'pending' | 'resulted' | 'reviewed';
}

// ---- The register, read --------------------------------------------------------------------------

/**
 * The systems the exam form lists (`consultationPage.exam.systems`). The form does not read the
 * clinic's `examination_systems` setting, so findings are written under the form's names.
 */
function examSystems(): string[] {
  return consultationPage.exam.systems.length ? [...consultationPage.exam.systems] : EXAM_SYSTEMS;
}

/** The clinic's own name for a body system the casebook names its way: "หู" is "ตาและหู" here. */
function systemName(name: string, systems: string[]): string {
  const bare = (value: string) => value.replace(/^ระบบ/, '').replace(/ทางเดิน/, '');
  return (
    systems.find((system) => system === name) ??
    systems.find((system) => system.includes(name) || name.includes(system)) ??
    systems.find((system) => bare(system).includes(bare(name)) || bare(name).includes(bare(system))) ??
    name
  );
}

function weightOf(pet: Row, when: string): number {
  const weighings = table('pet_weights').where((row) => row.pet_id === pet.id);
  if (weighings.length) {
    const nearest = weighings.reduce((best, row) =>
      Math.abs(Date.parse(row.recorded_at) - Date.parse(when)) < Math.abs(Date.parse(best.recorded_at) - Date.parse(when)) ? row : best,
    );
    return Number(nearest.weight_kg);
  }
  const kind = speciesKind(pet.species);
  return kind === 'dog' ? 12 : kind === 'cat' ? 4.2 : 1.6;
}

function vitalsFor(pet: Row, spec: CaseSpec, recordedAt: string, staffId: string): Vitals {
  const kind = speciesKind(pet.species);
  const ranges: Record<Species, { temp: [number, number]; hr: [number, number]; rr: [number, number] }> = {
    dog: { temp: [38.2, 39.1], hr: [80, 120], rr: [18, 30] },
    cat: { temp: [38.1, 39.2], hr: [150, 200], rr: [24, 36] },
    other: { temp: [38.3, 39.6], hr: [160, 240], rr: [36, 56] },
  };
  const range = ranges[kind];
  return {
    id: partId(),
    recorded_at: recordedAt,
    recorded_by_staff_id: staffId,
    weight_kg: weightOf(pet, recordedAt),
    temperature_c: spec.fever ? between(39.7, 40.3) : between(...range.temp),
    heart_rate_bpm: Math.round(between(...range.hr, 0)),
    respiratory_rate_bpm: Math.round(between(...range.rr, 0)),
    ...(spec.fever ? { note: 'มีไข้ เยื่อเมือกสีชมพูซีดเล็กน้อย' } : {}),
  };
}

/** `brief`: an older visit written up on the systems that mattered, as a busy day's notes are. */
function findingsFor(spec: CaseSpec, systems: string[], kind: Species, brief = false): ExamFinding[] {
  const abnormal = new Map<string, { normal: boolean; note?: string }>();
  for (const [system, normal, note] of spec.findings) {
    const name = systemName(system, systems);
    const seen = abnormal.get(name);
    abnormal.set(name, { normal: (seen?.normal ?? true) && normal, note: [seen?.note, note].filter(Boolean).join(' ') || undefined });
  }
  // An exotic is examined on fewer systems than a dog or a cat.
  const examined = brief ? systems.slice(0, 3) : kind === 'other' ? systems.slice(0, 5) : systems;
  return [...new Set([...examined, ...abnormal.keys()])].map((system) => {
    const found = abnormal.get(system);
    return { id: partId(), body_system: system, is_normal: found?.normal ?? true, ...(found?.note ? { note: found.note } : {}) };
  });
}

/** The drugs of a case the patient can take: none it has a recorded allergy to, nor a penicillin for a penicillin allergy. */
function drugsFor(pet: Row, spec: CaseSpec) {
  const allergies = allergiesOf(pet.id);
  const penicillin = allergies.some((allergy) => /penicillin|เพนิซิลลิน/i.test(allergy.substance));
  return spec.drugs.filter(([drug]) => {
    if (penicillin && /amoxi|penicil|ampi/i.test(drug)) return false;
    return !allergies.some((allergy) => allergyMatches(allergy.substance, drug) || allergyMatches(allergy.substance.replace(/^.*\(|\)$/g, ''), drug));
  });
}

const stockMatches = new Map<string, Row | undefined>();

/**
 * The stock item a casebook drug is: the prescriber's own match (the same name or first word),
 * else a brand or molecule word of it found in an item's name ("ยาหยอดหู Otomax" is the shelf's
 * "Otomax ยาหยอดหู", "อาหารสูตรโรคไต Renal" its Royal Canin Renal). Undefined when nothing fits.
 */
function stockItemFor(drug: string): Row | undefined {
  if (stockMatches.has(drug)) return stockMatches.get(drug);
  let found = matchMedicine(drug);
  if (!found) {
    const words = drug.split(/[\s()]+/).filter((word) => /^[A-Za-z][A-Za-z-]{3,}$/.test(word)).map((word) => word.toLowerCase());
    const shelf = table('inventory_items').where((item) => item.active !== false);
    for (const word of words) {
      found = shelf.find((item) => String(item.name).toLowerCase().includes(word));
      if (found) break;
    }
  }
  stockMatches.set(drug, found);
  return found;
}

// ---- Lab values ----------------------------------------------------------------------------------

function panelOf(test: { name: string; code?: string }) {
  const code = (test.code ?? '').toUpperCase();
  const name = test.name.toLowerCase();
  return resultPanels.find((panel) => panel.codes.includes(code) || panel.names.includes(name));
}

function resultsFor(tests: { name: string; code?: string }[], kind: Species, abnormal: Record<string, number | string>, recordedAt: string, staffId: string): LabResult[] {
  const species = kind === 'cat' ? 'cat' : 'dog';
  return tests.flatMap((test) => {
    const panel = panelOf(test);
    if (!panel) return [];
    return panel.analytes.map((analyte) => {
      const range = analyte.ranges[species];
      const forced = abnormal[analyte.analyte];
      const base = { id: partId(), analyte: analyte.analyte, ...(analyte.unit ? { unit: analyte.unit } : {}), recorded_at: recordedAt, recorded_by_staff_id: staffId, amended: false, previous_values: [], voided: false };
      if (range && 'text' in range) {
        const text = typeof forced === 'string' ? forced : range.text.split(' ')[0]!;
        return { ...base, value_text: text, reference_text: range.text, flag: (typeof forced === 'string' ? 'abnormal' : 'normal') as LabResult['flag'] };
      }
      const low = range?.low;
      const high = range?.high;
      const digits = high !== undefined && high < 2 ? 3 : high !== undefined && high < 20 ? 1 : 0;
      const value = typeof forced === 'number' ? forced : low !== undefined && high !== undefined ? Number((low + (high - low) * between(0.25, 0.75, 3)).toFixed(digits)) : 0;
      const flag = flagFor({ value, reference_low: low, reference_high: high });
      return { ...base, value_numeric: value, ...(low !== undefined ? { reference_low: low } : {}), ...(high !== undefined ? { reference_high: high } : {}), ...(flag ? { flag } : {}) };
    });
  });
}

const REVIEW_NOTES: Record<string, string> = {
  normal: 'ผลอยู่ในเกณฑ์ปกติ แจ้งเจ้าของแล้ว',
  abnormal: 'ค่าผิดปกติสอดคล้องกับอาการ แจ้งเจ้าของและปรับแผนการรักษาแล้ว',
};

function plantLab(plan: Plan, consultation: Consultation, technicianId: string) {
  const spec = plan.spec;
  if (!spec.lab || !plan.lab) return;
  const requestedAt = plus(plan.openedAt, 12);
  const kind = speciesKind(plan.pet.species);
  const external = spec.key === 'tick-fever';
  const resultedAt = plus(requestedAt, external ? 26 * 60 : 50);
  const reviewedAt = plus(resultedAt, 70);
  const results = plan.lab === 'pending' ? [] : resultsFor(spec.lab.tests, kind, spec.lab.abnormal ?? {}, resultedAt, technicianId);
  const abnormal = results.filter((result) => result.flag && result.flag !== 'normal').length;
  const tests = external ? [...spec.lab.tests, { name: 'PCR พยาธิเม็ดเลือด (Ehrlichia/Anaplasma/Babesia)', code: 'TBD-PCR' }] : spec.lab.tests;
  labRequests().insert({
    id: uuid(),
    version: plan.lab === 'reviewed' ? 3 : plan.lab === 'resulted' ? 2 : 1,
    pet_id: plan.pet.id,
    branch_id: plan.branchId,
    consultation_id: consultation.id,
    destination: external ? 'external' : 'in_house',
    ...(external ? { external_lab_name: 'Vet Central Lab' } : {}),
    priority: spec.fever || spec.key === 'kidney' ? 'urgent' : 'routine',
    tests,
    ...(spec.assessment ? { clinical_notes: spec.assessment } : {}),
    requested_by_staff_id: plan.vetId,
    requested_at: requestedAt,
    status: plan.lab,
    ...(plan.lab !== 'pending' ? { resulted_at: resultedAt } : {}),
    ...(plan.lab === 'reviewed' ? { reviewed_at: reviewedAt, reviewed_by_staff_id: plan.vetId, review_note: REVIEW_NOTES[abnormal ? 'abnormal' : 'normal'] } : {}),
    abnormal_count: abnormal,
    results,
    attachments: external && plan.lab !== 'pending'
      ? [{ id: partId(), file_name: 'VCL-ผลตรวจ.pdf', content_type: 'application/pdf', size_bytes: 184_320, attached_at: resultedAt, attached_by_staff_id: technicianId }]
      : [],
  });
}

// ---- One visit -----------------------------------------------------------------------------------

function plantVisit(plan: Plan, context: { systems: string[]; technicianId: string; today: boolean; brief: boolean }) {
  const { pet, spec } = plan;
  const kind = speciesKind(pet.species);
  const vaccines = plan.vaccines ?? [];
  const closedAt = plus(plan.openedAt, 25 + Math.floor(rand() * 15));
  const treatments = (vaccines.length
    ? [['ตรวจร่างกายก่อนฉีดวัคซีน', 1, 200] as [string, number, number], ...vaccines.map((name) => [`ฉีด${name}`, 1, /พิษสุนัขบ้า|Rabies/.test(name) ? 350 : /FeLV|ลิวคีเมีย/.test(name) ? 850 : 650] as [string, number, number])]
    : spec.treatments
  ).map(([name, quantity, baht], index) => ({
    id: partId(),
    name,
    quantity,
    unit_price_satang: baht * 100,
    line_total_satang: baht * 100 * quantity,
    performed_by_staff_id: plan.vetId,
    performed_at: plus(plan.openedAt, 5 + index * 4),
  }));
  const complaint = plan.appointment?.reason || spec.complaint;
  const diagnoses = spec.diagnoses.map((diagnosis, index) => ({ id: partId(), ...diagnosis, is_primary: index === 0 }));
  const vitals = [vitalsFor(pet, spec, plus(plan.openedAt, 3), context.technicianId)];

  const base: Consultation = {
    id: uuid(),
    version: 1,
    status: plan.open ? 'draft' : 'closed',
    branch_id: plan.branchId,
    pet_id: pet.id,
    customer_id: pet.primary_customer_id,
    vet_staff_id: plan.vetId,
    ...(plan.appointment ? { appointment_id: plan.appointment.id } : {}),
    ...(plan.ticketId ? { queue_ticket_id: plan.ticketId } : {}),
    opened_at: plan.openedAt,
    chief_complaint: complaint,
    subjective: spec.subjective,
    vitals,
    examination: [],
    diagnoses: [],
    treatments: [],
    prescriptions: [],
    treatment_charges_satang: 0,
  };

  if (plan.open) {
    // Part-written, as a case is a few minutes into the visit: history taken, weighed, examined.
    const consultation = consultations().insert({ ...base, objective: spec.objective, examination: findingsFor(spec, context.systems, kind), version: 4 });
    plantLab(plan, consultation, context.technicianId);
    return consultation;
  }

  const recordId = uuid();
  const consultation = consultations().insert({
    ...base,
    version: 9,
    closed_at: closedAt,
    medical_record_id: recordId,
    objective: spec.objective,
    assessment: spec.assessment,
    plan: vaccines.length ? vaccinePlan(vaccines) : spec.plan,
    examination: findingsFor(spec, context.systems, kind, context.brief),
    diagnoses,
    treatments,
    treatment_charges_satang: treatments.reduce((sum, line) => sum + line.line_total_satang, 0),
  });

  // Medicine written in the room; handed over at the counter on the way out, except today's last few.
  const prescribedAt = plus(plan.openedAt, 20);
  const handedOver = !context.today || rand() < 0.4;
  for (const [drug, dose, frequency, days, quantity, instructions] of drugsFor(pet, spec)) {
    const item = stockItemFor(drug);
    prescriptions().insert({
      id: uuid(),
      version: handedOver ? 2 : 1,
      pet_id: pet.id,
      consultation_id: consultation.id,
      branch_id: plan.branchId,
      customer_id: pet.primary_customer_id,
      drug_name: drug,
      dose,
      frequency,
      duration_days: days,
      quantity,
      ...(instructions ? { instructions } : {}),
      prescribed_at: prescribedAt,
      prescribed_by_staff_id: plan.vetId,
      course_ends_at: courseEndsAt(prescribedAt, days),
      ...(item ? { inventory_item_id: item.id } : {}),
      ...(handedOver
        ? {
            dispensed_at: plus(closedAt, 8),
            dispensed_by_staff_id: context.technicianId,
            stock_deducted: Boolean(item),
            ...(item ? { charged_unit_price_satang: Number(item.unit_price_satang) } : {}),
          }
        : {}),
    });
  }
  syncPrescriptions(consultation.id);

  const signed = consultations().get(consultation.id)!;
  medicalRecords().insert({
    id: recordId,
    version: 1,
    status: 'signed',
    consultation_id: consultation.id,
    pet_id: pet.id,
    customer_id: pet.primary_customer_id,
    branch_id: plan.branchId,
    signed_at: closedAt,
    signed_by_staff_id: plan.vetId,
    content_hash: hashOf(JSON.stringify(signed)),
    integrity_verified: true,
    amendments: [],
  });
  plantLab(plan, signed, context.technicianId);
  return signed;
}

// ---- Choosing the case ---------------------------------------------------------------------------

function caseFor(kind: Species, serviceName: string, reason: string | undefined, category: string | undefined): CaseSpec {
  if (category === 'vaccine' || /วัคซีน/.test(serviceName)) return caseByKey('vaccine');
  const fits = CASES.filter((spec) => spec.species.includes(kind) && spec.key !== 'vaccine');
  return (
    (reason ? fits.find((spec) => spec.reasons?.test(reason)) : undefined) ??
    fits.find((spec) => spec.services?.test(serviceName)) ??
    caseByKey(pick(SICK_VISITS[kind]))
  );
}

/** The vaccine a booked vaccination service gives, named as the patient's vaccine book names it. */
function vaccineOfService(serviceName: string, kind: Species): { name: string; manufacturer: string } {
  if (/พิษสุนัขบ้า|rabies/i.test(serviceName)) return { name: 'วัคซีนพิษสุนัขบ้า (Rabies)', manufacturer: 'Virbac' };
  if (/FeLV|ลิวคีเมีย/i.test(serviceName)) return { name: 'วัคซีนลิวคีเมียแมว FeLV', manufacturer: 'Boehringer Ingelheim' };
  if (/FVRCP/i.test(serviceName) || kind === 'cat') return { name: 'วัคซีนรวมแมว FVRCP', manufacturer: 'Zoetis' };
  return { name: 'วัคซีนรวม DHPPi+L', manufacturer: 'Boehringer Ingelheim' };
}

function labStatusFor(openedAt: string): 'pending' | 'resulted' | 'reviewed' {
  const age = daysAgo(openedAt);
  if (age === 0) return rand() < 0.35 ? 'pending' : 'resulted';
  if (age <= 2) return rand() < 0.5 ? 'resulted' : 'reviewed';
  return 'reviewed';
}

// ---- The seed ------------------------------------------------------------------------------------

export function seed() {
  // The stock room is billing's (order 40); planted now so prescriptions can name their items.
  ensureInventory();
  stockMatches.clear();
  treatmentTemplates().insertMany(TREATMENT_TEMPLATES.map((template) => ({ ...template, id: uuid() })));

  const pets = table('pets').all();
  const vets = table('staff_members').where((member) => member.staff_type === 'veterinarian' && member.status === 'active');
  if (!pets.length || !vets.length) {
    console.warn('[mock] clinical seed: no pets or vets yet, nothing planted');
    return;
  }
  const branches = table('branches').all();
  const mainBranch = branches[0]?.id ?? '';
  const services = new Map(table('services').all().map((service) => [service.id, service]));
  const demoVet = vets.find((member) => member.user_id === DEMO_ACCOUNTS.vet.user_id) ?? vets[0]!;
  const assistants = table('staff_members').where((member) => member.staff_type === 'vet_assistant' && member.status === 'active');
  const technicianId = (assistants[0] ?? demoVet).id;
  const vetAt = (branchId: string) => pick(vets.filter((member) => !member.branches?.length || member.branches.includes(branchId))) ?? demoVet;
  const demoCustomer = table('customers').find((customer) => customer.user_id === DEMO_ACCOUNTS.customer.user_id);
  const demoPets = new Set(pets.filter((pet) => pet.primary_customer_id === demoCustomer?.id).map((pet) => pet.id));
  const systems = examSystems();
  const visitDays = new Map<string, Set<string>>();
  const markVisit = (petId: string, iso: string) => {
    if (!visitDays.has(petId)) visitDays.set(petId, new Set());
    visitDays.get(petId)!.add(iso.slice(0, 10));
  };
  const plant = (plan: Plan) => {
    markVisit(plan.pet.id, plan.openedAt);
    const age = daysAgo(plan.openedAt);
    return plantVisit(plan, { systems, technicianId, today: age === 0, brief: age > 7 && !demoPets.has(plan.pet.id) });
  };

  // 1. Every completed appointment that was a medical visit.
  const appointments = table('appointments').where((row) => row.status === 'completed' && row.starts_at <= nowIso());
  const tickets = table('queue_tickets').all();
  for (const appointment of appointments.sort((a, b) => a.starts_at.localeCompare(b.starts_at))) {
    const pet = pets.find((row) => row.id === appointment.pet_id);
    const service = services.get(appointment.service_id);
    if (!pet || !service || service.category_code === 'grooming') continue;
    const kind = speciesKind(pet.species);
    const spec = caseFor(kind, service.name, appointment.reason ?? undefined, service.category_code);
    const openedAt = plus(appointment.starts_at, 2);
    const vaccine = spec.key === 'vaccine' ? vaccineOfService(service.name, kind) : undefined;
    if (vaccine && !demoPets.has(pet.id)) {
      // The dose goes in the patient's book, unless the book already has it from that week.
      const near = table('pet_vaccinations').find(
        (dose) => dose.pet_id === pet.id && dose.vaccine_name === vaccine.name && Math.abs(Date.parse(dose.administered_at) - Date.parse(openedAt)) < 30 * 86_400_000,
      );
      if (!near) {
        recordVaccinationDose(pet.id, {
          vaccine_name: vaccine.name,
          manufacturer: vaccine.manufacturer,
          lot_number: `${vaccine.manufacturer.slice(0, 2).toUpperCase()}${240000 + Math.floor(rand() * 9999)}`,
          vial_expires_on: day(300 - daysAgo(openedAt)),
          administered_at: plus(openedAt, 10),
          administered_by_staff_id: appointment.staff_id ?? demoVet.id,
          next_due_on: day(365 - daysAgo(openedAt)),
        });
      }
    }
    plant({
      pet,
      branchId: appointment.branch_id,
      vetId: appointment.staff_id ?? vetAt(appointment.branch_id).id,
      openedAt,
      spec,
      appointment,
      ticketId: tickets.find((ticket) => ticket.appointment_id === appointment.id)?.id,
      vaccines: vaccine ? [vaccine.name] : undefined,
      // Lab work from the last week; older results came back on paper and are filed with the visit.
      lab: spec.lab && daysAgo(openedAt) <= 7 ? labStatusFor(openedAt) : undefined,
    });
  }

  // 2. The visits the demo customer's vaccine books were filled in at.
  const byPetDay = new Map<string, Row[]>();
  for (const dose of table('pet_vaccinations').where((row) => demoPets.has(row.pet_id))) {
    const key = `${dose.pet_id}|${String(dose.administered_at).slice(0, 10)}`;
    byPetDay.set(key, [...(byPetDay.get(key) ?? []), dose]);
  }
  for (const [key, given] of byPetDay) {
    const [petId, date] = key.split('|') as [string, string];
    const pet = pets.find((row) => row.id === petId);
    if (!pet || visitDays.get(petId)?.has(date)) continue;
    const vet = table('staff_members').get(given[0]!.administered_by_staff_id);
    const branchId = vet?.branches?.[0] ?? mainBranch;
    plant({ pet, branchId, vetId: vet?.staff_type === 'veterinarian' ? vet.id : vetAt(branchId).id, openedAt: plus(given[0]!.administered_at, -12), spec: caseByKey('vaccine'), vaccines: given.map((dose) => dose.vaccine_name) });
  }

  // 3. The demo customer's animals: the illnesses that fill out their files.
  const designed: Record<Species, Array<[key: string, ago: number, time: string, lab?: 'reviewed']>> = {
    dog: [['skin', 150, '10:15'], ['checkup', 40, '09:40', 'reviewed'], ['ear', 6, '17:20']],
    cat: [['uri', 60, '11:30'], ['flutd', 18, '16:10', 'reviewed']],
    other: [['rabbit-gi', 30, '14:00']],
  };
  for (const pet of pets.filter((row) => demoPets.has(row.id))) {
    const kind = speciesKind(pet.species);
    for (const [key, ago, time, lab] of designed[kind]) {
      const date = day(-ago);
      if (visitDays.get(pet.id)?.has(date)) continue;
      plant({ pet, branchId: mainBranch, vetId: demoVet.id, openedAt: at(-ago, time), spec: caseByKey(key), lab });
    }
  }

  // 4. Today's patients in the room: their case is open, a few minutes in.
  for (const appointment of table('appointments').where((row) => row.status === 'in_consultation')) {
    const pet = pets.find((row) => row.id === appointment.pet_id);
    const service = services.get(appointment.service_id);
    if (!pet) continue;
    const spec = caseFor(speciesKind(pet.species), service?.name ?? '', appointment.reason ?? undefined, service?.category_code);
    const ticket = tickets.find((row) => row.appointment_id === appointment.id);
    plant({
      pet,
      branchId: appointment.branch_id,
      vetId: appointment.staff_id ?? ticket?.staff_id ?? vetAt(appointment.branch_id).id,
      openedAt: ticket?.entered_room_at ?? plus(appointment.starts_at, 3),
      spec,
      appointment,
      ticketId: ticket?.id,
      open: true,
      lab: spec.lab ? 'pending' : undefined,
    });
  }
  for (const ticket of tickets.filter((row) => row.status === 'in_room' && !row.appointment_id)) {
    const pet = pets.find((row) => row.id === ticket.pet_id);
    if (!pet || consultations().find((row) => row.queue_ticket_id === ticket.id)) continue;
    plant({
      pet,
      branchId: ticket.branch_id,
      vetId: ticket.staff_id ?? vetAt(ticket.branch_id).id,
      openedAt: ticket.entered_room_at ?? nowIso(),
      spec: caseByKey(pick(SICK_VISITS[speciesKind(pet.species)])),
      ticketId: ticket.id,
      open: true,
    });
  }

  // 5. The lab's worklist should never be empty: a few recent cases with work still waiting.
  const atMain = labRequests().where((row) => row.branch_id === mainBranch);
  if (atMain.filter((row) => row.status === 'pending').length < 3 || atMain.filter((row) => row.status === 'resulted').length < 3) {
    const recent: Array<[key: string, ago: number, time: string, lab: 'pending' | 'resulted']> = [
      ['tick-fever', 1, '10:20', 'pending'],
      ['kidney', 1, '15:40', 'resulted'],
      ['gastro', 2, '11:05', 'resulted'],
      ['checkup', 1, '09:15', 'resulted'],
      ['preop', 2, '16:05', 'pending'],
    ];
    // Patients the calendar has seen this month or has booked: the ones it keeps away (the CRM's
    // due-back list) must not turn up with a case from yesterday.
    const monthAgo = at(-30, '00:00');
    const regulars = new Set(table('appointments').where((row) => row.starts_at >= monthAgo).map((row) => row.pet_id));
    const candidates = pets.filter((pet) => !demoPets.has(pet.id) && !pet.is_deceased && speciesKind(pet.species) !== 'other' && regulars.has(pet.id));
    for (const [index, [key, ago, time, lab]] of recent.entries()) {
      const spec = caseByKey(key);
      const pet = candidates.find((row, at) => at >= index * 7 && spec.species.includes(speciesKind(row.species)) && !visitDays.get(row.id)?.has(day(-ago)));
      if (!pet) continue;
      plant({ pet, branchId: mainBranch, vetId: index % 2 === 0 ? demoVet.id : vetAt(mainBranch).id, openedAt: at(-ago, time), spec, lab });
    }
  }
}
