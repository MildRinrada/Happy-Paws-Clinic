import { displayTokens } from '../../core/auth';
import { table, uuid, type Row } from '../../core/db';
import { DEMO_ACCOUNTS } from '../../core/demo-accounts';
import { availability } from './availability';
import { branches, offeredAt, services, vetsAt } from './catalog';
import { issueTicket } from './queue';
import { appointments, screens, tickets, waitingList, type AppointmentRow, type AppointmentStatus, type TicketRow } from './store';
import { addMinutes, clinicDay, clinicMinutes, clinicToday, instant, timeOf } from './time';

/**
 * A clinic in the middle of its day: about twenty-five appointments today across both branches in
 * every state, a live queue with someone in each room, a month of history behind and two weeks of
 * bookings ahead, a waiting list, and the waiting-room screens already on the wall. Every id is
 * looked up from the clinic and customers seeds; times come from the real roster, so the free
 * slots the booking pages offer are the gaps between these.
 */
export const order = 20;

// ---- A repeatable dice -------------------------------------------------------------------------------

let state = 20261023;
function random(): number {
  state = (state + 0x6d2b79f5) | 0;
  let value = Math.imul(state ^ (state >>> 15), 1 | state);
  value = (value + Math.imul(value ^ (value >>> 7), 61 | value)) ^ value;
  return ((value ^ (value >>> 14)) >>> 0) / 4_294_967_296;
}
const pick = <T>(list: readonly T[]): T => list[Math.floor(random() * list.length)]!;
const chance = (probability: number) => random() < probability;

// ---- What a visit is for -----------------------------------------------------------------------------

/** Service codes by species, most common first; the clinic seed's codes, looked up by code. */
const VISITS: Record<string, string[]> = {
  สุนัข: ['VAC-DOG', 'GEN-CHECK', 'GEN-CHECK', 'SKIN', 'ANNUAL', 'GRM-BATH', 'VAC-RAB', 'DEN-SCALE', 'LAB-CBC'],
  แมว: ['VAC-CAT', 'GEN-CHECK', 'GEN-CHECK', 'ANNUAL', 'VAC-FELV', 'GRM-CAT', 'LAB-CHEM', 'SKIN'],
  กระต่าย: ['EXOTIC', 'EXOTIC', 'GEN-CHECK'],
};

const REASONS: Record<string, string[]> = {
  'GEN-CHECK': ['ซึม กินอาหารน้อยลง 2 วัน', 'อาเจียนเมื่อคืน 3 ครั้ง', 'ถ่ายเหลว', 'เดินกะเผลกขาหลังซ้าย', 'ตาแดง มีขี้ตา', 'ไอ จามบ่อย', 'ตรวจติดตามอาการ'],
  ANNUAL: ['ตรวจสุขภาพประจำปี', 'ตรวจเลือดก่อนเข้าวัยชรา'],
  SKIN: ['คันและเกาบ่อย ขนร่วงเป็นหย่อม', 'มีผื่นแดงที่ท้อง', 'เลียเท้าตลอด'],
  EXOTIC: ['ไม่ค่อยกินหญ้า', 'ฟันยาว ตัดฟัน', 'ขนร่วงผิดปกติ'],
  'VAC-DOG': ['ฉีดวัคซีนรวมประจำปี', 'วัคซีนเข็มกระตุ้น'],
  'VAC-CAT': ['ฉีดวัคซีนรวมประจำปี', 'วัคซีนเข็มที่ 2'],
  'VAC-RAB': ['ฉีดวัคซีนพิษสุนัขบ้าประจำปี'],
  'VAC-FELV': ['วัคซีนลิวคีเมียเข็มแรก'],
  'DEN-SCALE': ['หินปูนเยอะ มีกลิ่นปาก'],
  'GRM-BATH': ['อาบน้ำตัดเล็บ'],
  'GRM-CAT': ['อาบน้ำ ขนพันกัน'],
  'LAB-CBC': ['ตรวจเลือดก่อนผ่าตัด'],
  'LAB-CHEM': ['ตรวจค่าไตติดตามผล'],
};

const LEFT_REASONS = ['รอนานเกินไป มีธุระต่อ', 'น้องอาการดีขึ้นแล้ว ขอกลับก่อน'];
const URGENT_REASONS = ['หายใจหอบ เหงือกซีด', 'กินช็อกโกแลตไปเมื่อชั่วโมงก่อน', 'ถูกรถเฉี่ยว ขาหน้าบวม'];

interface Pet extends Row {
  name: string;
  species: string;
  primary_customer_id: string;
}

const bookedOn = (petId: string, date: string) =>
  Boolean(appointments().find((row) => row.pet_id === petId && row.starts_at >= instant(date, '00:00') && row.starts_at < instant(date, '23:59')));

const serviceByCode = (code: string) => services().find((service) => service.code === code && service.is_active !== false);

function serviceFor(pet: Pet, branchId: string): Row | undefined {
  const codes = VISITS[pet.species] ?? ['GEN-CHECK'];
  for (let attempt = 0; attempt < 6; attempt += 1) {
    const service = serviceByCode(pick(codes));
    if (service && offeredAt(service, branchId)) return service;
  }
  return services().find((service) => service.is_active !== false && offeredAt(service, branchId));
}

/** A free start for this service at this branch that day, by the roster. `after`/`before` are clinic minutes. */
function freeSlot(serviceId: string, branchId: string, date: string, window: { after?: number; before?: number } = {}) {
  const slots = availability({ serviceId, branchId, date, ignoreClock: true }).slots.filter((slot) => {
    const minute = clinicMinutes(slot.starts_at);
    return (window.after === undefined || minute >= window.after) && (window.before === undefined || minute < window.before);
  });
  return slots.length ? pick(slots) : undefined;
}

function book(pet: Pet, service: Row, slot: { starts_at: string; ends_at: string; staff_id: string }, branchId: string, status: AppointmentStatus, extra: Partial<AppointmentRow> = {}): AppointmentRow {
  const ahead = pick([1, 2, 3, 5, 7, 10, 21]);
  const createdAt = new Date(Math.min(Date.parse(slot.starts_at) - ahead * 86_400_000, Date.now() - 3_600_000)).toISOString();
  const confirmed = status === 'confirmed' || ['checked_in', 'in_consultation', 'completed'].includes(status);
  const type = service.category_code === 'surgery' ? 'surgery' : chance(0.12) ? 'follow_up' : 'regular';
  return appointments().insert({
    version: 1,
    status,
    branch_id: branchId,
    customer_id: pet.primary_customer_id,
    pet_id: pet.id,
    service_id: service.id,
    staff_id: slot.staff_id,
    starts_at: slot.starts_at,
    ends_at: slot.ends_at,
    reason: chance(0.85) ? pick(REASONS[service.code] ?? REASONS['GEN-CHECK']!) : null,
    walk_in: false,
    confirmed_at: confirmed ? addMinutes(slot.starts_at, -(18 * 60 + Math.floor(random() * 600))) : null,
    checked_in_at: ['checked_in', 'in_consultation', 'completed'].includes(status) ? addMinutes(slot.starts_at, -(5 + Math.floor(random() * 15))) : null,
    cancelled_at: status === 'cancelled' ? addMinutes(slot.starts_at, -(60 * pick([3, 20, 40, 72]))) : null,
    cancellation_reason: status === 'cancelled' ? pick(['ติดธุระด่วน', 'น้องอาการดีขึ้นแล้ว', 'ขอเลื่อนไปสัปดาห์หน้า', 'รถติดมาไม่ทัน']) : null,
    type,
    series_id: null,
    created_at: createdAt,
    source: chance(0.45) ? 'online' : 'staff',
    ...(status === 'cancelled' ? { late_cancellation: chance(0.4) } : {}),
    ...(status === 'no_show' ? { no_show_at: addMinutes(slot.starts_at, 30) } : {}),
    ...extra,
  });
}

// ---- The seed ----------------------------------------------------------------------------------------

export function seed() {
  state = 20261023;
  const open = branches();
  if (!open.length) return;

  const customers = new Map(table('customers').where((row) => row.is_active !== false && !row.merged_into_customer_id && !row.deleted_at).map((row) => [row.id, row]));
  const pets = table('pets').where((pet) => !pet.is_deceased && customers.has(pet.primary_customer_id)) as Pet[];
  if (!pets.length) return;
  const demoCustomer = table('customers').find((row) => row.user_id === DEMO_ACCOUNTS.customer.user_id);
  const demoPets = pets.filter((pet) => pet.primary_customer_id === demoCustomer?.id);
  const away = stayingAway(pets, demoCustomer?.id);
  const others = pets.filter((pet) => pet.primary_customer_id !== demoCustomer?.id && !away.pets.has(pet.id));
  // Rabbits go to the branch with the rabbit vet; everyone else is spread over both.
  const petsFor = (branchId: string) => others.filter((pet) => pet.species !== 'กระต่าย' || serviceFor(pet, branchId)?.code === 'EXOTIC');

  const today = clinicToday();
  const realNow = clinicMinutes(new Date());
  // The day as the seed draws it: mid-morning at the earliest and early evening at the latest, so
  // there is always a queue to look at, whenever the demo is opened.
  const now = Math.min(Math.max(realNow, 11 * 60), 18 * 60);
  const nowAt = instant(today, timeOf(now));

  // History and bookings ahead.
  for (let offset = -30; offset <= 14; offset += 1) {
    if (offset === 0) continue;
    const date = clinicDay(offset);
    for (const branch of open) {
      const count = offset < 0 ? 2 + Math.floor(random() * 3) : 2 + Math.floor(random() * 3);
      for (let index = 0; index < count; index += 1) {
        const pet = pick(petsFor(branch.id));
        const service = serviceFor(pet, branch.id);
        const slot = service && freeSlot(service.id, branch.id, date);
        if (!service || !slot) continue;
        const status: AppointmentStatus =
          offset < 0 ? (chance(0.08) ? 'no_show' : chance(0.08) ? 'cancelled' : 'completed') : chance(0.06) ? 'cancelled' : offset <= 2 && chance(0.6) ? 'confirmed' : chance(0.3) ? 'confirmed' : 'booked';
        book(pet, service, slot, branch.id, status);
      }
    }
  }

  // Today: most of the roster's slots, in whatever state the hour puts them.
  const queueSpecs: Array<{ appointment?: AppointmentRow; pet: Pet; serviceId: string; branchId: string; issuedAt: string; status: TicketRow['status']; extra: Partial<TicketRow> }> = [];
  for (const [branchIndex, branch] of open.entries()) {
    const wanted = branchIndex === 0 ? 15 : 10;
    const rooms = (branch.exam_rooms ?? []) as string[];
    let booked = 0;
    let inRoom = 0;
    let called = false;
    for (let attempt = 0; attempt < wanted * 4 && booked < wanted; attempt += 1) {
      const pet = pick(petsFor(branch.id));
      if (bookedOn(pet.id, today)) continue;
      const service = serviceFor(pet, branch.id);
      const slot = service && freeSlot(service.id, branch.id, today);
      if (!service || !slot) continue;
      const start = clinicMinutes(slot.starts_at);
      const end = clinicMinutes(slot.ends_at);
      let status: AppointmentStatus;
      if (end <= now - 10) status = chance(0.1) ? 'no_show' : chance(0.08) ? 'cancelled' : 'completed';
      else if (start <= now && inRoom < Math.max(1, rooms.length - 1)) status = 'in_consultation';
      else if (start <= now + 50) status = 'checked_in';
      else status = chance(0.08) ? 'cancelled' : chance(0.55) ? 'confirmed' : 'booked';
      const row = book(pet, service, slot, branch.id, status);
      booked += 1;

      if (status === 'completed' || status === 'in_consultation' || status === 'checked_in') {
        const issuedAt = row.checked_in_at ?? addMinutes(row.starts_at, -10);
        const ticketStatus: TicketRow['status'] = status === 'completed' ? 'served' : status === 'in_consultation' ? 'in_room' : !called && start <= now + 15 ? 'called' : 'waiting';
        const room = rooms[ticketStatus === 'in_room' ? inRoom % Math.max(1, rooms.length) : (inRoom + 1) % Math.max(1, rooms.length)];
        if (ticketStatus === 'in_room') inRoom += 1;
        if (ticketStatus === 'called') called = true;
        const calledAt = ticketStatus === 'waiting' ? undefined : ticketStatus === 'called' ? addMinutes(nowAt, -3) : addMinutes(row.starts_at, 2);
        queueSpecs.push({
          appointment: row,
          pet,
          serviceId: service.id,
          branchId: branch.id,
          issuedAt: new Date(Math.min(Date.parse(issuedAt), Date.parse(nowAt) - 60_000)).toISOString(),
          status: ticketStatus,
          extra: {
            ...(calledAt ? { called_at: calledAt } : {}),
            ...(ticketStatus !== 'waiting' && room ? { room_id: room } : {}),
            ...(ticketStatus === 'in_room' || ticketStatus === 'served' ? { entered_room_at: addMinutes(row.starts_at, 4) } : {}),
            ...(ticketStatus === 'served' ? { served_at: addMinutes(row.ends_at, 2) } : {}),
          },
        });
      }
    }

    // Whatever the hour, someone is in a room and someone has just been called.
    const branchSpecs = () => queueSpecs.filter((spec) => spec.branchId === branch.id);
    const waitingSpecs = () => branchSpecs().filter((spec) => spec.status === 'waiting' && spec.appointment).sort((a, b) => a.issuedAt.localeCompare(b.issuedAt));
    const usedRooms = () => new Set(branchSpecs().filter((spec) => spec.status === 'in_room' || spec.status === 'called').map((spec) => spec.extra.room_id));
    while (branchSpecs().filter((spec) => spec.status === 'in_room').length < Math.min(2, Math.max(1, rooms.length - 1)) && waitingSpecs().length > 1) {
      const spec = waitingSpecs()[0]!;
      const room = rooms.find((name) => !usedRooms().has(name));
      spec.status = 'in_room';
      spec.extra = { called_at: addMinutes(nowAt, -14), entered_room_at: addMinutes(nowAt, -12), ...(room ? { room_id: room } : {}) };
      appointments().update(spec.appointment!.id, { status: 'in_consultation', version: 1 });
    }
    // The dice may have left no visit running right now: book one that started a little while ago.
    if (!branchSpecs().some((spec) => spec.status === 'in_room')) {
      const pet = petsFor(branch.id).find((candidate) => candidate.species !== 'กระต่าย' && !bookedOn(candidate.id, today));
      const service = serviceByCode('GEN-CHECK');
      const slot = pet && service && offeredAt(service, branch.id) ? freeSlot(service.id, branch.id, today, { after: now - 40, before: now - 2 }) : undefined;
      // No free time just behind us: whoever was called or is waiting goes in instead.
      const next = branchSpecs()
        .filter((spec) => spec.appointment && (spec.status === 'called' || spec.status === 'waiting'))
        .sort((a, b) => a.issuedAt.localeCompare(b.issuedAt))[0];
      if (pet && service && slot) {
        const room = rooms.find((name) => !usedRooms().has(name)) ?? rooms[0];
        const row = book(pet, service, slot, branch.id, 'in_consultation');
        queueSpecs.push({
          appointment: row,
          pet,
          serviceId: service.id,
          branchId: branch.id,
          issuedAt: addMinutes(row.starts_at, -10),
          status: 'in_room',
          extra: { called_at: addMinutes(row.starts_at, 1), entered_room_at: addMinutes(row.starts_at, 3), ...(room ? { room_id: room } : {}) },
        });
      } else if (next) {
        const room = (next.extra.room_id as string | undefined) ?? rooms.find((name) => !usedRooms().has(name)) ?? rooms[0];
        next.status = 'in_room';
        next.extra = { called_at: addMinutes(nowAt, -14), entered_room_at: addMinutes(nowAt, -12), ...(room ? { room_id: room } : {}) };
        appointments().update(next.appointment!.id, { status: 'in_consultation', version: 1 });
      }
    }
    if (!branchSpecs().some((spec) => spec.status === 'called') && waitingSpecs().length > 0) {
      const spec = waitingSpecs()[0]!;
      const room = rooms.find((name) => !usedRooms().has(name)) ?? rooms[0];
      spec.status = 'called';
      spec.extra = { called_at: addMinutes(nowAt, -2), ...(room ? { room_id: room } : {}) };
    }

    // Walk-ins: a few waiting, one urgent, one passed over, one who gave up.
    const walkInPets = petsFor(branch.id).filter((pet) => !bookedOn(pet.id, today) && !tickets().find((row) => row.pet_id === pet.id));
    const walkIns: Array<{ status: TicketRow['status']; minutesAgo: number; extra: Partial<TicketRow> }> = [
      { status: 'served', minutesAgo: 150, extra: { called_at: addMinutes(nowAt, -130), entered_room_at: addMinutes(nowAt, -128), served_at: addMinutes(nowAt, -105), room_id: rooms[0] } },
      { status: 'left', minutesAgo: 95, extra: { left_at: addMinutes(nowAt, -40), left_reason: pick(LEFT_REASONS) } },
      { status: 'skipped', minutesAgo: 55, extra: { skip_count: 1, skipped_at: addMinutes(nowAt, -12), called_at: addMinutes(nowAt, -20) } },
      { status: 'waiting', minutesAgo: 35, extra: {} },
      { status: 'waiting', minutesAgo: 8, extra: branchIndex === 0 ? { priority: 'urgent', priority_reason: pick(URGENT_REASONS) } : {} },
      ...(branchIndex === 0 ? [{ status: 'waiting' as const, minutesAgo: 20, extra: {} }] : []),
    ];
    for (const walkIn of walkIns) {
      const pet = walkInPets.splice(Math.floor(random() * walkInPets.length), 1)[0];
      if (!pet) break;
      const service = serviceByCode(pet.species === 'กระต่าย' ? 'EXOTIC' : 'GEN-CHECK') ?? serviceFor(pet, branch.id);
      if (!service) continue;
      queueSpecs.push({ pet, serviceId: service.id, branchId: branch.id, issuedAt: addMinutes(nowAt, -walkIn.minutesAgo), status: walkIn.status, extra: walkIn.extra });
    }
  }

  // Numbers go out in the order people arrived.
  queueSpecs.sort((a, b) => a.issuedAt.localeCompare(b.issuedAt));
  for (const spec of queueSpecs) {
    const { priority, priority_reason, ...rest } = spec.extra;
    const ticket = issueTicket(
      {
        branchId: spec.branchId,
        petId: spec.pet.id,
        customerId: spec.pet.primary_customer_id,
        serviceId: spec.serviceId,
        appointmentId: spec.appointment?.id,
        staffId: spec.appointment?.staff_id ?? (spec.status === 'served' ? vetsAt(spec.branchId)[0]?.id : undefined),
        priority: priority ?? (spec.appointment?.type === 'emergency' ? 'emergency' : 'normal'),
        priorityReason: priority_reason,
        issuedAt: spec.issuedAt,
      },
      { silent: true },
    );
    tickets().update(ticket.id, { status: spec.status, ...rest, version: 1 + (spec.status === 'waiting' ? 0 : 1) });
  }

  // Last week's queue, for the history page.
  for (let offset = -7; offset < 0; offset += 1) {
    const date = clinicDay(offset);
    const done = appointments()
      .where((row) => row.starts_at >= instant(date, '00:00') && row.starts_at < instant(date, '23:59') && row.status === 'completed')
      .sort((a, b) => a.starts_at.localeCompare(b.starts_at));
    for (const row of done) {
      const ticket = issueTicket(
        { branchId: row.branch_id, petId: row.pet_id, customerId: row.customer_id, serviceId: row.service_id, appointmentId: row.id, staffId: row.staff_id ?? undefined, date, issuedAt: addMinutes(row.starts_at, -12) },
        { silent: true },
      );
      const room = (open.find((branch) => branch.id === row.branch_id)?.exam_rooms ?? [])[0];
      tickets().update(ticket.id, {
        status: 'served',
        called_at: addMinutes(row.starts_at, 1),
        entered_room_at: addMinutes(row.starts_at, 3),
        served_at: addMinutes(row.ends_at, 4),
        ...(room ? { room_id: room } : {}),
        version: 4,
      });
    }
  }

  seedDemoCustomer(demoPets, nowAt);
  seedLapsed(away.lapsed);
  seedWaitingList(others);
  seedScreens(open);
}

// ---- Who has not been back ---------------------------------------------------------------------------

/**
 * The patients the CRM's "ควรกลับมาแล้ว" page is about, kept off the calendar so they stay due:
 * up to five pets whose vaccine fell due in the last six months (by the register's vaccine book),
 * and four customers who used to come regularly and stopped two to six months ago. None of them is
 * booked, queued or waiting-listed; the lapsed ones get their old visits from `seedLapsed`.
 *
 * The lapsed are taken from the end of the register: the CRM seed sells packages (bills, so visits)
 * to the first few customers with animals.
 */
function stayingAway(pets: Pet[], demoCustomerId: string | undefined): { pets: Set<string>; lapsed: Pet[][] } {
  const today = clinicToday();
  const since = clinicDay(-180);
  const latest = new Map<string, Row>();
  for (const dose of table('pet_vaccinations').all()) {
    const key = `${dose.pet_id}|${dose.vaccine_name}`;
    const seen = latest.get(key);
    if (!seen || String(seen.administered_at) < String(dose.administered_at)) latest.set(key, dose);
  }
  const overdueOn = new Map<string, string>();
  for (const dose of latest.values()) {
    const due = String(dose.next_due_on ?? '');
    if (due && due < today && due >= since && (!overdueOn.has(dose.pet_id) || due > overdueOn.get(dose.pet_id)!)) overdueOn.set(dose.pet_id, due);
  }
  const candidates = pets.filter((pet) => pet.primary_customer_id !== demoCustomerId);
  const overdue: Pet[] = [];
  for (const pet of candidates.filter((row) => overdueOn.has(row.id)).sort((a, b) => overdueOn.get(b.id)!.localeCompare(overdueOn.get(a.id)!))) {
    if (overdue.length >= 5 || overdue.some((row) => row.primary_customer_id === pet.primary_customer_id)) continue;
    overdue.push(pet);
  }
  const busyOwners = new Set(overdue.map((pet) => pet.primary_customer_id));
  const owners = [...new Set(candidates.map((pet) => pet.primary_customer_id))].filter((id) => !busyOwners.has(id));
  const lapsed = owners
    .slice(-4)
    .map((id) => candidates.filter((pet) => pet.primary_customer_id === id));
  return { pets: new Set([...overdue, ...lapsed.flat()].map((pet) => pet.id)), lapsed };
}

/**
 * The lapsed customers' history: three visits three to six weeks apart, the last one between
 * ten weeks and five months ago, and nothing since. Old days may be outside the roster the clinic
 * seed planted, so a visit takes the roster's slot when there is one and a plain morning one if not.
 */
function seedLapsed(lapsed: Pet[][]) {
  const branch = branches()[0];
  if (!branch) return;
  const plans = [
    [-140, -105, -70],
    [-185, -140, -95],
    [-190, -160, -130],
    [-198, -175, -150],
  ];
  lapsed.forEach((petsOfOwner, index) => {
    for (const [visit, offset] of (plans[index] ?? []).entries()) {
      const pet = petsOfOwner[visit % petsOfOwner.length];
      if (!pet) continue;
      const service = [serviceByCode(pet.species === 'กระต่าย' ? 'EXOTIC' : pick(['GEN-CHECK', 'ANNUAL', 'SKIN'])), serviceByCode('GEN-CHECK')].find(
        (candidate) => candidate && offeredAt(candidate, branch.id),
      );
      if (!service) continue;
      const date = clinicDay(offset);
      const vet = vetsAt(branch.id)[visit % Math.max(1, vetsAt(branch.id).length)];
      const start = instant(date, pick(['10:00', '11:30', '14:00', '16:30']));
      const slot = freeSlot(service.id, branch.id, date) ?? (vet ? { starts_at: start, ends_at: addMinutes(start, Number(service.duration_minutes) || 30), staff_id: vet.id } : undefined);
      if (slot) book(pet, service, slot, branch.id, 'completed');
    }
  });
}

/** The demo customer: visits behind them, one later today, and one next week. */
function seedDemoCustomer(demoPets: Pet[], nowAt: string) {
  if (!demoPets.length) return;
  const branch = branches()[0]!;
  const plan: Array<{ offset: number; status: AppointmentStatus; window?: { after?: number; before?: number } }> = [
    { offset: -26, status: 'completed' },
    { offset: -12, status: 'completed' },
    { offset: -9, status: 'cancelled' },
    { offset: -4, status: 'completed' },
    { offset: 0, status: 'confirmed', window: { after: clinicMinutes(nowAt) + 90 } },
    { offset: 6, status: 'booked', window: { after: 10 * 60, before: 17 * 60 } },
  ];
  plan.forEach((visit, index) => {
    const pet = demoPets[index % demoPets.length]!;
    const date = clinicDay(visit.offset);
    for (const candidate of [serviceFor(pet, branch.id), serviceByCode('GEN-CHECK')]) {
      if (!candidate) continue;
      let slot = freeSlot(candidate.id, branch.id, date, visit.window);
      // Late in the day there may be nothing left after the window: any free time today will do.
      if (!slot && visit.offset === 0) slot = freeSlot(candidate.id, branch.id, date, { after: clinicMinutes(new Date()) + 30 });
      if (!slot) continue;
      book(pet, candidate, slot, branch.id, visit.status, { source: 'online', ...(visit.status === 'cancelled' ? { late_cancellation: false, cancellation_reason: 'ติดประชุม ขอเลื่อนเป็นสัปดาห์หน้า' } : {}) });
      break;
    }
  });
}

function seedWaitingList(pets: Pet[]) {
  const open = branches();
  const entries: Array<{ code: string; branch?: number; from: number; to: number; note?: string; notified?: number }> = [
    { code: 'DEN-SCALE', branch: 0, from: 0, to: 14, note: 'สะดวกช่วงบ่าย วันธรรมดา', notified: 1 },
    { code: 'SUR-NEUTER-DOG', branch: 0, from: 1, to: 21, note: 'ขอคิวผ่าตัดเร็วที่สุด' },
    { code: 'GRM-FULL', from: 0, to: 10, note: 'สาขาไหนก็ได้' },
    { code: 'EXOTIC', branch: 1, from: 0, to: 7 },
    { code: 'ANNUAL', branch: 0, from: -20, to: -2, note: 'อยากได้วันเสาร์' },
  ];
  for (const [index, entry] of entries.entries()) {
    const service = serviceByCode(entry.code);
    const branch = entry.branch === undefined ? undefined : open[Math.min(entry.branch, open.length - 1)];
    if (!service) continue;
    const pet = pets.find((candidate) => (VISITS[candidate.species] ?? []).includes(entry.code) && !waitingList().find((row) => row.pet_id === candidate.id)) ?? pets[index];
    if (!pet) continue;
    waitingList().insert({
      customer_id: pet.primary_customer_id,
      pet_id: pet.id,
      service_id: service.id,
      branch_id: branch?.id ?? null,
      staff_id: null,
      earliest_on: clinicDay(entry.from),
      latest_on: clinicDay(entry.to),
      note: entry.note ?? null,
      added_at: instant(clinicDay(Math.min(entry.from, 0) - 3 - index), '10:15'),
      last_notified_at: entry.notified ? instant(clinicDay(-1), '16:40') : null,
      notified_count: entry.notified ?? 0,
    });
  }
}

/** One screen on the wall at each branch, plus the history a real clinic's list would have. */
function seedScreens(open: Row[]) {
  const creator = table('staff_members').find((member) => member.user_id === DEMO_ACCOUNTS.owner.user_id)?.id ?? '';
  open.forEach((branch, index) => {
    const active = screens().insert({
      name: index === 0 ? 'ทีวีห้องรอ ชั้น 1' : `ทีวีห้องรอ ${branch.name}`,
      branch_id: branch.id,
      created_by_staff_id: creator,
      created_at: instant(clinicDay(-40 + index), '09:20'),
      setup_code: null,
      setup_expires_at: instant(clinicDay(-40 + index), '09:50'),
      claimed_at: instant(clinicDay(-40 + index), '09:24'),
      last_seen_at: new Date(Date.now() - 40_000).toISOString(),
    });
    displayTokens().insert({ token: `demo_dt_${uuid()}`, branch_id: branch.id, screen_id: active.id, revoked: false });
    if (index === 0) {
      screens().insert({
        name: 'จอเก่าหน้าเคาน์เตอร์',
        branch_id: branch.id,
        created_by_staff_id: creator,
        created_at: instant(clinicDay(-120), '11:00'),
        setup_code: null,
        setup_expires_at: instant(clinicDay(-120), '11:30'),
        claimed_at: instant(clinicDay(-120), '11:05'),
        last_seen_at: instant(clinicDay(-41), '19:58'),
        revoked_at: instant(clinicDay(-40), '09:15'),
      });
    }
  });
}
