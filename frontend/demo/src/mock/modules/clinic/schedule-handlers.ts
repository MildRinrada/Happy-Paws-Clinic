import { table, today, uuid, type Row } from '../../core/db';
import { checkVersion, created, fail, invalid, notFound, requireStaff, route, type Ctx } from '../../core/http';
import {
  addDays,
  branches,
  clinicDateOf,
  clinicInstant,
  mondayOf,
  openingHoursOn,
  overlaps,
  shifts,
  staffMembers,
  timeOff,
  vetsOnShiftAt,
  WEEKDAYS,
  weekdayOf,
  workingVetsOn,
  type ShiftRow,
} from './data';
import { audit, compact, isDate, isInstant } from './util';

/**
 * The roster: `/staff/schedules*` on `schedule:read` / `schedule:write`, and the demand forecast
 * the roster page links to. What is written here is what booking offers (see `workingVetsOn`).
 */

const shiftAnswer = ({ id, staff_id, branch_id, starts_at, ends_at, version }: ShiftRow) => ({ id, staff_id, branch_id, starts_at, ends_at, version });

route('GET', '/staff/schedules', (ctx) => {
  const auth = requireStaff(ctx, 'schedule:read');
  const from = ctx.query.get('from') ?? '';
  const to = ctx.query.get('to') ?? '';
  if (!isDate(from)) invalid('from', 'กรุณาระบุวันเริ่มต้น');
  if (!isDate(to) || to < from) invalid('to', 'วันสิ้นสุดต้องไม่ก่อนวันเริ่มต้น');
  const branch = ctx.query.get('branch') || undefined;
  if (branch && !branches().get(branch)) invalid('branch', 'ไม่พบสาขานี้');
  const reach = auth.branches;
  const start = clinicInstant(from, '00:00');
  const end = clinicInstant(addDays(to, 1), '00:00');

  const inSpan = shifts().where(
    (shift) => overlaps(shift.starts_at, shift.ends_at, start, end) && (!branch || shift.branch_id === branch) && (!reach.length || reach.includes(shift.branch_id)),
  );
  const leave = timeOff().where((entry) => overlaps(entry.starts_at, entry.ends_at, start, end));
  const people = new Set([...inSpan.map((shift) => shift.staff_id), ...leave.map((entry) => entry.staff_id)]);

  const staff = staffMembers()
    .where((member) => people.has(member.id))
    // Leave alone does not put someone from another branch on this branch's roster.
    .filter((member) => inSpan.some((shift) => shift.staff_id === member.id) || !branch || !member.branches.length || member.branches.includes(branch))
    .sort((a, b) => a.display_name.localeCompare(b.display_name, 'th'))
    .map((member) => ({
      staff_id: member.id,
      display_name: member.display_name,
      department: member.department,
      shifts: inSpan.filter((shift) => shift.staff_id === member.id).sort((a, b) => Date.parse(a.starts_at) - Date.parse(b.starts_at)).map(shiftAnswer),
      time_off: leave.filter((entry) => entry.staff_id === member.id).map((entry) => compact({ id: entry.id, staff_id: entry.staff_id, starts_at: entry.starts_at, ends_at: entry.ends_at, reason: entry.reason })),
    }));

  return { from, to, staff };
});

// ---- Clashes ---------------------------------------------------------------------------------------

function refuseClashes(blocks: Array<{ staff_id: string; starts_at: string; ends_at: string }>, ignoreId?: string): void {
  const clashes = blocks.flatMap((block) =>
    shifts()
      .where((shift) => shift.id !== ignoreId && shift.staff_id === block.staff_id && overlaps(shift.starts_at, shift.ends_at, block.starts_at, block.ends_at))
      .map((shift) => ({ staff_id: shift.staff_id, shift_id: shift.id, starts_at: shift.starts_at, ends_at: shift.ends_at })),
  );
  if (clashes.length) fail(409, 'shift_overlap', 'ช่วงเวลานี้ทับกับกะที่มีอยู่แล้ว', { details: { conflicts: clashes } });
  const leave = blocks.flatMap((block) =>
    timeOff()
      .where((entry) => entry.staff_id === block.staff_id && overlaps(entry.starts_at, entry.ends_at, block.starts_at, block.ends_at))
      .map((entry) => ({ staff_id: entry.staff_id, time_off_id: entry.id, starts_at: entry.starts_at, ends_at: entry.ends_at })),
  );
  if (leave.length) fail(409, 'staff_on_time_off', 'พนักงานลาในช่วงเวลานี้', { details: { time_off: leave } });
}

/** Appointments of a person inside a span that the roster no longer covers, and who could take them. */
function appointmentsIn(staffId: string, start: string, end: string) {
  return table('appointments')
    .where(
      (appointment: Row) =>
        appointment.staff_id === staffId &&
        !['cancelled', 'no_show', 'completed'].includes(appointment.status) &&
        overlaps(appointment.starts_at, appointment.ends_at, start, end),
    )
    .map((appointment: Row) => ({
      appointment_id: appointment.id,
      branch_id: appointment.branch_id,
      service_id: appointment.service_id,
      starts_at: appointment.starts_at,
      ends_at: appointment.ends_at,
      suggested_staff_ids: vetsOnShiftAt(appointment.starts_at, appointment.branch_id, appointment.ends_at).filter((id) => id !== staffId),
    }));
}

// ---- Shifts ----------------------------------------------------------------------------------------

const BYDAY: Record<string, string> = { MO: 'monday', TU: 'tuesday', WE: 'wednesday', TH: 'thursday', FR: 'friday', SA: 'saturday', SU: 'sunday' };

/** The dates a weekly RRULE (`FREQ=WEEKLY;BYDAY=MO,WE;UNTIL=20261231` or `COUNT=n`) lands on, from `first`. */
function ruleDates(rrule: string, first: string): string[] {
  const parts = Object.fromEntries(rrule.split(';').map((part) => part.split('=') as [string, string]));
  if (parts.FREQ !== 'WEEKLY' && parts.FREQ !== 'DAILY') invalid('rrule', 'รองรับเฉพาะการทำซ้ำรายวันหรือรายสัปดาห์');
  const days = parts.FREQ === 'DAILY' ? [...WEEKDAYS] : (parts.BYDAY ?? '').split(',').map((code) => BYDAY[code]).filter(Boolean);
  if (!days.length) days.push(weekdayOf(first));
  const until = parts.UNTIL ? `${parts.UNTIL.slice(0, 4)}-${parts.UNTIL.slice(4, 6)}-${parts.UNTIL.slice(6, 8)}` : undefined;
  const count = parts.COUNT ? Number(parts.COUNT) : undefined;
  if (!until && !count) invalid('rrule', 'กรุณาระบุวันสิ้นสุดของการทำซ้ำ');
  const last = until ?? addDays(first, 366);
  if (last > addDays(first, 366)) invalid('rrule', 'ทำซ้ำได้ไม่เกิน 1 ปี');
  const dates: string[] = [];
  for (let date = first; date <= last && (!count || dates.length < count); date = addDays(date, 1)) {
    if (days.includes(weekdayOf(date))) dates.push(date);
  }
  return dates;
}

/** An instant moved to another clinic date, keeping its wall-clock time. */
const onDate = (instant: string, date: string) => `${date}T${new Date(Date.parse(instant) + 7 * 3_600_000).toISOString().slice(11, 19)}+07:00`;

function staffAt(ctx: Ctx, staffId: unknown, branchId: unknown) {
  const auth = requireStaff(ctx, 'schedule:write');
  const member = staffMembers().get(String(staffId ?? ''));
  if (!member || member.status !== 'active') invalid('staff_id', 'ไม่พบพนักงาน');
  const branch = branches().get(String(branchId ?? ''));
  if (!branch) invalid('branch_id', 'ไม่พบสาขา');
  if (auth.branches.length && !auth.branches.includes(branch.id)) fail(403, 'insufficient_scope', 'บัญชีนี้จัดตารางสาขานี้ไม่ได้');
  return { member, branch };
}

route('POST', '/staff/schedules/shifts', (ctx) => {
  const body = ctx.body ?? {};
  const { member, branch } = staffAt(ctx, body.staff_id, body.branch_id);
  if (!isInstant(body.starts_at)) invalid('starts_at', 'เวลาเริ่มไม่ถูกต้อง');
  if (!isInstant(body.ends_at) || Date.parse(body.ends_at) <= Date.parse(body.starts_at)) invalid('ends_at', 'เวลาเลิกต้องหลังเวลาเริ่ม');
  if (Date.parse(body.ends_at) - Date.parse(body.starts_at) > 16 * 3_600_000) invalid('ends_at', 'กะหนึ่งยาวได้ไม่เกิน 16 ชั่วโมง');

  // A break splits the shift into two blocks, before and after it.
  let pieces: Array<[string, string]> = [[body.starts_at, body.ends_at]];
  if (body.break) {
    const { starts_at: breakStart, ends_at: breakEnd } = body.break;
    if (!isInstant(breakStart) || !isInstant(breakEnd) || Date.parse(breakStart) <= Date.parse(body.starts_at) || Date.parse(breakEnd) >= Date.parse(body.ends_at) || Date.parse(breakEnd) <= Date.parse(breakStart)) {
      invalid('break', 'เวลาพักต้องอยู่ภายในกะ');
    }
    pieces = [[body.starts_at, breakStart], [breakEnd, body.ends_at]];
  }

  const first = clinicDateOf(body.starts_at);
  const dates = body.rrule ? ruleDates(String(body.rrule), first) : [first];
  const blocks = dates.flatMap((date) =>
    pieces.map(([start, end]) => ({ id: uuid(), staff_id: member.id, branch_id: branch.id, starts_at: onDate(start, date), ends_at: onDate(end, addDays(date, clinicDateOf(end) > clinicDateOf(start) ? 1 : 0)), version: 1 })),
  );
  refuseClashes(blocks);
  const saved = shifts().insertMany(blocks);
  audit(ctx, 'schedule.shifts_created', 'shift', `เพิ่มกะของ ${member.display_name} ${dates.length} วัน`);
  return created({ data: saved.map(shiftAnswer) });
});

function editableShift(ctx: Ctx): ShiftRow {
  const shift = shifts().get(ctx.params.id);
  if (!shift) notFound('กะ');
  staffAt(ctx, shift.staff_id, shift.branch_id);
  checkVersion(ctx, shift);
  return shift;
}

route('PATCH', '/staff/schedules/shifts/:id', (ctx) => {
  const shift = editableShift(ctx);
  const body = ctx.body ?? {};
  if (!isInstant(body.starts_at)) invalid('starts_at', 'เวลาเริ่มไม่ถูกต้อง');
  if (!isInstant(body.ends_at) || Date.parse(body.ends_at) <= Date.parse(body.starts_at)) invalid('ends_at', 'เวลาเลิกต้องหลังเวลาเริ่ม');
  refuseClashes([{ staff_id: shift.staff_id, starts_at: body.starts_at, ends_at: body.ends_at }], shift.id);
  // The appointments in the part of the old block the new one no longer covers.
  const lost = [
    ...(Date.parse(body.starts_at) > Date.parse(shift.starts_at) ? appointmentsIn(shift.staff_id, shift.starts_at, body.starts_at) : []),
    ...(Date.parse(body.ends_at) < Date.parse(shift.ends_at) ? appointmentsIn(shift.staff_id, body.ends_at, shift.ends_at) : []),
  ];
  const saved = shifts().update(shift.id, { starts_at: body.starts_at, ends_at: body.ends_at })!;
  audit(ctx, 'schedule.shift_changed', 'shift', `แก้เวลากะของ ${staffMembers().get(shift.staff_id)?.display_name ?? ''}`, shift.id);
  return { shift: shiftAnswer(saved), conflicts: lost };
});

route('DELETE', '/staff/schedules/shifts/:id', (ctx) => {
  const shift = editableShift(ctx);
  const conflicts = appointmentsIn(shift.staff_id, shift.starts_at, shift.ends_at);
  shifts().remove(shift.id);
  audit(ctx, 'schedule.shift_removed', 'shift', `ลบกะของ ${staffMembers().get(shift.staff_id)?.display_name ?? ''}`, shift.id);
  return { shift_id: shift.id, conflicts };
});

route('POST', '/staff/schedules/shifts/copy-week', (ctx) => {
  const auth = requireStaff(ctx, 'schedule:write');
  const body = ctx.body ?? {};
  const branch = branches().get(String(body.branch_id ?? ''));
  if (!branch) invalid('branch_id', 'ไม่พบสาขา');
  if (auth.branches.length && !auth.branches.includes(branch.id)) fail(403, 'insufficient_scope', 'บัญชีนี้จัดตารางสาขานี้ไม่ได้');
  if (!isDate(body.from_week) || mondayOf(body.from_week) !== body.from_week) invalid('from_week', 'สัปดาห์ต้นทางต้องเป็นวันจันทร์');
  if (!isDate(body.to_week) || mondayOf(body.to_week) !== body.to_week) invalid('to_week', 'สัปดาห์ปลายทางต้องเป็นวันจันทร์');
  if (body.to_week === body.from_week) invalid('to_week', 'สัปดาห์ปลายทางต้องไม่ใช่สัปดาห์เดียวกัน');
  const only: string[] | undefined = Array.isArray(body.staff_ids) && body.staff_ids.length ? body.staff_ids.map(String) : undefined;
  const shiftDays = (Date.parse(body.to_week) - Date.parse(body.from_week)) / 86_400_000;

  const source = shifts().where(
    (shift) =>
      shift.branch_id === branch.id &&
      clinicDateOf(shift.starts_at) >= body.from_week &&
      clinicDateOf(shift.starts_at) <= addDays(body.from_week, 6) &&
      (!only || only.includes(shift.staff_id)) &&
      staffMembers().get(shift.staff_id)?.status === 'active',
  );
  const now = Date.now();
  let skipped = 0;
  const copies = source.flatMap((shift) => {
    const date = addDays(clinicDateOf(shift.starts_at), shiftDays);
    const startsAt = onDate(shift.starts_at, date);
    // A day that has already begun is left alone.
    if (Date.parse(clinicInstant(date, '00:00')) <= now && date <= today()) {
      skipped += 1;
      return [];
    }
    return [{ id: uuid(), staff_id: shift.staff_id, branch_id: shift.branch_id, starts_at: startsAt, ends_at: onDate(shift.ends_at, addDays(clinicDateOf(shift.ends_at), shiftDays)), version: 1 }];
  });
  refuseClashes(copies);
  const saved = shifts().insertMany(copies);
  audit(ctx, 'schedule.week_copied', 'shift', `คัดลอกตาราง${branch.name} สัปดาห์ ${body.from_week} ไป ${body.to_week}`);
  return { data: saved.map(shiftAnswer), skipped_started: skipped };
});

route('POST', '/staff/schedules/time-off', (ctx) => {
  requireStaff(ctx, 'schedule:write');
  const body = ctx.body ?? {};
  const member = staffMembers().get(String(body.staff_id ?? ''));
  if (!member || member.status !== 'active') invalid('staff_id', 'ไม่พบพนักงาน');
  if (!isInstant(body.starts_at)) invalid('starts_at', 'เวลาเริ่มลาไม่ถูกต้อง');
  if (!isInstant(body.ends_at) || Date.parse(body.ends_at) <= Date.parse(body.starts_at)) invalid('ends_at', 'เวลาสิ้นสุดต้องหลังเวลาเริ่ม');
  const reason = typeof body.reason === 'string' && body.reason.trim() ? body.reason.trim() : undefined;

  const entry = timeOff().insert(compact({ staff_id: member.id, starts_at: body.starts_at, ends_at: body.ends_at, reason }) as never);
  // Leave takes effect at once: blocks inside it go, blocks across its edges are cut back.
  let cancelled = 0;
  const remaining: ShiftRow[] = [];
  for (const shift of shifts().where((row) => row.staff_id === member.id && overlaps(row.starts_at, row.ends_at, body.starts_at, body.ends_at))) {
    const before = Date.parse(shift.starts_at) < Date.parse(body.starts_at);
    const after = Date.parse(shift.ends_at) > Date.parse(body.ends_at);
    if (!before && !after) {
      shifts().remove(shift.id);
      cancelled += 1;
    } else if (before && after) {
      remaining.push(shifts().update(shift.id, { ends_at: body.starts_at })!);
      remaining.push(shifts().insert({ staff_id: shift.staff_id, branch_id: shift.branch_id, starts_at: body.ends_at, ends_at: shift.ends_at, version: 1 }));
    } else {
      remaining.push(shifts().update(shift.id, before ? { ends_at: body.starts_at } : { starts_at: body.ends_at })!);
    }
  }
  audit(ctx, 'schedule.time_off_recorded', 'time_off', `บันทึกการลาของ ${member.display_name}`, entry.id);
  return created({
    time_off: compact({ id: entry.id, staff_id: entry.staff_id, starts_at: entry.starts_at, ends_at: entry.ends_at, reason: entry.reason }),
    shifts_cancelled: cancelled,
    shifts_remaining: remaining.map(shiftAnswer),
    conflicts: appointmentsIn(member.id, body.starts_at, body.ends_at),
  });
});

// ---- Assignment rules ------------------------------------------------------------------------------

const rules = () => table('assignment_rules');
const ruleSet = () => rules().get('singleton') ?? rules().insert({ id: 'singleton', version: 0, rules: [] });

route('GET', '/staff/schedules/assignment-rules', (ctx) => {
  requireStaff(ctx, 'schedule:read');
  const { version, rules: list } = ruleSet();
  return { version, rules: list };
});

route('PUT', '/staff/schedules/assignment-rules', (ctx) => {
  requireStaff(ctx, 'schedule:write');
  const current = ruleSet();
  checkVersion(ctx, current as any);
  const list = ctx.body?.rules;
  if (!Array.isArray(list)) invalid('rules', 'รูปแบบกติกาไม่ถูกต้อง');
  const cleaned = list.map((rule: Row, index: number) => {
    const species = typeof rule.species === 'string' && rule.species.trim() ? rule.species.trim() : null;
    const serviceId = typeof rule.service_id === 'string' && rule.service_id ? rule.service_id : null;
    if (!species && !serviceId) invalid(`rules[${index}]`, 'แต่ละกติกาต้องระบุชนิดสัตว์หรือบริการ');
    if (serviceId && !table('services').get(serviceId)) invalid(`rules[${index}].service_id`, 'ไม่พบบริการ');
    const staffIds: string[] = Array.isArray(rule.staff_ids) ? [...new Set(rule.staff_ids.map(String))] as string[] : [];
    if (!staffIds.length) invalid(`rules[${index}].staff_ids`, 'กรุณาเลือกผู้รับเคสอย่างน้อยหนึ่งคน');
    if (staffIds.some((id) => !staffMembers().get(id))) invalid(`rules[${index}].staff_ids`, 'ไม่พบพนักงาน');
    return { species, service_id: serviceId, staff_ids: staffIds };
  });
  const saved = rules().put({ id: 'singleton', version: (current.version as number) + 1, rules: cleaned });
  audit(ctx, 'schedule.assignment_rules_replaced', 'settings', 'แก้กติกาจ่ายเคสอัตโนมัติ');
  return { version: saved.version, rules: saved.rules };
});

// ---- Demand forecast -------------------------------------------------------------------------------

/** Walk-ins an hour usually brings, by weekday (0 = Monday) and hour: busier after work and on weekends. */
function usualWalkIns(weekday: number, hour: number): number {
  const weekend = weekday >= 5;
  const base = hour < 11 ? 0.6 : hour < 14 ? 1.1 : hour < 17 ? 0.9 : 1.6;
  return Math.round(base * (weekend ? 1.6 : 1) * 10) / 10;
}

const HIGH_LOAD_PER_VET = 3;

route('GET', '/staff/stats/demand-forecast', (ctx) => {
  const auth = requireStaff(ctx, 'stats:read');
  const branchId = ctx.query.get('branch') ?? '';
  if (!branches().get(branchId)) invalid('branch', 'ไม่พบสาขานี้');
  if (auth.branches.length && !auth.branches.includes(branchId)) fail(403, 'insufficient_scope', 'บัญชีนี้ดูสาขานี้ไม่ได้');
  const from = ctx.query.get('from') ?? today();
  const to = ctx.query.get('to') ?? addDays(from, 6);
  if (!isDate(from) || from < today()) invalid('from', 'วันเริ่มต้องเป็นวันนี้หรือหลังจากนี้');
  if (!isDate(to) || to < from) invalid('to', 'วันสิ้นสุดต้องไม่ก่อนวันเริ่ม');
  if (to > addDays(from, 13)) invalid('to', 'ดูล่วงหน้าได้ไม่เกิน 14 วัน');

  const booked = table('appointments').where(
    (appointment: Row) => appointment.branch_id === branchId && !['cancelled', 'no_show'].includes(appointment.status),
  );

  const days = [];
  for (let date = from; date <= to; date = addDays(date, 1)) {
    const open = openingHoursOn(date, branchId);
    const vets = workingVetsOn(date, branchId);
    const weekday = WEEKDAYS.indexOf(weekdayOf(date));
    const hours: Array<Record<string, any>> = [];
    if (open) {
      for (let hour = Number(open.opens.slice(0, 2)); hour < Number(open.closes.slice(0, 2)); hour += 1) {
        const startsAt = clinicInstant(date, `${String(hour).padStart(2, '0')}:00`);
        const endsAt = clinicInstant(date, `${String(hour).padStart(2, '0')}:59`);
        const bookings = booked.filter((appointment: Row) => Date.parse(appointment.starts_at) >= Date.parse(startsAt) && Date.parse(appointment.starts_at) <= Date.parse(endsAt)).length;
        const walkIns = usualWalkIns(weekday, hour);
        const total = Math.round((bookings + walkIns) * 10) / 10;
        const onShift = vets.filter((vet) => vet.shifts.some((shift) => Date.parse(shift.starts_at) <= Date.parse(startsAt) && Date.parse(shift.ends_at) > Date.parse(startsAt))).length;
        const load = onShift ? Math.round((total / onShift) * 10) / 10 : undefined;
        hours.push(
          compact({
            hour,
            starts_at: startsAt,
            booked_appointments: bookings,
            expected_walk_ins: walkIns,
            expected_total: total,
            vets_on_shift: onShift,
            load_per_vet: load,
            high_load: load === undefined ? total >= 1 : load > HIGH_LOAD_PER_VET,
          }),
        );
      }
    }
    const sum = (field: string) => Math.round(hours.reduce((total, hour) => total + (hour[field] as number), 0) * 10) / 10;
    days.push({
      date,
      booked_appointments: sum('booked_appointments'),
      expected_walk_ins: sum('expected_walk_ins'),
      expected_total: sum('expected_total'),
      high_load_hours: hours.filter((hour) => hour.high_load).length,
      hours,
    });
  }

  return { branch_id: branchId, from, to, history_weeks: 8, high_load_per_vet: HIGH_LOAD_PER_VET, days };
});
