import { nowIso, table, today, type Row } from '../../core/db';
import { checkVersion, created, fail, file, invalid, noContent, notFound, requireStaff, route, type Ctx } from '../../core/http';
import { getFile, signedUpload } from '../../core/files';
import { branches, clinicProfile, clinicSettings, contacts, holidays, openingHours, WEEKDAYS, type ContactRow } from './data';
import { audit, compact, isDate, isTime, optionalText } from './util';

/**
 * `/clinic*`: the profile, branches, hours, holidays and contacts the public site reads, the
 * staff edits behind them on `settings:write`, and the operating rules under `/clinic/settings/*`.
 */

// ---- Profile and branding --------------------------------------------------------------------------

const profileAnswer = () => compact({ ...clinicProfile().get(), id: undefined } as Record<string, unknown>);

route('GET', '/clinic', () => profileAnswer());

const BRAND_TYPES = ['image/png', 'image/jpeg', 'image/webp'];
const BRAND_MAX_BYTES = 5 * 1024 * 1024;

/** The value an uploaded picture is saved as, and back: `storage:clinic-brand/logo-<file id>`. */
const brandValue = (kind: string, fileId: string) => `storage:clinic-brand/${kind}-${fileId}`;
export const brandFileId = (value: string | undefined, kind: string) =>
  value?.startsWith(`storage:clinic-brand/${kind}-`) ? value.slice(`storage:clinic-brand/${kind}-`.length) : undefined;

for (const kind of ['logo', 'tab-icon']) {
  route('POST', `/clinic/${kind}`, (ctx) => {
    requireStaff(ctx, 'settings:write');
    const type = String(ctx.body?.content_type ?? '');
    const size = Number(ctx.body?.size_bytes ?? 0);
    if (!BRAND_TYPES.includes(type)) invalid('content_type', 'รองรับเฉพาะไฟล์ PNG, JPEG หรือ WebP');
    if (!(size > 0) || size > BRAND_MAX_BYTES) invalid('size_bytes', 'ไฟล์ต้องไม่เกิน 5 MB');
    const upload = signedUpload({ name: `${kind}.${type.split('/')[1]}`, content_type: type, size });
    return {
      value: brandValue(kind, upload.id),
      upload: { url: upload.url, method: 'PUT', headers: upload.headers, expires_at: new Date(Date.now() + 15 * 60_000).toISOString() },
    };
  });

  /** The fixed public address of the picture; `?v=` names the file. `<img>` tags are pointed at the kept data URL instead (brand-images.ts). */
  route('GET', `/clinic/${kind}`, async (ctx) => {
    const stored = getFile(ctx.query.get('v')?.replace(`${kind}-`, ''));
    if (!stored?.data_url) notFound('รูป');
    return file(await (await fetch(stored.data_url)).blob(), stored.content_type, stored.name);
  });
}

route('PATCH', '/clinic', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const current = clinicProfile().get() as any;
  checkVersion(ctx, current);
  const body = ctx.body ?? {};

  const name = typeof body.name === 'string' ? body.name.trim() : current.name;
  if (!name) invalid('name', 'กรุณากรอกชื่อคลินิก');
  if (name.length > 120) invalid('name', 'ชื่อคลินิกยาวเกิน 120 ตัวอักษร');

  const patch: Record<string, unknown> = { name };
  for (const field of ['logo_url', 'brand_color', 'license_no', 'operator_name', 'tab_icon_url']) {
    if (field in body) patch[field] = optionalText(body[field]);
  }
  if (typeof body.show_name_with_logo === 'boolean') patch.show_name_with_logo = body.show_name_with_logo;

  const logo = patch.logo_url as string | undefined;
  if (logo && !/^https:\/\//i.test(logo) && !brandFileId(logo, 'logo')) invalid('logo_url', 'ลิงก์โลโก้ต้องขึ้นต้นด้วย https://');
  const icon = patch.tab_icon_url as string | undefined;
  if (icon && !brandFileId(icon, 'tab-icon')) invalid('tab_icon_url', 'ไอคอนแท็บต้องเป็นไฟล์ที่อัปโหลด');
  const colour = patch.brand_color as string | undefined;
  if (colour && !/^#[0-9a-f]{6}$/i.test(colour)) invalid('brand_color', 'สีต้องอยู่ในรูปแบบ #RRGGBB');

  clinicProfile().set(patch as never);
  audit(ctx, 'clinic.profile_updated', 'clinic', 'แก้ข้อมูลคลินิก');
  return profileAnswer();
});

// ---- Branches --------------------------------------------------------------------------------------

route('GET', '/clinic/branches', () => ({ data: branches().all().map((branch) => compact({ ...branch })) }));

route('GET', '/clinic/branches/:id', (ctx) => {
  const branch = branches().get(ctx.params.id);
  if (!branch) notFound('สาขา');
  return compact({ ...branch });
});

function examRooms(value: unknown): string[] {
  if (!Array.isArray(value)) invalid('exam_rooms', 'กรุณาระบุห้องตรวจ');
  const rooms = [...new Set(value.map((room) => String(room).trim()).filter(Boolean))];
  if (!rooms.length) invalid('exam_rooms', 'สาขาต้องมีห้องตรวจอย่างน้อยหนึ่งห้อง');
  return rooms;
}

function pin(body: Record<string, unknown>): { latitude?: number; longitude?: number } {
  const hasLat = typeof body.latitude === 'number';
  const hasLng = typeof body.longitude === 'number';
  if (hasLat !== hasLng) invalid('latitude', 'กรุณาระบุพิกัดทั้งละติจูดและลองจิจูด');
  if (!hasLat) return {};
  if (Math.abs(body.latitude as number) > 90) invalid('latitude', 'ละติจูดไม่ถูกต้อง');
  if (Math.abs(body.longitude as number) > 180) invalid('longitude', 'ลองจิจูดไม่ถูกต้อง');
  return { latitude: body.latitude as number, longitude: body.longitude as number };
}

route('POST', '/clinic/branches', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const body = ctx.body ?? {};
  const code = String(body.code ?? '').trim().toLowerCase();
  if (!/^[a-z0-9][a-z0-9_-]{1,39}$/.test(code)) invalid('code', 'รหัสสาขาใช้ได้เฉพาะ a-z 0-9 - และ _ (2-40 ตัว)');
  if (branches().get(`brn_${code}`)) fail(409, 'branch_code_taken', 'มีสาขาที่ใช้รหัสนี้แล้ว', { field: 'code' });
  const name = String(body.name ?? '').trim();
  if (!name) invalid('name', 'กรุณากรอกชื่อสาขา');
  const branch = branches().insert(
    compact({ id: `brn_${code}`, version: 1, code, name, address: optionalText(body.address), ...pin(body), exam_rooms: examRooms(body.exam_rooms) }),
  );
  audit(ctx, 'branch.created', 'branch', `เพิ่ม${name}`, branch.id);
  return created(branch);
});

route('PATCH', '/clinic/branches/:id', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const branch = branches().get(ctx.params.id);
  if (!branch) notFound('สาขา');
  checkVersion(ctx, branch);
  const body = ctx.body ?? {};
  const next: Record<string, unknown> = { ...branch };
  if ('name' in body) {
    const name = String(body.name ?? '').trim();
    if (!name) invalid('name', 'กรุณากรอกชื่อสาขา');
    next.name = name;
  }
  if ('address' in body) next.address = optionalText(body.address);
  if ('latitude' in body || 'longitude' in body) Object.assign(next, pin(body));
  if ('exam_rooms' in body) {
    const rooms = examRooms(body.exam_rooms);
    // A room a ticket on today's queue is still called to or in cannot be taken away. Tickets
    // carry the room's name as `room_id` (appointments/queue.ts).
    const busy = table('queue_tickets').find(
      (ticket) => ticket.branch_id === branch.id && ticket.room_id && !rooms.includes(ticket.room_id) && ['called', 'in_room'].includes(ticket.status),
    );
    if (busy) fail(409, 'room_in_use', `ห้อง ${busy.room_id} ยังมีคิวที่กำลังตรวจอยู่`, { field: 'exam_rooms' });
    next.exam_rooms = rooms;
  }
  next.version = branch.version + 1;
  const saved = branches().put(compact(next) as never);
  audit(ctx, 'branch.updated', 'branch', `แก้${saved.name}`, saved.id);
  return saved;
});

// ---- Hours and holidays ----------------------------------------------------------------------------

function hoursAnswer() {
  const hours = openingHours().get() as any;
  return {
    version: hours.version,
    configured: hours.configured,
    days: hours.days.map((entry: Row) => compact({ ...entry, closed: entry.closed })),
    holidays: holidays()
      .where((holiday) => holiday.date >= today())
      .sort((a, b) => a.date.localeCompare(b.date))
      .map((holiday) => compact({ ...holiday })),
  };
}

route('GET', '/clinic/hours', () => hoursAnswer());

route('PUT', '/clinic/hours', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const current = openingHours().get() as any;
  checkVersion(ctx, current);
  const days = ctx.body?.days;
  if (!Array.isArray(days) || days.length !== 7) invalid('days', 'ต้องระบุเวลาครบทั้ง 7 วัน');
  const seen = new Set<string>();
  const cleaned = days.map((entry: Row, index: number) => {
    if (!WEEKDAYS.includes(entry.day) || seen.has(entry.day)) invalid(`days[${index}].day`, 'ต้องระบุแต่ละวันเพียงครั้งเดียว');
    seen.add(entry.day);
    if (entry.closed) return { day: entry.day, closed: true };
    if (!isTime(entry.opens) || !isTime(entry.closes)) invalid(`days[${index}].opens`, 'เวลาต้องอยู่ในรูปแบบ HH:mm');
    if (entry.opens >= entry.closes) invalid(`days[${index}].closes`, 'เวลาปิดต้องหลังเวลาเปิด');
    return { day: entry.day, opens: entry.opens, closes: entry.closes, closed: false };
  });
  cleaned.sort((a, b) => WEEKDAYS.indexOf(a.day) - WEEKDAYS.indexOf(b.day));
  openingHours().set({ days: cleaned, configured: true } as never);
  audit(ctx, 'clinic.hours_replaced', 'clinic', 'แก้เวลาทำการ');
  return hoursAnswer();
});

route('POST', '/clinic/holidays', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const body = ctx.body ?? {};
  if (!isDate(body.date)) invalid('date', 'กรุณาเลือกวันที่');
  if (body.date < today()) invalid('date', 'วันหยุดต้องเป็นวันนี้หรือหลังจากนี้');
  const name = String(body.name ?? '').trim();
  if (!name) invalid('name', 'กรุณาตั้งชื่อวันหยุด');
  const branchId = optionalText(body.branch_id);
  if (branchId && !branches().get(branchId)) invalid('branch_id', 'ไม่พบสาขานี้');
  if (holidays().find((holiday) => holiday.date === body.date && (holiday.branch_id ?? '') === (branchId ?? ''))) {
    fail(409, 'holiday_exists', 'มีวันหยุดในวันนี้อยู่แล้ว', { field: 'date' });
  }
  const holiday = holidays().insert(compact({ date: body.date, name, branch_id: branchId }));
  audit(ctx, 'clinic.holiday_added', 'clinic', `เพิ่มวันหยุด ${name} (${body.date})`, holiday.id);
  return created(holiday);
});

// ---- Contacts --------------------------------------------------------------------------------------

const CONTACT_KINDS = ['phone', 'emergency', 'email', 'line', 'facebook', 'instagram', 'website', 'map', 'other'];

const sortedContacts = () => contacts().all().sort((a, b) => a.sort_order - b.sort_order || a.label.localeCompare(b.label));

route('GET', '/clinic/contacts', () => ({ data: sortedContacts().map((contact) => compact({ ...contact })) }));

function contactFields(body: Record<string, unknown>, current?: ContactRow): any {
  const kind = String(body.kind ?? current?.kind ?? '');
  if (!CONTACT_KINDS.includes(kind)) invalid('kind', 'ประเภทช่องทางไม่ถูกต้อง');
  const label = String(body.label ?? current?.label ?? '').trim();
  if (!label) invalid('label', 'กรุณาตั้งชื่อที่แสดง');
  const value = String(body.value ?? current?.value ?? '').trim();
  if (!value) invalid('value', 'กรุณากรอกข้อมูลติดต่อ');
  if ((kind === 'phone' || kind === 'emergency') && value.replace(/\D/g, '').length < 9) invalid('value', 'เบอร์โทรศัพท์ไม่ถูกต้อง');
  if (kind === 'email' && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(value)) invalid('value', 'อีเมลไม่ถูกต้อง');
  const pick = (field: 'url' | 'embed_url' | 'branch_id') => (field in body ? optionalText(body[field]) : current?.[field]);
  const url = pick('url');
  if (url && !/^https?:\/\//i.test(url)) invalid('url', 'ลิงก์ต้องขึ้นต้นด้วย https://');
  const embed = kind === 'map' ? pick('embed_url') : undefined;
  if (embed && !/^https:\/\//i.test(embed)) invalid('embed_url', 'ลิงก์แผนที่ต้องขึ้นต้นด้วย https://');
  const branchId = pick('branch_id');
  if (branchId && !branches().get(branchId)) invalid('branch_id', 'ไม่พบสาขานี้');
  const order = Number(body.sort_order ?? current?.sort_order ?? contacts().count() + 1);
  return compact({ kind, label, value, url, embed_url: embed, branch_id: branchId, sort_order: Number.isFinite(order) ? order : 0 }) as never;
}

route('POST', '/clinic/contacts', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const contact = contacts().insert({ version: 1, ...contactFields(ctx.body ?? {}) });
  audit(ctx, 'clinic.contact_added', 'clinic_contact', `เพิ่มช่องทางติดต่อ ${contact.label}`, contact.id);
  return created(contact);
});

route('PATCH', '/clinic/contacts/:id', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const contact = contacts().get(ctx.params.id);
  if (!contact) notFound('ช่องทางติดต่อ');
  checkVersion(ctx, contact);
  const saved = contacts().put({ id: contact.id, version: contact.version + 1, ...contactFields(ctx.body ?? {}, contact) });
  audit(ctx, 'clinic.contact_updated', 'clinic_contact', `แก้ช่องทางติดต่อ ${saved.label}`, saved.id);
  return saved;
});

route('DELETE', '/clinic/contacts/:id', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const contact = contacts().get(ctx.params.id);
  if (!contact) notFound('ช่องทางติดต่อ');
  checkVersion(ctx, contact);
  contacts().remove(contact.id);
  audit(ctx, 'clinic.contact_removed', 'clinic_contact', `ลบช่องทางติดต่อ ${contact.label}`, contact.id);
  return noContent();
});

// ---- Operating rules -------------------------------------------------------------------------------

const setting = (id: string): any => clinicSettings().get(id)!;
const answer = (row: Row) => {
  const { id: _id, ...rest } = row;
  return rest;
};

function intIn(body: Record<string, unknown>, field: string, min: number, max: number, current: number): number {
  if (!(field in body)) return current;
  const value = Number(body[field]);
  if (!Number.isInteger(value) || value < min || value > max) invalid(field, `ต้องเป็นจำนวนเต็มระหว่าง ${min} ถึง ${max}`);
  return value;
}

/** Saves a settings row when the body changes something; an empty `PATCH` only reads it. */
function saveSetting(ctx: Ctx, id: string, patch: Record<string, unknown>, label: string): Row {
  const current = setting(id);
  const changed = Object.keys(patch).some((key) => JSON.stringify(patch[key]) !== JSON.stringify(current[key]));
  if (!changed) return current;
  checkVersion(ctx, current);
  const saved = clinicSettings().update(id, { ...patch, ...('updated_at' in current ? { updated_at: nowIso() } : {}) })!;
  audit(ctx, `settings.${id}_updated`, 'settings', `แก้${label}`);
  return saved;
}

route('GET', '/clinic/settings/booking', (ctx) => {
  requireStaff(ctx, 'settings:read');
  return answer(setting('booking'));
});

route('PATCH', '/clinic/settings/booking', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const body = ctx.body ?? {};
  const current = setting('booking');
  const slot = intIn(body, 'slot_minutes', 5, 240, current.slot_minutes);
  if (slot % 5 !== 0) invalid('slot_minutes', 'ความยาวช่องนัดต้องเป็นทวีคูณของ 5 นาที');
  const patch = {
    slot_minutes: slot,
    lead_time_minutes: intIn(body, 'lead_time_minutes', 0, 10_080, current.lead_time_minutes),
    cancellation_cutoff_hours: intIn(body, 'cancellation_cutoff_hours', 0, 336, current.cancellation_cutoff_hours),
    max_advance_days: intIn(body, 'max_advance_days', 1, 365, current.max_advance_days),
    walk_in_slots_per_hour: intIn(body, 'walk_in_slots_per_hour', 0, 60, current.walk_in_slots_per_hour),
  };
  return answer(saveSetting(ctx, 'booking', patch, 'กติกาการนัดหมาย'));
});

const queueAnswer = (row: Row) => ({ ...answer(row), example: `${row.number_prefix}${String(14).padStart(row.number_digits, '0')}` });

route('PATCH', '/clinic/settings/queue', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const body = ctx.body ?? {};
  const current = setting('queue');
  const prefix = 'number_prefix' in body ? String(body.number_prefix ?? '').trim().toUpperCase() : current.number_prefix;
  if (!/^[A-Z]{0,3}$/.test(prefix)) invalid('number_prefix', 'คำนำหน้าใช้ได้เฉพาะตัวอักษร A-Z ไม่เกิน 3 ตัว');
  const patch = {
    number_prefix: prefix,
    number_digits: intIn(body, 'number_digits', 1, 6, current.number_digits),
    reset_daily: typeof body.reset_daily === 'boolean' ? body.reset_daily : current.reset_daily,
  };
  return queueAnswer(saveSetting(ctx, 'queue', patch, 'กติกาเลขคิว'));
});

const REQUIRED_FIELDS = ['chief_complaint', 'subjective', 'objective', 'assessment', 'plan', 'vitals', 'examination'];
const RECORD_SECTIONS = ['vitals', 'examination', 'diagnosis', 'treatment', 'prescription', 'lab_result', 'attachments'];

function listOf(body: Record<string, unknown>, field: string, allowed: string[] | null, current: string[]): string[] {
  if (!(field in body)) return current;
  const value = body[field];
  if (!Array.isArray(value)) invalid(field, 'รูปแบบไม่ถูกต้อง');
  const list = [...new Set(value.map((item) => String(item).trim()).filter(Boolean))];
  const unknown = allowed ? list.filter((item) => !allowed.includes(item)) : [];
  if (unknown.length) invalid(field, `ไม่รู้จัก: ${unknown.join(', ')}`);
  return allowed ? allowed.filter((item) => list.includes(item)) : list;
}

route('PATCH', '/clinic/settings/medical-record', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const body = ctx.body ?? {};
  const current = setting('medical_record');
  const systems = listOf(body, 'examination_systems', null, current.examination_systems);
  if (systems.length > 40) invalid('examination_systems', 'ระบบที่ตรวจได้ไม่เกิน 40 ระบบ');
  if (systems.some((system) => system.length > 60)) invalid('examination_systems', 'ชื่อระบบที่ตรวจยาวเกิน 60 ตัวอักษร');
  const patch = {
    required_fields: listOf(body, 'required_fields', REQUIRED_FIELDS, current.required_fields),
    examination_systems: systems,
    owner_visible_sections: listOf(body, 'owner_visible_sections', RECORD_SECTIONS, current.owner_visible_sections),
    signer_must_be_case_vet: typeof body.signer_must_be_case_vet === 'boolean' ? body.signer_must_be_case_vet : current.signer_must_be_case_vet,
  };
  return answer(saveSetting(ctx, 'medical_record', patch, 'กติกาเวชระเบียน'));
});

const loyaltyAnswer = (row: Row) => compact(answer(row));

route('GET', '/clinic/settings/loyalty', (ctx) => {
  requireStaff(ctx, 'settings:read');
  return loyaltyAnswer(setting('loyalty'));
});

route('PUT', '/clinic/settings/loyalty', (ctx) => {
  requireStaff(ctx, 'settings:write');
  const body = ctx.body ?? {};
  const current = setting('loyalty');
  checkVersion(ctx, current);
  const silver = intIn(body, 'silver_min_points', 1, 10_000_000, current.silver_min_points);
  const gold = intIn(body, 'gold_min_points', 1, 10_000_000, current.gold_min_points);
  if (gold <= silver) invalid('gold_min_points', 'แต้มขั้นทองต้องมากกว่าขั้นเงิน');
  const expiry = body.points_expire_after_months === null ? undefined : intIn(body, 'points_expire_after_months', 1, 120, current.points_expire_after_months ?? 24);
  const saved = clinicSettings().put({
    ...current,
    satang_per_point: intIn(body, 'satang_per_point', 1, 10_000_000, current.satang_per_point),
    point_value_satang: intIn(body, 'point_value_satang', 1, 10_000_000, current.point_value_satang),
    silver_min_points: silver,
    gold_min_points: gold,
    points_expire_after_months: expiry,
    referral_enabled: typeof body.referral_enabled === 'boolean' ? body.referral_enabled : current.referral_enabled,
    referral_referrer_points: intIn(body, 'referral_referrer_points', 0, 100_000, current.referral_referrer_points),
    referral_friend_points: intIn(body, 'referral_friend_points', 0, 100_000, current.referral_friend_points),
    referral_monthly_cap: intIn(body, 'referral_monthly_cap', 0, 1000, current.referral_monthly_cap),
    version: current.version + 1,
    updated_at: nowIso(),
  });
  audit(ctx, 'settings.loyalty_updated', 'settings', 'แก้กติกาแต้มสะสม');
  return loyaltyAnswer(saved);
});
