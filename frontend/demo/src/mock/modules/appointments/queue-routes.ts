import { displayTokens } from '../../core/auth';
import { nowIso, uuid } from '../../core/db';
import { checkVersion, created, fail, invalid, noContent, requireAuth, requireStaff, route, type Ctx } from '../../core/http';
import { branchOf, closedOn, hoursOn, isVet, petOf, serviceOf, shiftsOn, staffOf } from './catalog';
import {
  assignTicket,
  AWAITING,
  boardOf,
  callTicket,
  issueTicket,
  leaveTicket,
  setPriority,
  skipTicket,
  ticketOrFail,
} from './queue';
import { once } from './replay';
import { screens, ticketDto, tickets, type ScreenRow, type TicketPriority, type TicketStatus } from './store';
import { addDays, clinicMinutes, clinicToday, instant, minutesOf } from './time';

/**
 * The queue console, its history, the waiting-room screens and their pairing, and the public
 * wait estimate. Changes go through queue.ts, which announces them on the bus.
 */

const STATUSES: TicketStatus[] = ['waiting', 'called', 'in_room', 'skipped', 'served', 'left'];
const PRIORITIES: TicketPriority[] = ['normal', 'urgent', 'emergency'];

function branchFor(ctx: Ctx, auth: { branches: string[] }, value = ctx.query.get('branch')): string {
  if (!value) invalid('branch', 'คลินิกมีหลายสาขา ต้องระบุสาขา');
  if (!branchOf(value)) invalid('branch', 'ไม่พบสาขานี้');
  if (auth.branches.length && !auth.branches.includes(value)) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่ได้ประจำสาขานี้');
  return value;
}

route('GET', '/staff/queue', (ctx) => {
  const auth = requireStaff(ctx, 'queue:read');
  const branch = branchFor(ctx, auth);
  const date = ctx.query.get('date') || clinicToday();
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) invalid('date', 'กรุณาระบุวันที่ในรูปแบบ YYYY-MM-DD');
  const status = ctx.query.get('status');
  const wanted = status ? status.split(',').map((part) => part.trim()).filter(Boolean) : undefined;
  if (wanted?.some((part) => !STATUSES.includes(part as TicketStatus))) {
    invalid('status', 'สถานะต้องเป็น waiting, called, in_room, skipped, served หรือ left คั่นด้วยจุลภาค');
  }
  const includeFinished = ctx.query.get('include_finished') === 'true' || Boolean(wanted);
  let board = boardOf(branch, date, includeFinished);
  if (wanted) board = board.filter((ticket) => wanted.includes(ticket.status));
  const room = ctx.query.get('room');
  if (room) board = board.filter((ticket) => ticket.room_id === room);
  return {
    branch_id: branch,
    service_date: date,
    ...(room ? { room_id: room } : {}),
    waiting_count: board.filter((ticket) => AWAITING.includes(ticket.status)).length,
    tickets: board.map(ticketDto),
  };
});

function priorityOf(value: unknown, reason: unknown): { priority: TicketPriority; reason?: string } {
  const priority = (value ?? 'normal') as TicketPriority;
  if (!PRIORITIES.includes(priority)) invalid('priority', 'ระดับความเร่งด่วนไม่ถูกต้อง');
  const text = typeof reason === 'string' ? reason.trim() : '';
  if (priority !== 'normal' && !text) fail(409, 'invalid_priority', 'กรุณาระบุเหตุผลเมื่อเพิ่มความเร่งด่วน', { field: 'priority_reason' });
  return { priority, ...(text ? { reason: text } : {}) };
}

route('POST', '/staff/queue/tickets', (ctx) => {
  const auth = requireStaff(ctx, 'queue:write');
  const body = ctx.body ?? {};
  const pet = petOf(body.pet_id);
  if (!pet) invalid('pet_id', 'กรุณาเลือกสัตว์เลี้ยง');
  if (!serviceOf(body.service_id)) invalid('service_id', 'กรุณาเลือกบริการ');
  const branch = branchFor(ctx, auth, body.branch_id);
  const { priority, reason } = priorityOf(body.priority, body.priority_reason);
  if (tickets().find((row) => row.pet_id === pet.id && row.service_date === clinicToday() && ['waiting', 'called', 'skipped', 'in_room'].includes(row.status))) {
    fail(409, 'state_conflict', `น้อง${pet.name}มีบัตรคิววันนี้อยู่แล้ว`);
  }
  const ticket = once(
    ctx,
    'walk-in',
    () => issueTicket({ branchId: branch, petId: pet.id, customerId: pet.primary_customer_id, serviceId: body.service_id, priority, priorityReason: reason }),
    (id) => tickets().get(id),
  );
  return created(ticketDto(ticket));
});

/** One ticket the caller may act on: its branch must be one of theirs, and `If-Match` must agree. */
function ticketFor(ctx: Ctx, scope: string) {
  const auth = requireStaff(ctx, scope);
  const ticket = ticketOrFail(ctx.params.id!);
  if (auth.branches.length && !auth.branches.includes(ticket.branch_id)) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่ได้ประจำสาขานี้');
  checkVersion(ctx, ticket);
  return { auth, ticket };
}

route('POST', '/staff/queue/:id/call', (ctx) => {
  const { auth, ticket } = ticketFor(ctx, 'queue:call');
  // A vet calling a ticket nobody was given takes it; the counter calling it leaves it open.
  const caller = staffOf(auth.staffId);
  return ticketDto(callTicket(ticket.id, caller && isVet(caller) ? caller.id : undefined));
});

route('POST', '/staff/queue/:id/skip', (ctx) => ticketDto(skipTicket(ticketFor(ctx, 'queue:call').ticket.id)));

route('POST', '/staff/queue/:id/leave', (ctx) => {
  const reason = typeof ctx.body?.reason === 'string' ? ctx.body.reason.trim().slice(0, 300) : undefined;
  return ticketDto(leaveTicket(ticketFor(ctx, 'queue:write').ticket.id, reason || undefined));
});

route('POST', '/staff/queue/:id/priority', (ctx) => {
  const { ticket } = ticketFor(ctx, 'queue:write');
  const { priority, reason } = priorityOf(ctx.body?.priority, ctx.body?.reason);
  return ticketDto(setPriority(ticket.id, priority, reason));
});

route('POST', '/staff/queue/:id/assign', (ctx) => {
  const { ticket } = ticketFor(ctx, 'queue:write');
  const staffId = ctx.body?.staff_id || undefined;
  const roomId = ctx.body?.room_id || undefined;
  if (!staffId && !roomId) invalid('staff_id', 'กรุณาเลือกหมอหรือห้องตรวจ');
  if (staffId) {
    const member = staffOf(staffId);
    if (!member || !isVet(member)) invalid('staff_id', 'ไม่พบสัตวแพทย์นี้');
  }
  return ticketDto(assignTicket(ticket.id, { staffId, roomId }));
});

// ---- Waiting-room screens (docs/contract-extensions.md § 30) ---------------------------------------

const SETUP_MINUTES = 30;
const CODE_LETTERS = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';

function setupCode(): string {
  const pick = () => CODE_LETTERS[Math.floor(Math.random() * CODE_LETTERS.length)];
  const half = () => Array.from({ length: 4 }, pick).join('');
  return `${half()}-${half()}`;
}

const normalise = (code: string) => code.toUpperCase().replace(/[^A-Z0-9]/g, '');

export function screenStatus(row: ScreenRow): 'pending_setup' | 'setup_expired' | 'active' | 'expired' | 'revoked' {
  const now = nowIso();
  if (row.revoked_at) return 'revoked';
  if (row.claimed_at) return row.expires_at && row.expires_at <= now ? 'expired' : 'active';
  return row.setup_expires_at && row.setup_expires_at <= now ? 'setup_expired' : 'pending_setup';
}

function screenSummary(row: ScreenRow) {
  const { setup_code: _code, ...rest } = row;
  return { ...rest, status: screenStatus(row) };
}

route('GET', '/staff/queue/displays', (ctx) => {
  const auth = requireStaff(ctx, 'queue:write');
  const branch = branchFor(ctx, auth);
  return {
    data: screens()
      .where((row) => row.branch_id === branch)
      .sort((a, b) => b.created_at.localeCompare(a.created_at))
      .map(screenSummary),
  };
});

route('POST', '/staff/queue/displays', (ctx) => {
  const auth = requireStaff(ctx, 'queue:write');
  const body = ctx.body ?? {};
  const name = String(body.name ?? '').trim();
  if (!name) invalid('name', 'กรุณาตั้งชื่อจอ');
  const branch = branchFor(ctx, auth, body.branch_id);
  if (body.expires_at && Date.parse(body.expires_at) <= Date.now()) invalid('expires_at', 'วันหมดอายุต้องเป็นวันในอนาคต');
  const code = setupCode();
  const row = screens().insert({
    name,
    branch_id: branch,
    created_by_staff_id: auth.staffId,
    created_at: nowIso(),
    setup_code: normalise(code),
    setup_expires_at: new Date(Date.now() + SETUP_MINUTES * 60_000).toISOString(),
    ...(body.expires_at ? { expires_at: new Date(body.expires_at).toISOString() } : {}),
  });
  return created({
    id: row.id,
    name: row.name,
    branch_id: row.branch_id,
    status: screenStatus(row),
    setup_code: code,
    setup_expires_at: row.setup_expires_at,
    ...(row.expires_at ? { expires_at: row.expires_at } : {}),
    created_at: row.created_at,
  });
});

route('DELETE', '/staff/queue/displays/:id', (ctx) => {
  const auth = requireStaff(ctx, 'queue:write');
  const row = screens().get(ctx.params.id!);
  if (!row || (auth.branches.length && !auth.branches.includes(row.branch_id))) fail(404, 'not_found', 'ไม่พบจอนี้');
  if (!row.revoked_at) screens().update(row.id, { revoked_at: nowIso(), setup_code: null });
  for (const token of displayTokens().where((item) => item.screen_id === row.id && !item.revoked)) displayTokens().update(token.id, { revoked: true });
  return noContent();
});

/** Pairs a screen with its own token, as the TV's setup page asks. */
export function pairScreen(screenId: string): string {
  const token = `demo_dt_${uuid()}`;
  const row = screens().get(screenId)!;
  displayTokens().insert({ token, branch_id: row.branch_id, screen_id: row.id, revoked: false });
  return token;
}

route('POST', '/queue/displays/claim', (ctx) => {
  const code = normalise(String(ctx.body?.code ?? ''));
  const refused = (): never => invalid('code', 'รหัสตั้งค่าจอไม่ถูกต้องหรือหมดอายุแล้ว กรุณาสร้างรหัสใหม่ที่หน้าจอเรียกคิว');
  if (!code) refused();
  const row = screens().find((item) => item.setup_code === code) ?? refused();
  if (screenStatus(row) !== 'pending_setup') refused();
  screens().update(row.id, { setup_code: null, claimed_at: nowIso(), last_seen_at: nowIso() });
  const token = pairScreen(row.id);
  return { display_id: row.id, name: row.name, branch_id: row.branch_id, token, ...(row.expires_at ? { expires_at: row.expires_at } : {}) };
});

route('GET', '/queue/display-board', (ctx) => {
  const auth = requireAuth(ctx);
  if (!auth.display) fail(403, 'insufficient_scope', 'อ่านได้เฉพาะจอเรียกคิวที่ตั้งค่าแล้ว');
  const screen = screens().get(auth.display.screenId);
  if (!screen || screenStatus(screen) !== 'active') fail(401, 'unauthenticated', 'จอนี้ถูกยกเลิกหรือหมดอายุแล้ว');
  const requested = ctx.query.get('branch');
  if (requested && requested !== auth.display.branchId) fail(403, 'insufficient_scope', 'จอนี้ไม่ได้ผูกกับสาขานี้');
  const branch = branchOf(auth.display.branchId);
  if (!branch) fail(404, 'not_found', 'ไม่พบสาขา');
  if (!screen.last_seen_at || Date.now() - Date.parse(screen.last_seen_at) > 60_000) screens().update(screen.id, { last_seen_at: nowIso() });
  const board = boardOf(branch.id);
  return {
    branch_id: branch.id,
    branch_name: branch.name,
    service_date: clinicToday(),
    waiting_count: board.filter((ticket) => AWAITING.includes(ticket.status)).length,
    exam_rooms: branch.exam_rooms ?? [],
    tickets: board.map((ticket) =>
      ticket.status === 'called' || ticket.status === 'in_room'
        ? {
            number: ticket.number,
            status: ticket.status,
            ...(ticket.room_id ? { room_id: ticket.room_id } : {}),
            ...(staffOf(ticket.staff_id)?.display_name ? { vet_name: staffOf(ticket.staff_id)!.display_name } : {}),
            ...(ticket.called_at ? { called_at: ticket.called_at } : {}),
          }
        : { number: ticket.number, status: 'waiting' as const },
    ),
  };
});

// ---- Wait estimate (public, docs/contract-extensions.md § 37) -----------------------------------------

/** The median visit the clinic's history shows, in minutes; the demo's history is the seed's. */
const CONSULTATION_MINUTES = 18;

function nextOpening(branchId: string): string | undefined {
  const today = clinicToday();
  const now = clinicMinutes(new Date());
  for (let offset = 0; offset <= 14; offset += 1) {
    const day = addDays(today, offset);
    const hours = hoursOn(day);
    if (closedOn(day, branchId) || !hours.opens) continue;
    if (offset === 0 && now >= minutesOf(hours.opens)) continue;
    return instant(day, hours.opens).replace('.000Z', 'Z');
  }
  return undefined;
}

route('GET', '/queue/wait-estimate', (ctx) => {
  const branchId = ctx.query.get('branch');
  if (!branchId || !branchOf(branchId)) invalid('branch', 'ไม่พบสาขาที่เปิดให้บริการนี้');
  const today = clinicToday();
  const hours = hoursOn(today);
  const nowMinutes = clinicMinutes(new Date());
  const now = nowIso();
  const waiting = boardOf(branchId).filter((ticket) => AWAITING.includes(ticket.status)).length;
  const vets = new Set(shiftsOn(today, branchId).filter((shift) => shift.starts_at <= now && shift.ends_at > now && isVet(staffOf(shift.staff_id) ?? { id: '' })).map((shift) => shift.staff_id)).size;
  const open =
    !closedOn(today, branchId) && Boolean(hours.opens && hours.closes) && nowMinutes >= minutesOf(hours.opens!) && nowMinutes < minutesOf(hours.closes!) && vets > 0;
  if (!open) {
    const opensAt = nextOpening(branchId);
    return { branch_id: branchId, open_now: false, waiting, vets_on_duty: vets, ...(opensAt ? { opens_at: opensAt } : {}), updated_at: now };
  }
  const perVet = (waiting * CONSULTATION_MINUTES) / Math.max(1, vets);
  return {
    branch_id: branchId,
    open_now: true,
    waiting,
    vets_on_duty: vets,
    estimate_minutes_low: Math.max(0, Math.round((perVet * 0.8) / 5) * 5),
    estimate_minutes_high: Math.max(10, Math.round((perVet * 1.3 + CONSULTATION_MINUTES) / 5) * 5),
    updated_at: now,
  };
});
