import { singleton, table, type Row } from '../../core/db';

/**
 * The clinic module's tables and the fixed lists the backend declares in code. Other modules read
 * these through the helpers at the bottom (`workingVetsOn`, `openingHoursOn`, `bookableServices`);
 * only this module writes them.
 */

// ---- Row shapes ------------------------------------------------------------------------------------

/** `GET /clinic/branches`'s `Branch`. */
export interface BranchRow extends Row {
  version: number;
  code: string;
  name: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  exam_rooms: string[];
}

export interface HolidayRow extends Row {
  date: string;
  name: string;
  branch_id?: string;
}

export interface ContactRow extends Row {
  version: number;
  kind: string;
  label: string;
  value: string;
  url?: string;
  embed_url?: string;
  branch_id?: string;
  sort_order: number;
}

export interface PriceTier {
  species?: string;
  min_weight_kg?: number;
  max_weight_kg?: number;
  price_satang: number;
}

export interface ServicePricing {
  effective_from?: string;
  base_price_satang: number;
  tiers: PriceTier[];
}

/** `GET /staff/services`'s `ManagedService`, plus the public card's `description`. */
export interface ServiceRow extends Row {
  version: number;
  code: string;
  name: string;
  category_code: string;
  duration_minutes: number;
  price_satang: number;
  online_booking_enabled: boolean;
  is_active: boolean;
  eligible_staff_ids: string[];
  branch_ids: string[];
  booking_windows: Array<{ day: string; from: string; to: string }>;
  pricing: ServicePricing;
  upcoming_pricing: ServicePricing[];
  description?: string;
}

export interface RoleRow extends Row {
  version: number;
  name: string;
  is_system: boolean;
  scopes: string[];
  assignable: boolean;
}

export interface PublicProfile {
  job_title?: string;
  specialty?: string;
  bio?: string;
  photo_path?: string;
  career_start_year?: number;
  show_on_website: boolean;
}

/** `GET /staff/members`'s `StaffMember` (two_factor_enabled is read from the account at answer time). */
export interface StaffRow extends Row {
  version: number;
  user_id?: string;
  display_name: string;
  email?: string;
  staff_type: string;
  license_no?: string;
  roles: string[];
  branches: string[];
  status: 'active' | 'deactivated' | 'invited';
  two_factor_enabled: boolean;
  created_at: string;
  department: string;
  public_profile?: PublicProfile;
}

/** An open invitation, in the staff list's shape, plus the link's secret. */
export interface InvitationRow extends StaffRow {
  status: 'invited';
  invitation_expires_at: string;
  token: string;
  /** Set once the invitee has asked for the authenticator secret. */
  totp_secret?: string;
  accepted_at?: string;
}

/** One working block, `Shift` of `GET /staff/schedules`. Instants carry the clinic's +07:00. */
export interface ShiftRow extends Row {
  staff_id: string;
  branch_id: string;
  starts_at: string;
  ends_at: string;
  version: number;
}

export interface TimeOffRow extends Row {
  staff_id: string;
  starts_at: string;
  ends_at: string;
  reason?: string;
}

export interface AuditRow extends Row {
  occurred_at: string;
  actor_id?: string;
  actor_name: string;
  action: string;
  resource_type: string;
  resource_id?: string;
  summary: string;
  ip?: string;
}

export const branches = () => table<BranchRow>('branches');
export const holidays = () => table<HolidayRow>('holidays');
export const contacts = () => table<ContactRow>('clinic_contacts');
export const services = () => table<ServiceRow>('services');
export const roles = () => table<RoleRow>('roles');
export const staffMembers = () => table<StaffRow>('staff_members');
export const invitations = () => table<InvitationRow>('staff_invitations');
export const shifts = () => table<ShiftRow>('shifts');
export const timeOff = () => table<TimeOffRow>('time_off');
export const auditLogs = () => table<AuditRow>('audit_logs');

/** The profile `GET /clinic` answers, and the week `GET /clinic/hours` answers (holidays apart). */
export const clinicProfile = () =>
  singleton('clinic_profile', () => ({
    version: 1,
    name: 'Happy Paws Clinic',
    brand_color: '#0f766e',
    license_no: 'สพ.กทม. 1024/2558',
    operator_name: 'สพ.ญ. ใจดี รักสัตว์',
    show_name_with_logo: true,
  }));

export const openingHours = () =>
  singleton('clinic_hours', () => ({
    version: 1,
    configured: true,
    days: WEEKDAYS.map((weekday) => ({ day: weekday, opens: '09:00', closes: '20:00', closed: false })),
  }));

export const clinicSettings = () => table('clinic_settings');

// ---- Fixed lists -----------------------------------------------------------------------------------

/** `ServiceCategories.All`, in the contract's order. */
export const SERVICE_CATEGORIES = [
  { code: 'checkup', name: 'ตรวจ', name_en: 'Check-up' },
  { code: 'vaccine', name: 'วัคซีน', name_en: 'Vaccination' },
  { code: 'dental', name: 'ทันตกรรม', name_en: 'Dental' },
  { code: 'surgery', name: 'ผ่าตัด', name_en: 'Surgery' },
  { code: 'lab', name: 'แล็บ', name_en: 'Laboratory' },
  { code: 'grooming', name: 'กรูมมิ่ง', name_en: 'Grooming' },
];

/** `Departments.All`, in the team page's order. */
export const DEPARTMENTS = [
  { department: 'veterinary', name: 'ทีมสัตวแพทย์' },
  { department: 'nursing', name: 'ผู้ช่วยสัตวแพทย์' },
  { department: 'front_desk', name: 'ต้อนรับและนัดหมาย' },
  { department: 'finance', name: 'การเงิน' },
  { department: 'management', name: 'บริหารคลินิก' },
  { department: 'support', name: 'ทีมสนับสนุน' },
];

export const departmentForStaffType = (staffType?: string): string =>
  ({ veterinarian: 'veterinary', vet_assistant: 'nursing', receptionist: 'front_desk', cashier: 'finance', owner: 'management', clinic_admin: 'management' })[
    staffType?.trim() ?? ''
  ] ?? 'support';

/** The four positions `GET /staff/staff-types` always suggests. */
export const SUGGESTED_STAFF_TYPES = ['veterinarian', 'vet_assistant', 'receptionist', 'cashier'];

/** `ScopeCatalogue.All`: every constant of Scopes.cs except `*` and `portal`, in ordinal order. */
export const SCOPE_CATALOGUE = [
  'ai:use', 'analytics:hr', 'analytics:read', 'appointment:checkin', 'appointment:read', 'appointment:write', 'audit:read',
  'automation:manage', 'automation:read', 'billing:payout', 'consult:read', 'consult:write', 'content:manage', 'crm:manage',
  'crm:read', 'customer:merge', 'customer:read', 'customer:write', 'dashboard:read', 'integration:manage', 'inventory:adjust',
  'inventory:purchase', 'inventory:read', 'inventory:write', 'invoice:collect', 'invoice:discount', 'invoice:write', 'lab:read',
  'lab:request', 'lab:write', 'message:read', 'message:write', 'notify:broadcast', 'notify:manage', 'notify:read', 'notify:send',
  'patient:read', 'patient:write', 'payment:collect', 'payment:refund', 'queue:call', 'queue:read', 'queue:write', 'receipt:issue',
  'record:amend', 'record:audit', 'record:read', 'record:sign', 'record:write', 'report:export', 'role:manage', 'role:read',
  'rx:dispense', 'rx:read', 'rx:write', 'schedule:read', 'schedule:write', 'service:manage', 'settings:danger', 'settings:read',
  'settings:write', 'staff:manage', 'staff:read', 'stats:read', 'user:manage', 'vitals:write',
].sort();

/** `DefaultRoles.StarterScopes`, row for row; `clinic_admin` expanded against the catalogue. */
export const STARTER_ROLES: Array<{ name: string; scopes: string[] }> = [
  { name: 'clinic_owner', scopes: ['*'] },
  { name: 'clinic_admin', scopes: SCOPE_CATALOGUE.filter((scope) => !['billing:payout', 'settings:danger', 'ai:use'].includes(scope)) },
  // The contract's vet row grants no reads of patients, owners or records, so a real clinic adds
  // them in role settings before a vet can work; the demo's vet starts with them already added.
  {
    name: 'veterinarian',
    scopes: [
      'consult:*', 'record:write', 'lab:*', 'rx:write', 'queue:call', 'ai:use', 'dashboard:read',
      'patient:read', 'customer:read', 'record:read', 'record:sign', 'rx:read', 'vitals:write',
      'appointment:read', 'queue:read', 'schedule:read',
    ],
  },
  // The contract's counter row has no `patient:*`, yet booking finds the patient through
  // `GET /staff/pets` and the counter registers new pets; the demo's counter starts with both.
  { name: 'receptionist', scopes: ['appointment:*', 'queue:*', 'customer:*', 'patient:read', 'patient:write', 'invoice:collect', 'ai:use', 'dashboard:read'] },
  { name: 'vet_assistant', scopes: ['queue:read', 'patient:read', 'vitals:write'] },
  { name: 'cashier', scopes: ['invoice:*', 'payment:*', 'receipt:issue'] },
  { name: 'customer', scopes: ['portal'] },
];

export const WEEKDAYS = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday'] as const;
export type Weekday = (typeof WEEKDAYS)[number];

// ---- Clinic time -----------------------------------------------------------------------------------

/** Bangkok has no daylight saving: the clinic's wall clock is UTC+7 everywhere the demo is opened. */
const OFFSET_MS = 7 * 3_600_000;

/** `2026-10-23` + `09:00` as an instant in clinic time, `2026-10-23T09:00:00+07:00`. */
export const clinicInstant = (date: string, time: string) => `${date}T${time.length === 5 ? `${time}:00` : time}+07:00`;

/** The clinic day an instant falls on. */
export const clinicDateOf = (iso: string) => new Date(Date.parse(iso) + OFFSET_MS).toISOString().slice(0, 10);

/** The clinic wall-clock time of an instant, `HH:mm`. */
export const clinicTimeOf = (iso: string) => new Date(Date.parse(iso) + OFFSET_MS).toISOString().slice(11, 16);

/** The weekday of a clinic date, as the hours list names it. */
export function weekdayOf(date: string): Weekday {
  const index = new Date(`${date}T00:00:00Z`).getUTCDay(); // 0 = Sunday
  return WEEKDAYS[(index + 6) % 7]!;
}

export function addDays(date: string, days: number): string {
  const value = new Date(`${date}T00:00:00Z`);
  value.setUTCDate(value.getUTCDate() + days);
  return value.toISOString().slice(0, 10);
}

/** The Monday of the week a clinic date falls in. */
export const mondayOf = (date: string) => addDays(date, -WEEKDAYS.indexOf(weekdayOf(date)));

export const overlaps = (aStart: string, aEnd: string, bStart: string, bEnd: string) =>
  Date.parse(aStart) < Date.parse(bEnd) && Date.parse(bStart) < Date.parse(aEnd);

// ---- Helpers for other modules ---------------------------------------------------------------------

/**
 * The clinic's hours on a date at a branch, or `null` when it is closed (a closed weekday, or a
 * holiday for the whole clinic or that branch). Times are clinic wall-clock `HH:mm`.
 */
export function openingHoursOn(date: string, branchId?: string): { opens: string; closes: string } | null {
  const hours = table('clinic_hours').get('singleton') as { days?: Array<{ day: string; opens?: string; closes?: string; closed: boolean }> } | undefined;
  const entry = hours?.days?.find((day) => day.day === weekdayOf(date));
  if (!entry || entry.closed || !entry.opens || !entry.closes) return null;
  const closed = holidays().find((holiday) => holiday.date === date && (!holiday.branch_id || holiday.branch_id === branchId));
  return closed ? null : { opens: entry.opens, closes: entry.closes };
}

export interface WorkingVet {
  staff_id: string;
  display_name: string;
  /** The person's working blocks that day at that branch, earliest first. */
  shifts: ShiftRow[];
}

/**
 * The vets rostered at a branch on a clinic date (`YYYY-MM-DD`), with their working blocks. Leave
 * is already taken out: recording time off removes the blocks it covers, as the backend does. A
 * holiday closes the branch, so nobody is working then. This is what booking availability should
 * offer slots from: a slot is free when one of these blocks covers it and that vet has no
 * appointment then.
 */
export function workingVetsOn(date: string, branchId: string): WorkingVet[] {
  if (!openingHoursOn(date, branchId)) return [];
  const vets = staffMembers().where((member) => member.status === 'active' && member.staff_type === 'veterinarian');
  return vets
    .map((vet) => ({
      staff_id: vet.id,
      display_name: vet.display_name,
      shifts: shifts()
        .where((shift) => shift.staff_id === vet.id && shift.branch_id === branchId && clinicDateOf(shift.starts_at) === date)
        .sort((a, b) => Date.parse(a.starts_at) - Date.parse(b.starts_at)),
    }))
    .filter((vet) => vet.shifts.length > 0);
}

/** The vets whose block covers an instant at a branch (for "who could take this appointment"). */
export function vetsOnShiftAt(instant: string, branchId: string, endsAt = instant): string[] {
  return workingVetsOn(clinicDateOf(instant), branchId)
    .filter((vet) => vet.shifts.some((shift) => Date.parse(shift.starts_at) <= Date.parse(instant) && Date.parse(endsAt) <= Date.parse(shift.ends_at)))
    .map((vet) => vet.staff_id);
}

/** Services open for online booking at a branch, in the public `PublicService` shape. */
export function bookableServices(branchId?: string) {
  return services()
    .where((service) => service.is_active && service.online_booking_enabled && (!branchId || !service.branch_ids.length || service.branch_ids.includes(branchId)))
    .map(publicServiceOf);
}

export function publicServiceOf(service: ServiceRow) {
  return {
    id: service.id,
    code: service.code,
    name: service.name,
    category_code: service.category_code,
    duration_minutes: service.duration_minutes,
    price_satang: service.price_satang,
    price_tiers: service.pricing.tiers,
    branch_ids: service.branch_ids,
    ...(service.description ? { description: service.description } : {}),
  };
}

/** A staff member's display name, for other modules' answers that print one. */
export const staffName = (staffId?: string | null) => (staffId ? staffMembers().get(staffId)?.display_name : undefined);
