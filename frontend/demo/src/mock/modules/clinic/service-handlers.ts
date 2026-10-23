import { today } from '../../core/db';
import { checkVersion, created, fail, invalid, notFound, requireStaff, route, type Ctx } from '../../core/http';
import {
  bookableServices,
  branches,
  publicServiceOf,
  SERVICE_CATEGORIES,
  services,
  staffMembers,
  WEEKDAYS,
  type PriceTier,
  type ServicePricing,
  type ServiceRow,
} from './data';
import { audit, isDate, isTime } from './util';

/**
 * The service menu: `GET /services` for the public site and booking (online-bookable only), and
 * `/staff/services*` for the catalogue as staff manage it, on `service:manage`.
 */

const categoryOrder = (code: string) => SERVICE_CATEGORIES.findIndex((category) => category.code === code);
/** Category order, then the order the clinic added them in (sort is stable). */
const byMenuOrder = (a: ServiceRow, b: ServiceRow) => categoryOrder(a.category_code) - categoryOrder(b.category_code);

route('GET', '/services', (ctx) => {
  const branch = ctx.query.get('branch') || undefined;
  if (branch && !branches().get(branch)) invalid('branch', 'ไม่พบสาขานี้');
  const rows = services().where((service) => bookableServices(branch).some((open) => open.id === service.id)).sort(byMenuOrder);
  return { data: rows.map(publicServiceOf) };
});

route('GET', '/services/categories', () => ({ data: SERVICE_CATEGORIES }));

route('GET', '/services/:id', (ctx) => {
  const service = services().get(ctx.params.id);
  if (!service || !service.is_active || !service.online_booking_enabled) notFound('บริการ');
  return publicServiceOf(service);
});

/** Applies price lists whose day has come, so `pricing` is always the one in force today. */
function current(service: ServiceRow): ServiceRow {
  const due = service.upcoming_pricing.filter((pricing) => (pricing.effective_from ?? '') <= today());
  if (!due.length) return service;
  const latest = due[due.length - 1]!;
  return services().update(service.id, {
    pricing: latest,
    price_satang: latest.base_price_satang,
    upcoming_pricing: service.upcoming_pricing.filter((pricing) => !due.includes(pricing)),
    version: service.version,
  })!;
}

const managed = (service: ServiceRow) => {
  const { description: _description, ...rest } = current(service);
  return rest;
};

route('GET', '/staff/services', (ctx) => {
  requireStaff(ctx, 'service:manage');
  return { data: services().all().sort(byMenuOrder).map(managed) };
});

route('GET', '/staff/services/:id', (ctx) => {
  requireStaff(ctx, 'service:manage');
  const service = services().get(ctx.params.id);
  if (!service) notFound('บริการ');
  return managed(service);
});

function name(value: unknown): string {
  const text = String(value ?? '').trim();
  if (!text) invalid('name', 'กรุณากรอกชื่อบริการ');
  if (text.length > 120) invalid('name', 'ชื่อบริการยาวเกิน 120 ตัวอักษร');
  return text;
}

function category(value: unknown): string {
  const code = String(value ?? '');
  if (categoryOrder(code) < 0) invalid('category_code', 'หมวดบริการไม่ถูกต้อง');
  return code;
}

function minutes(value: unknown): number {
  const number = Number(value);
  if (!Number.isInteger(number) || number < 5 || number > 600) invalid('duration_minutes', 'ระยะเวลาต้องอยู่ระหว่าง 5 ถึง 600 นาที');
  return number;
}

function satang(value: unknown, field: string): number {
  const number = Number(value);
  if (!Number.isInteger(number) || number < 0) invalid(field, 'ราคาไม่ถูกต้อง');
  return number;
}

route('POST', '/staff/services', (ctx) => {
  requireStaff(ctx, 'service:manage');
  const body = ctx.body ?? {};
  const serviceName = name(body.name);
  const categoryCode = category(body.category_code);
  const code = String(body.code ?? '').trim().toUpperCase() || `${categoryCode.slice(0, 3).toUpperCase()}-${String(services().count() + 1).padStart(3, '0')}`;
  if (!/^[A-Z0-9_-]{2,40}$/.test(code)) invalid('code', 'รหัสบริการใช้ได้เฉพาะ A-Z 0-9 - และ _');
  if (services().find((service) => service.code === code)) fail(409, 'service_code_taken', 'มีบริการที่ใช้รหัสนี้แล้ว', { field: 'code' });
  const price = satang(body.price_satang, 'price_satang');
  const service = services().insert({
    version: 1,
    code,
    name: serviceName,
    category_code: categoryCode,
    duration_minutes: minutes(body.duration_minutes),
    price_satang: price,
    online_booking_enabled: body.online_booking_enabled !== false,
    is_active: true,
    eligible_staff_ids: [],
    branch_ids: [],
    booking_windows: [],
    pricing: { base_price_satang: price, tiers: [] },
    upcoming_pricing: [],
  });
  audit(ctx, 'service.created', 'service', `เพิ่มบริการ ${service.name}`, service.id);
  return created(managed(service));
});

function editable(ctx: Ctx): ServiceRow {
  requireStaff(ctx, 'service:manage');
  const service = services().get(ctx.params.id);
  if (!service) notFound('บริการ');
  checkVersion(ctx, service);
  return service;
}

route('PATCH', '/staff/services/:id', (ctx) => {
  const service = editable(ctx);
  const body = ctx.body ?? {};
  const staffIds: string[] = Array.isArray(body.eligible_staff_ids) ? [...new Set(body.eligible_staff_ids.map(String))] as string[] : service.eligible_staff_ids;
  const unknown = staffIds.find((id) => staffMembers().get(id)?.status !== 'active');
  if (unknown) invalid('eligible_staff_ids', 'มีผู้ให้บริการที่ไม่พบหรือปิดบัญชีแล้ว');
  const saved = services().update(service.id, {
    name: 'name' in body ? name(body.name) : service.name,
    category_code: 'category_code' in body ? category(body.category_code) : service.category_code,
    duration_minutes: 'duration_minutes' in body ? minutes(body.duration_minutes) : service.duration_minutes,
    eligible_staff_ids: staffIds,
    is_active: typeof body.is_active === 'boolean' ? body.is_active : service.is_active,
  })!;
  audit(ctx, 'service.updated', 'service', `แก้บริการ ${saved.name}`, saved.id);
  return managed(saved);
});

function priceTiers(value: unknown): PriceTier[] {
  if (value === undefined) return [];
  if (!Array.isArray(value)) invalid('tiers', 'รูปแบบราคาไม่ถูกต้อง');
  return value.map((tier: Record<string, unknown>, index) => {
    const species = typeof tier.species === 'string' && tier.species.trim() ? tier.species.trim() : undefined;
    const min = tier.min_weight_kg === undefined || tier.min_weight_kg === null ? undefined : Number(tier.min_weight_kg);
    const max = tier.max_weight_kg === undefined || tier.max_weight_kg === null ? undefined : Number(tier.max_weight_kg);
    if (!species && min === undefined && max === undefined) invalid(`tiers[${index}]`, 'แต่ละราคาต้องระบุชนิดสัตว์หรือช่วงน้ำหนัก');
    if (min !== undefined && max !== undefined && min >= max) invalid(`tiers[${index}].max_weight_kg`, 'น้ำหนักสูงสุดต้องมากกว่าน้ำหนักต่ำสุด');
    return {
      ...(species ? { species } : {}),
      ...(min !== undefined ? { min_weight_kg: min } : {}),
      ...(max !== undefined ? { max_weight_kg: max } : {}),
      price_satang: satang(tier.price_satang, `tiers[${index}].price_satang`),
    };
  });
}

route('PATCH', '/staff/services/:id/pricing', (ctx) => {
  const service = current(editable(ctx));
  const body = ctx.body ?? {};
  const from = isDate(body.effective_from) ? body.effective_from : today();
  if (from < today()) invalid('effective_from', 'วันที่มีผลต้องเป็นวันนี้หรือหลังจากนี้');
  const pricing: ServicePricing = { effective_from: from, base_price_satang: satang(body.base_price_satang, 'base_price_satang'), tiers: priceTiers(body.tiers) };
  const upcoming = service.upcoming_pricing.filter((entry) => entry.effective_from !== from);
  const saved =
    from <= today()
      ? services().update(service.id, { pricing, price_satang: pricing.base_price_satang, upcoming_pricing: upcoming })!
      : services().update(service.id, { upcoming_pricing: [...upcoming, pricing].sort((a, b) => (a.effective_from ?? '').localeCompare(b.effective_from ?? '')) })!;
  audit(ctx, 'service.pricing_updated', 'service', `ตั้งราคาใหม่ของ${saved.name} มีผล ${from}`, saved.id);
  return managed(saved);
});

route('PATCH', '/staff/services/:id/availability', (ctx) => {
  const service = editable(ctx);
  const body = ctx.body ?? {};
  const branchIds: string[] = Array.isArray(body.branch_ids) ? [...new Set(body.branch_ids.map(String))] as string[] : service.branch_ids;
  if (branchIds.some((id) => !branches().get(id))) invalid('branch_ids', 'ไม่พบสาขาที่เลือก');
  const windows = Array.isArray(body.booking_windows) ? body.booking_windows : service.booking_windows;
  windows.forEach((window: Record<string, unknown>, index: number) => {
    if (!WEEKDAYS.includes(window.day as never)) invalid(`booking_windows[${index}].day`, 'วันไม่ถูกต้อง');
    if (!isTime(window.from) || !isTime(window.to) || window.from >= window.to) invalid(`booking_windows[${index}].to`, 'ช่วงเวลาไม่ถูกต้อง');
  });
  const saved = services().update(service.id, {
    online_booking_enabled: typeof body.online_booking_enabled === 'boolean' ? body.online_booking_enabled : service.online_booking_enabled,
    branch_ids: branchIds,
    booking_windows: windows.map((window: Record<string, string>) => ({ day: window.day, from: window.from, to: window.to })),
  })!;
  audit(ctx, 'service.availability_updated', 'service', `แก้การเปิดจองของ${saved.name}`, saved.id);
  return managed(saved);
});
