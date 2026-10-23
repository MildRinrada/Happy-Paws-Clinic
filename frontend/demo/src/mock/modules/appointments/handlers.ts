import { allows, checkVersion, created, fail, invalid, noContent, notFound, page, requireAuth, requireStaff, route, type Ctx } from '../../core/http';
import { nowIso } from '../../core/db';
import {
  appointmentOrFail,
  attendanceOf,
  cancelAppointment,
  checkInAppointment,
  confirmAppointment,
  createAppointment,
  createSeries,
  defaultWindow,
  markNoShow,
  rescheduleAppointment,
  waitingStatus,
} from './appointments';
import { conflictsFor, durationFor } from './availability';
import { branchOf, customerOf, petOf, serviceOf } from './catalog';
import { appointmentDto, appointments, appointmentTypes, cancellationPolicy, ticketDto, waitingList, type AppointmentRow, type AppointmentType } from './store';
import { addDays, clinicDayOf, clinicToday, instant } from './time';
import { once } from './replay';
import './booking-routes';
import './queue-routes';

/**
 * The calendar's and the shared `/appointments/{id}` routes, the waiting list, conflicts, courses
 * and the two appointment rule sets. Booking (`/booking/*`, `/portal/appointments`) is in
 * booking-routes.ts and the queue in queue-routes.ts.
 */

const iso = (value: unknown, field: string): string => {
  const text = String(value ?? '');
  if (!text || Number.isNaN(Date.parse(text))) invalid(field, 'กรุณาระบุวันเวลาให้ถูกต้อง');
  return new Date(text).toISOString();
};

const dayParam = (value: string | null, field = 'date'): string => {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) invalid(field, 'กรุณาระบุวันที่ในรูปแบบ YYYY-MM-DD');
  return value;
};

function branchParam(ctx: Ctx, auth: { branches: string[] }): string {
  const branch = ctx.query.get('branch') ?? ctx.query.get('branch_id');
  if (!branch) invalid('branch', 'คลินิกมีหลายสาขา ต้องระบุสาขา');
  if (!branchOf(branch)) invalid('branch', 'ไม่พบสาขานี้');
  if (auth.branches.length && !auth.branches.includes(branch)) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่ได้ประจำสาขานี้');
  return branch;
}

/**
 * Who may act on one appointment through the shared routes: staff holding the scope, or the owner
 * of the pet. Anyone else gets `404`, as the backend answers for someone else's appointment.
 */
function forActor(ctx: Ctx, id: string, scope: string): { row: AppointmentRow; by: 'staff' | 'customer' } {
  const auth = requireAuth(ctx);
  const row = appointments().get(id);
  if (auth.staffId && allows(auth.scopes, scope)) {
    if (!row) notFound('นัดหมาย');
    return { row, by: 'staff' };
  }
  if (auth.customerId && row && row.customer_id === auth.customerId) return { row, by: 'customer' };
  if (auth.staffId) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่มีสิทธิ์ทำรายการนี้');
  notFound('นัดหมาย');
}

// ---- The calendar -----------------------------------------------------------------------------------

route('GET', '/staff/appointments', (ctx) => {
  const auth = requireStaff(ctx, 'appointment:read');
  const branch = branchParam(ctx, auth);
  const date = ctx.query.get('date') ? dayParam(ctx.query.get('date')) : undefined;
  const from = ctx.query.get('from');
  const to = ctx.query.get('to');
  const rows = appointments()
    .where(
      (row) =>
        row.branch_id === branch &&
        (!date || clinicDayOf(row.starts_at) === date) &&
        (!from || clinicDayOf(row.starts_at) >= from) &&
        (!to || clinicDayOf(row.starts_at) <= to) &&
        (!ctx.query.get('status') || ctx.query.get('status')!.split(',').includes(row.status)) &&
        (!ctx.query.get('staff_id') || row.staff_id === ctx.query.get('staff_id')),
    )
    .sort((a, b) => a.starts_at.localeCompare(b.starts_at))
    .map(appointmentDto);
  return page(rows, ctx.query, 100);
});

route('POST', '/staff/appointments', (ctx) => {
  requireStaff(ctx, 'appointment:write');
  const body = ctx.body ?? {};
  if (!body.pet_id) invalid('pet_id', 'กรุณาเลือกสัตว์เลี้ยง');
  if (!body.service_id) invalid('service_id', 'กรุณาเลือกบริการ');
  const row = once(
    ctx,
    'staff-appointment',
    () =>
      createAppointment({
        petId: body.pet_id,
        serviceId: body.service_id,
        startsAt: iso(body.starts_at, 'starts_at'),
        branchId: body.branch_id,
        staffId: body.staff_id || undefined,
        reason: body.reason,
        type: body.type,
        source: 'staff',
      }),
    (id) => appointments().get(id),
  );
  return created(appointmentDto(row));
});

/** `FREQ=DAILY|WEEKLY;INTERVAL=n;COUNT=n|UNTIL=YYYYMMDD`, the part of RRULE the course form writes. */
function expandRrule(first: string, rrule: string): string[] {
  const parts = Object.fromEntries(rrule.split(';').map((part) => part.split('=') as [string, string]));
  const step = parts.FREQ === 'DAILY' ? 1 : parts.FREQ === 'WEEKLY' ? 7 : 0;
  if (!step) invalid('rrule', 'รองรับเฉพาะการนัดซ้ำรายวันหรือรายสัปดาห์');
  const interval = Math.max(1, Number(parts.INTERVAL) || 1);
  const count = parts.COUNT ? Number(parts.COUNT) : undefined;
  const until = parts.UNTIL ? `${parts.UNTIL.slice(0, 4)}-${parts.UNTIL.slice(4, 6)}-${parts.UNTIL.slice(6, 8)}` : undefined;
  if (!count && !until) invalid('rrule', 'กรุณาระบุจำนวนครั้งหรือวันสิ้นสุด');
  const firstDay = clinicDayOf(first);
  const time = new Date(Date.parse(first) + 7 * 3_600_000).toISOString().slice(11, 16);
  const starts: string[] = [];
  for (let index = 0; index < 52; index += 1) {
    const day = addDays(firstDay, index * step * interval);
    if (count !== undefined && starts.length >= count) break;
    if (until && day > until) break;
    starts.push(instant(day, time));
  }
  if (starts.length < 2) invalid('rrule', 'นัดซ้ำต้องมีอย่างน้อย 2 ครั้ง');
  return starts;
}

route('POST', '/staff/appointments/recurring', (ctx) => {
  requireStaff(ctx, 'appointment:write');
  const body = ctx.body ?? {};
  if (!body.pet_id) invalid('pet_id', 'กรุณาเลือกสัตว์เลี้ยง');
  const starts = expandRrule(iso(body.first_starts_at, 'first_starts_at'), String(body.rrule ?? ''));
  const result = createSeries({
    petId: body.pet_id,
    serviceId: body.service_id,
    branchId: body.branch_id,
    staffId: body.staff_id || undefined,
    reason: body.reason,
    type: body.type,
    source: 'staff',
    starts,
  });
  return created({ series_id: result.series_id, appointments: result.appointments.map(appointmentDto) });
});

route('GET', '/staff/appointments/conflicts', (ctx) => {
  const auth = requireStaff(ctx, 'appointment:read');
  const branch = branchParam(ctx, auth);
  const date = ctx.query.get('date');
  if (date) {
    const conflicts = appointments()
      .where((row) => row.branch_id === branch && clinicDayOf(row.starts_at) === date && ['booked', 'confirmed', 'checked_in'].includes(row.status))
      .flatMap((row) =>
        conflictsFor({ startsAt: row.starts_at, endsAt: row.ends_at, branchId: branch, staffId: row.staff_id, petId: row.pet_id, appointmentId: row.id }),
      );
    return { clear: conflicts.length === 0, conflicts };
  }
  const startsAt = iso(ctx.query.get('starts_at'), 'starts_at');
  const serviceId = ctx.query.get('service_id');
  if (!serviceId || !serviceOf(serviceId)) invalid('service_id', 'ไม่พบบริการนี้');
  const endsAt = ctx.query.get('ends_at') ? iso(ctx.query.get('ends_at'), 'ends_at') : new Date(Date.parse(startsAt) + durationFor(serviceId) * 60_000).toISOString();
  const conflicts = conflictsFor({ startsAt, endsAt, branchId: branch, staffId: ctx.query.get('vet_id'), petId: ctx.query.get('pet_id') });
  return { clear: conflicts.length === 0, conflicts };
});

route('GET', '/staff/appointments/attendance-risk', (ctx) => {
  const auth = requireStaff(ctx, 'appointment:read');
  const branch = branchParam(ctx, auth);
  const date = dayParam(ctx.query.get('date'));
  if (date < clinicToday()) invalid('date', 'ดูความเสี่ยงได้เฉพาะวันนี้เป็นต้นไป');
  const policy = cancellationPolicy.get();
  const level = { high: 0, medium: 1, low: 2 } as const;
  const data = appointments()
    .where((row) => row.branch_id === branch && clinicDayOf(row.starts_at) === date && (row.status === 'booked' || row.status === 'confirmed'))
    .map((row) => {
      const history = attendanceOf(row.customer_id);
      const ahead = Math.max(0, Math.round((Date.parse(row.starts_at) - Date.parse(row.created_at ?? row.starts_at)) / 86_400_000));
      const confirmed = row.status === 'confirmed';
      const reasons: string[] = [];
      let score = 0;
      if (history.no_shows) {
        score += 30 * history.no_shows;
        reasons.push(`ไม่มาตามนัด ${history.no_shows} ครั้งใน ${policy.window_days} วันที่ผ่านมา`);
      }
      if (history.late_cancellations) {
        score += 15 * history.late_cancellations;
        reasons.push(`ยกเลิกกระชั้นชิด ${history.late_cancellations} ครั้ง`);
      }
      if (ahead >= 14) {
        score += 15;
        reasons.push(`จองล่วงหน้า ${ahead} วัน`);
      }
      if (!confirmed) {
        score += 20;
        reasons.push('ยังไม่ได้ยืนยันนัด');
      }
      score = Math.min(100, score);
      const risk = score >= 50 ? 'high' : score >= 25 ? 'medium' : 'low';
      return {
        appointment_id: row.id,
        branch_id: row.branch_id,
        starts_at: row.starts_at,
        status: row.status as 'booked' | 'confirmed',
        customer_id: row.customer_id,
        customer_name: customerOf(row.customer_id)?.display_name ?? 'ลูกค้า',
        pet_id: row.pet_id,
        pet_name: petOf(row.pet_id)?.name ?? 'สัตว์เลี้ยง',
        ...(row.staff_id ? { staff_id: row.staff_id } : {}),
        risk,
        score,
        reasons,
        signals: { no_shows: history.no_shows, late_cancellations: history.late_cancellations, days_booked_ahead: ahead, confirmed },
      };
    })
    .sort((a, b) => level[a.risk as keyof typeof level] - level[b.risk as keyof typeof level] || a.starts_at.localeCompare(b.starts_at));
  return { date, branch_id: branch, window_days: policy.window_days, data };
});

route('POST', '/staff/appointments/:id/no-show', (ctx) => {
  requireStaff(ctx, 'appointment:write');
  const result = markNoShow(ctx.params.id!);
  return { appointment: appointmentDto(result.appointment), attendance: result.attendance };
});

// ---- One appointment, staff or owner ----------------------------------------------------------------

route('GET', '/appointments/:id', (ctx) => appointmentDto(forActor(ctx, ctx.params.id!, 'appointment:read').row));

route('POST', '/appointments/:id/confirm', (ctx) => appointmentDto(confirmAppointment(forActor(ctx, ctx.params.id!, 'appointment:write').row.id)));

route('POST', '/appointments/:id/cancel', (ctx) => {
  const { row, by } = forActor(ctx, ctx.params.id!, 'appointment:write');
  return appointmentDto(cancelAppointment(row.id, ctx.body?.reason, by === 'customer' ? 'customer' : 'staff'));
});

route('POST', '/appointments/:id/reschedule', (ctx) => {
  const { row, by } = forActor(ctx, ctx.params.id!, 'appointment:write');
  checkVersion(ctx, row);
  return appointmentDto(rescheduleAppointment(row.id, { startsAt: iso(ctx.body?.starts_at, 'starts_at'), staffId: ctx.body?.staff_id || undefined, by }));
});

route('POST', '/appointments/:id/check-in', (ctx) => {
  const { row } = forActor(ctx, ctx.params.id!, 'appointment:checkin');
  const result = checkInAppointment(appointmentOrFail(row.id).id);
  return { appointment: appointmentDto(result.appointment), ticket: ticketDto(result.ticket) };
});

// ---- Waiting list -----------------------------------------------------------------------------------

route('GET', '/staff/waiting-list', (ctx) => {
  const auth = requireStaff(ctx, 'appointment:read');
  const branch = ctx.query.get('branch');
  if (branch && auth.branches.length && !auth.branches.includes(branch)) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่ได้ประจำสาขานี้');
  const status = ctx.query.get('status');
  const service = ctx.query.get('service_id');
  const rows = waitingList()
    .where((row) => (!branch || !row.branch_id || row.branch_id === branch) && (!service || row.service_id === service) && (!status || waitingStatus(row) === status))
    .sort((a, b) => a.added_at.localeCompare(b.added_at))
    .map((row) => ({
      ...row,
      status: waitingStatus(row),
      pet_name: petOf(row.pet_id)?.name ?? 'สัตว์เลี้ยง',
      service_name: serviceOf(row.service_id)?.name ?? 'บริการ',
    }));
  return page(rows, ctx.query, 50);
});

route('POST', '/staff/waiting-list', (ctx) => {
  requireStaff(ctx, 'appointment:write');
  const body = ctx.body ?? {};
  const pet = petOf(body.pet_id);
  if (!pet) invalid('pet_id', 'กรุณาเลือกสัตว์เลี้ยง');
  if (!serviceOf(body.service_id)) invalid('service_id', 'กรุณาเลือกบริการ');
  const window = defaultWindow();
  const earliest = body.earliest_on || window.earliest_on;
  const latest = body.latest_on || addDays(earliest, 14);
  if (latest < earliest) invalid('latest_on', 'วันสุดท้ายต้องไม่ก่อนวันแรก');
  if (latest > addDays(clinicToday(), 90)) invalid('latest_on', 'รอคิวได้ไม่เกิน 90 วัน');
  const duplicate = waitingList().find((row) => row.pet_id === pet.id && row.service_id === body.service_id && waitingStatus(row) === 'waiting');
  if (duplicate) fail(409, 'state_conflict', 'สัตว์เลี้ยงตัวนี้อยู่ในรายชื่อรอสำหรับบริการนี้แล้ว');
  const row = waitingList().insert({
    customer_id: pet.primary_customer_id,
    pet_id: pet.id,
    service_id: body.service_id,
    branch_id: body.branch_id || null,
    staff_id: body.staff_id || null,
    earliest_on: earliest,
    latest_on: latest,
    note: body.note || null,
    added_at: nowIso(),
    last_notified_at: null,
    notified_count: 0,
  });
  return created(row);
});

route('DELETE', '/staff/waiting-list/:id', (ctx) => {
  requireStaff(ctx, 'appointment:write');
  if (!waitingList().remove(ctx.params.id!)) notFound('รายการรอคิว');
  return noContent();
});

// ---- Rule sets --------------------------------------------------------------------------------------

const withoutId = <T extends object>(value: T) => {
  const { id: _id, ...rest } = value as T & { id?: string };
  return rest;
};

route('GET', '/staff/appointment-types', (ctx) => {
  requireStaff(ctx, 'appointment:read');
  return withoutId(appointmentTypes.get());
});

route('PUT', '/staff/appointment-types', (ctx) => {
  requireStaff(ctx, 'settings:write');
  checkVersion(ctx, appointmentTypes.get());
  const types = (ctx.body?.types ?? []) as AppointmentType[];
  if (!types.some((type) => type.code === 'regular')) invalid('types', 'ต้องมีประเภท "regular" เสมอ');
  const codes = new Set<string>();
  for (const type of types) {
    if (!/^[a-z][a-z0-9_]{1,29}$/.test(type.code)) invalid('types', `รหัส "${type.code}" ใช้ได้เฉพาะ a-z 0-9 และ _`);
    if (codes.has(type.code)) invalid('types', `รหัส "${type.code}" ซ้ำกัน`);
    codes.add(type.code);
    if (type.duration_minutes != null && (type.duration_minutes < 5 || type.duration_minutes > 480 || type.duration_minutes % 5)) {
      invalid('types', 'ระยะเวลาต้องอยู่ระหว่าง 5-480 นาที และเป็นทวีคูณของ 5');
    }
  }
  return withoutId(
    appointmentTypes.set({
      types: types.map((type) => ({
        code: type.code,
        name: type.name,
        duration_minutes: type.duration_minutes ?? null,
        online_booking: Boolean(type.online_booking),
        lead_time_minutes: type.lead_time_minutes ?? null,
        queue_priority: type.queue_priority ?? 'normal',
      })),
    }),
  );
});

route('GET', '/staff/policies/cancellation', (ctx) => {
  requireStaff(ctx, 'settings:read');
  return withoutId(cancellationPolicy.get());
});

route('PUT', '/staff/policies/cancellation', (ctx) => {
  requireStaff(ctx, 'settings:write');
  checkVersion(ctx, cancellationPolicy.get());
  const body = ctx.body ?? {};
  const whole = (field: string, min: number, max: number) => {
    const value = Number(body[field]);
    if (!Number.isInteger(value) || value < min || value > max) invalid(field, `ค่าต้องอยู่ระหว่าง ${min} ถึง ${max}`);
    return value;
  };
  return withoutId(
    cancellationPolicy.set({
      cutoff_hours: whole('cutoff_hours', 0, 168),
      fee_satang: whole('fee_satang', 0, 10_000_000),
      free_allowance: whole('free_allowance', 0, 20),
      window_days: whole('window_days', 1, 730),
    }),
  );
});
