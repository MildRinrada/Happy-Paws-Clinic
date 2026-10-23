import { findUserByIdentifier, sessions, users } from '../../core/auth';
import { nowIso, today, uuid } from '../../core/db';
import { DEMO_TOTP } from '../../core/demo-accounts';
import { fileUrl, signedUpload } from '../../core/files';
import { allows, checkVersion, created, fail, invalid, notFound, page, requireStaff, route, stateConflict, type Ctx } from '../../core/http';
import { send } from '../../core/outbox';
import {
  auditLogs,
  branches,
  DEPARTMENTS,
  departmentForStaffType,
  invitations,
  roles,
  SCOPE_CATALOGUE,
  staffMembers,
  SUGGESTED_STAFF_TYPES,
  type InvitationRow,
  type PublicProfile,
  type RoleRow,
  type StaffRow,
} from './data';
import { portraitUrl } from './portraits';
import { audit, compact } from './util';

/**
 * People: the staff list and its invitations, roles and the scope catalogue, the public team page,
 * and accepting an invitation. Reads are `staff:read` / `role:read`; changes `staff:manage` /
 * `role:manage`, and nobody may hand out a scope they do not hold.
 */

const INVITATION_DAYS = 7;
const POSITION_NAMES: Record<string, string> = {
  veterinarian: 'สัตวแพทย์',
  vet_assistant: 'ผู้ช่วยสัตวแพทย์',
  receptionist: 'พนักงานต้อนรับ',
  cashier: 'แคชเชียร์',
  owner: 'เจ้าของคลินิก',
  clinic_admin: 'ผู้ดูแลคลินิก',
};
const departmentName = (code: string) => DEPARTMENTS.find((department) => department.department === code)?.name ?? code;

// ---- Answers ---------------------------------------------------------------------------------------

/** The file id behind `storage:staff-photos/<id>`, the value an uploaded photo is saved as. */
const photoFileId = (path?: string) => (path?.startsWith('storage:staff-photos/') ? path.slice('storage:staff-photos/'.length) : undefined);

function profileAnswer(profile?: PublicProfile) {
  if (!profile) return undefined;
  const fileId = photoFileId(profile.photo_path);
  return compact({ ...profile, ...(fileId ? { photo_url: fileUrl(fileId) ?? undefined } : {}) });
}

export function memberAnswer(row: StaffRow | InvitationRow) {
  const { token: _token, totp_secret: _secret, accepted_at: _accepted, ...rest } = row as InvitationRow;
  const user = row.user_id ? users().get(row.user_id) : undefined;
  return compact({
    ...rest,
    two_factor_enabled: user ? user.two_factor.enabled : false,
    public_profile: profileAnswer(row.public_profile),
  });
}

// ---- Guards ----------------------------------------------------------------------------------------

/** Whether the caller's branch reach covers a member: an unbound caller sees everyone. */
function reaches(callerBranches: string[], member: { branches: string[] }): boolean {
  if (!callerBranches.length || !member.branches.length) return true;
  return member.branches.some((branch) => callerBranches.includes(branch));
}

const scopesOf = (roleNames: string[]) => roles().where((role) => roleNames.includes(role.name)).flatMap((role) => role.scopes);

/** True when holding `scopes` lets someone grant `scope` (a wildcard needs its whole area, `*` needs `*`). */
function covers(scopes: string[], scope: string): boolean {
  if (scopes.includes('*')) return true;
  if (scope === '*') return false;
  if (scope.endsWith(':*')) return scopes.includes(scope);
  return allows(scopes, scope);
}

function roleNames(ctx: Ctx, value: unknown, field = 'roles'): string[] {
  if (!Array.isArray(value)) invalid(field, 'กรุณาเลือกบทบาท');
  const names = [...new Set(value.map(String))];
  for (const name of names) {
    const role = roles().find((row) => row.name === name);
    if (!role || !role.assignable) invalid(field, `ไม่พบบทบาท ${name}`);
    const missing = role.scopes.find((scope) => !covers(ctx.auth!.scopes, scope));
    if (missing) fail(403, 'insufficient_scope', `ให้บทบาท ${name} ไม่ได้ เพราะบัญชีนี้ไม่มีสิทธิ์ ${missing}`, { field });
  }
  return names;
}

function branchList(ctx: Ctx, value: unknown): string[] {
  const list = Array.isArray(value) ? [...new Set(value.map(String))] : [];
  if (list.some((id) => !branches().get(id))) invalid('branches', 'ไม่พบสาขาที่เลือก');
  const own = ctx.auth!.branches;
  if (own.length) {
    if (!list.length) invalid('branches', 'กรุณาเลือกสาขาอย่างน้อยหนึ่งสาขา');
    if (list.some((id) => !own.includes(id))) fail(403, 'insufficient_scope', 'เลือกได้เฉพาะสาขาที่บัญชีนี้ดูแล', { field: 'branches' });
  }
  return list;
}

/** Members who could still run the clinic with `*`, other than `exceptId`. */
const otherOwners = (exceptId: string) =>
  staffMembers().where((member) => member.id !== exceptId && member.status === 'active' && scopesOf(member.roles).includes('*')).length;

// ---- Staff list ------------------------------------------------------------------------------------

route('GET', '/staff/members', (ctx) => {
  const auth = requireStaff(ctx, 'staff:read');
  const status = ctx.query.get('status') || '';
  if (status && !['active', 'deactivated', 'invited'].includes(status)) invalid('status', 'สถานะต้องเป็น active, deactivated หรือ invited');
  const department = ctx.query.get('department') || '';
  if (department && !DEPARTMENTS.some((entry) => entry.department === department)) invalid('department', 'แผนกไม่ถูกต้อง');
  const branch = ctx.query.get('branch') || '';
  if (branch && auth.branches.length && !auth.branches.includes(branch)) fail(403, 'insufficient_scope', 'บัญชีนี้ดูสาขานี้ไม่ได้');
  const role = ctx.query.get('role') || '';
  const staffType = ctx.query.get('staff_type') || '';

  const source: Array<StaffRow | InvitationRow> =
    status === 'invited'
      ? invitations().where((invite) => !invite.accepted_at && invite.invitation_expires_at > nowIso())
      : staffMembers().where((member) => (status ? member.status === status : true));

  const rows = source
    .filter((member) => reaches(auth.branches, member))
    .filter((member) => !branch || !member.branches.length || member.branches.includes(branch))
    .filter((member) => !department || member.department === department)
    .filter((member) => !role || member.roles.includes(role))
    .filter((member) => !staffType || member.staff_type === staffType)
    .sort((a, b) => a.display_name.localeCompare(b.display_name, 'th'));

  return page(rows.map(memberAnswer), ctx.query);
});

route('GET', '/staff/members/:id', (ctx) => {
  const auth = requireStaff(ctx, 'staff:read');
  const member = staffMembers().get(ctx.params.id) ?? invitations().get(ctx.params.id);
  if (!member || !reaches(auth.branches, member)) notFound('พนักงาน');
  return memberAnswer(member);
});

route('GET', '/staff/staff-types', (ctx) => {
  requireStaff(ctx, 'staff:read');
  const counts = new Map<string, number>(SUGGESTED_STAFF_TYPES.map((type) => [type, 0]));
  for (const member of staffMembers().where((row) => row.status === 'active')) {
    counts.set(member.staff_type, (counts.get(member.staff_type) ?? 0) + 1);
  }
  return {
    data: [...counts.entries()]
      .map(([staff_type, members]) => ({ staff_type, members }))
      .sort((a, b) => b.members - a.members || a.staff_type.localeCompare(b.staff_type)),
  };
});

route('GET', '/staff/departments', (ctx) => {
  requireStaff(ctx, 'staff:read');
  const active = staffMembers().where((row) => row.status === 'active');
  return { data: DEPARTMENTS.map((entry) => ({ ...entry, members: active.filter((member) => member.department === entry.department).length })) };
});

// ---- Invitations -----------------------------------------------------------------------------------

function invitationLink(invite: InvitationRow): string {
  return `${location.origin}/invitations/${encodeURIComponent(invite.id)}#token=${encodeURIComponent(invite.token)}`;
}

route('POST', '/staff/members/invitations', (ctx) => {
  requireStaff(ctx, 'staff:manage');
  const body = ctx.body ?? {};
  const email = String(body.email ?? '').trim().toLowerCase();
  if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) invalid('email', 'อีเมลไม่ถูกต้อง');
  const displayName = String(body.display_name ?? '').trim();
  if (!displayName) invalid('display_name', 'กรุณากรอกชื่อที่แสดง');
  const staffType = String(body.staff_type ?? '').trim();
  if (!staffType) invalid('staff_type', 'กรุณาระบุตำแหน่ง');
  const assigned = roleNames(ctx, body.roles);
  const branchIds = branchList(ctx, body.branches);
  const license = typeof body.license_no === 'string' && body.license_no.trim() ? body.license_no.trim() : undefined;

  if (staffMembers().find((member) => member.email?.toLowerCase() === email && member.status === 'active')) {
    fail(409, 'already_staff', 'อีเมลนี้เป็นพนักงานของคลินิกอยู่แล้ว', { field: 'email' });
  }
  // Inviting an address again re-issues the invitation and kills the old link.
  invitations().removeWhere((invite) => invite.email?.toLowerCase() === email && !invite.accepted_at);

  const invite = invitations().insert({
    id: `sin_${uuid()}`,
    version: 1,
    display_name: displayName,
    email,
    staff_type: staffType,
    ...(license ? { license_no: license } : {}),
    roles: assigned,
    branches: branchIds,
    status: 'invited',
    two_factor_enabled: false,
    created_at: nowIso(),
    invitation_expires_at: new Date(Date.now() + INVITATION_DAYS * 86_400_000).toISOString(),
    department: departmentForStaffType(staffType),
    token: uuid().replace(/-/g, ''),
  });

  send({
    channel: 'email',
    to: email,
    subject: 'คำเชิญเข้าร่วมทีม Happy Paws Clinic',
    body: `สวัสดี ${displayName}\n\nคุณได้รับเชิญให้เข้าใช้ระบบของ Happy Paws Clinic ในตำแหน่ง${POSITION_NAMES[staffType] ?? ` ${staffType}`}\nตั้งรหัสผ่านและยืนยันตัวตนสองขั้นตอนได้ที่ลิงก์นี้ภายใน ${INVITATION_DAYS} วัน:\n${invitationLink(invite)}`,
  });
  audit(ctx, 'staff.invited', 'staff_invitation', `เชิญ ${displayName} (${email})`, invite.id);
  return created(memberAnswer(invite));
});

route('POST', '/staff/members/invitations/:id/accept', (ctx) => {
  const body = ctx.body ?? {};
  const invite = invitations().get(ctx.params.id);
  if (!invite || invite.accepted_at || invite.token !== body.token || invite.invitation_expires_at <= nowIso()) {
    notFound('คำเชิญ หรือลิงก์หมดอายุแล้ว');
  }

  const existing = findUserByIdentifier(invite.email!);
  if (existing && staffMembers().find((member) => member.user_id === existing.id && member.status === 'active')) {
    fail(409, 'superseded', 'อีเมลนี้มีบัญชีพนักงานอยู่แล้ว');
  }

  if (body.password === undefined && body.code === undefined) {
    const secret = invite.totp_secret ?? Array.from({ length: 16 }, () => 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'[Math.floor(Math.random() * 32)]).join('');
    invitations().update(invite.id, { totp_secret: secret });
    const label = encodeURIComponent(`Happy Paws Clinic:${invite.email}`);
    return {
      status: 'two_factor_setup_required',
      email: invite.email,
      two_factor: { secret, otp_auth_uri: `otpauth://totp/${label}?secret=${secret}&issuer=${encodeURIComponent('Happy Paws Clinic')}` },
    };
  }

  const password = String(body.password ?? '');
  if (password.length < 8) invalid('password', 'รหัสผ่านต้องยาวอย่างน้อย 8 ตัวอักษร');
  if (!invite.totp_secret) invalid('code', 'กรุณาตั้งค่าแอปยืนยันตัวตนก่อน');
  if (String(body.code ?? '').trim() !== DEMO_TOTP) invalid('code', `รหัสไม่ถูกต้อง (ในเดโมใช้ ${DEMO_TOTP})`);

  const user =
    existing ??
    users().insert({
      display_name: invite.display_name,
      email: invite.email,
      phone: null,
      password,
      status: 'active',
      two_factor: { enabled: true, method: 'totp', recovery_codes_left: 10 },
      language: 'th',
      notifications: { email: true, sms: false, line: false },
      version: 1,
      created_at: nowIso(),
    });
  if (existing) users().update(existing.id, { password, two_factor: { enabled: true, method: 'totp', recovery_codes_left: 10 }, status: 'active' });

  const member = staffMembers().insert({
    version: 1,
    user_id: user.id,
    display_name: invite.display_name,
    email: invite.email,
    staff_type: invite.staff_type,
    ...(invite.license_no ? { license_no: invite.license_no } : {}),
    roles: invite.roles,
    branches: invite.branches,
    status: 'active',
    two_factor_enabled: true,
    created_at: nowIso(),
    department: invite.department,
    public_profile: { show_on_website: false },
  });
  invitations().update(invite.id, { accepted_at: nowIso() });
  auditLogs().insert({ occurred_at: nowIso(), actor_id: member.id, actor_name: member.display_name, action: 'staff.invitation_accepted', resource_type: 'staff_member', resource_id: member.id, summary: `${member.display_name} รับคำเชิญ`, ip: '203.150.12.60' });
  return { status: 'accepted', email: invite.email, user_id: user.id, staff_id: member.id };
});

// ---- Changing a member -----------------------------------------------------------------------------

function managedMember(ctx: Ctx): StaffRow {
  const auth = requireStaff(ctx, 'staff:manage');
  const member = staffMembers().get(ctx.params.id);
  if (!member || !reaches(auth.branches, member)) notFound('พนักงาน');
  // A manager may not change someone who holds more than they do.
  const beyond = scopesOf(member.roles).find((scope) => !covers(auth.scopes, scope));
  if (beyond) fail(403, 'insufficient_scope', 'แก้ข้อมูลของผู้ที่มีสิทธิ์มากกว่าบัญชีนี้ไม่ได้');
  return member;
}

route('PATCH', '/staff/members/:id', (ctx) => {
  const member = managedMember(ctx);
  checkVersion(ctx, member);
  const body = ctx.body ?? {};
  const patch: Partial<StaffRow> = {};
  if ('staff_type' in body) {
    const staffType = String(body.staff_type ?? '').trim();
    if (!staffType) invalid('staff_type', 'กรุณาระบุตำแหน่ง');
    patch.staff_type = staffType;
  }
  if ('roles' in body) {
    patch.roles = roleNames(ctx, body.roles);
    if (scopesOf(member.roles).includes('*') && !scopesOf(patch.roles).includes('*') && !otherOwners(member.id)) {
      stateConflict('ต้องมีอย่างน้อยหนึ่งคนที่ถือสิทธิ์เต็ม (*) ไว้เสมอ');
    }
  }
  if ('branches' in body) patch.branches = branchList(ctx, body.branches);
  if ('license_no' in body) patch.license_no = typeof body.license_no === 'string' && body.license_no.trim() ? body.license_no.trim() : undefined;
  if ('department' in body) {
    if (!DEPARTMENTS.some((entry) => entry.department === body.department)) invalid('department', 'แผนกไม่ถูกต้อง');
    patch.department = body.department;
  }
  const saved = staffMembers().put(compact({ ...member, ...patch, version: member.version + 1 }) as StaffRow);
  audit(ctx, 'staff.updated', 'staff_member', `แก้ข้อมูลของ ${saved.display_name}`, saved.id);
  return memberAnswer(saved);
});

route('PUT', '/staff/members/:id/public-profile', (ctx) => {
  const member = managedMember(ctx);
  checkVersion(ctx, member);
  const body = ctx.body ?? {};
  const text = (field: string, max: number) => {
    const value = typeof body[field] === 'string' ? body[field].trim() : '';
    if (value.length > max) invalid(field, `ยาวเกิน ${max} ตัวอักษร`);
    return value || undefined;
  };
  const year = body.career_start_year === undefined || body.career_start_year === null ? undefined : Number(body.career_start_year);
  if (year !== undefined && (!Number.isInteger(year) || year < 1950 || year > Number(today().slice(0, 4)))) invalid('career_start_year', 'ปีที่เริ่มทำงานไม่ถูกต้อง');
  const photo = text('photo_path', 300);
  if (photo && !photo.startsWith('/') && !photoFileId(photo)) invalid('photo_path', 'รูปต้องเป็นไฟล์ที่อัปโหลด หรือที่อยู่ที่ขึ้นต้นด้วย /');
  const profile: PublicProfile = compact({
    job_title: text('job_title', 120),
    specialty: text('specialty', 120),
    bio: text('bio', 1000),
    photo_path: photo,
    career_start_year: year,
    show_on_website: body.show_on_website === true,
  });
  const saved = staffMembers().update(member.id, { public_profile: profile })!;
  audit(ctx, 'staff.public_profile_replaced', 'staff_member', `แก้โปรไฟล์บนเว็บไซต์ของ ${saved.display_name}`, saved.id);
  return memberAnswer(saved);
});

route('POST', '/staff/members/:id/public-profile/photo', (ctx) => {
  managedMember(ctx);
  const type = String(ctx.body?.content_type ?? '');
  const size = Number(ctx.body?.size_bytes ?? 0);
  if (!['image/jpeg', 'image/png', 'image/webp'].includes(type)) invalid('content_type', 'รองรับเฉพาะไฟล์ JPEG, PNG หรือ WebP');
  if (!(size > 0) || size > 5 * 1024 * 1024) invalid('size_bytes', 'ไฟล์ต้องไม่เกิน 5 MB');
  const upload = signedUpload({ name: `photo.${type.split('/')[1]}`, content_type: type, size });
  return {
    photo_path: `storage:staff-photos/${upload.id}`,
    upload: { url: upload.url, method: 'PUT', headers: upload.headers, expires_at: new Date(Date.now() + 15 * 60_000).toISOString() },
  };
});

route('POST', '/staff/members/:id/deactivate', (ctx) => {
  const member = managedMember(ctx);
  if (member.id === ctx.auth!.staffId) stateConflict('ปิดบัญชีของตัวเองไม่ได้');
  if (member.status !== 'active') stateConflict('บัญชีนี้ถูกปิดไปแล้ว');
  if (scopesOf(member.roles).includes('*') && !otherOwners(member.id)) stateConflict('ต้องมีอย่างน้อยหนึ่งคนที่ถือสิทธิ์เต็ม (*) ไว้เสมอ');
  const saved = staffMembers().update(member.id, { status: 'deactivated' })!;
  for (const session of sessions().where((row) => row.user_id === member.user_id && !row.revoked)) sessions().update(session.id, { revoked: true });
  audit(ctx, 'staff.deactivated', 'staff_member', `ปิดบัญชีของ ${saved.display_name}`, saved.id);
  return memberAnswer(saved);
});

// ---- Roles -----------------------------------------------------------------------------------------

const roleAnswer = (role: RoleRow) => {
  const { id, version, name, is_system, scopes, assignable } = role;
  return { id, version, name, is_system, scopes, assignable };
};

const AREAS = new Set(SCOPE_CATALOGUE.map((scope) => scope.split(':')[0]));
const grantable = (scope: string) => scope === '*' || SCOPE_CATALOGUE.includes(scope) || (scope.endsWith(':*') && AREAS.has(scope.slice(0, -2)));

function scopeSet(ctx: Ctx, value: unknown): string[] {
  if (!Array.isArray(value) || !value.length) invalid('scopes', 'กรุณาเลือกสิทธิ์อย่างน้อยหนึ่งรายการ');
  const scopes = [...new Set(value.map(String))];
  const unknown = scopes.filter((scope) => !grantable(scope));
  if (unknown.length) fail(422, 'validation_failed', `ไม่รู้จักสิทธิ์: ${unknown.join(', ')}`, { field: 'scopes', details: { unknown_scopes: unknown } });
  const beyond = scopes.find((scope) => !covers(ctx.auth!.scopes, scope));
  if (beyond) fail(403, 'insufficient_scope', `ให้สิทธิ์ ${beyond} ไม่ได้ เพราะบัญชีนี้ไม่มีสิทธิ์นั้น`, { field: 'scopes' });
  return scopes;
}

route('GET', '/staff/roles', (ctx) => {
  requireStaff(ctx, 'role:read');
  return { data: roles().all().map(roleAnswer) };
});

route('GET', '/staff/permissions', (ctx) => {
  requireStaff(ctx, 'role:read');
  return { data: SCOPE_CATALOGUE.map((scope) => ({ scope, resource: scope.split(':')[0], action: scope.split(':')[1] })) };
});

route('POST', '/staff/roles', (ctx) => {
  requireStaff(ctx, 'role:manage');
  const name = String(ctx.body?.name ?? '').trim();
  if (!/^[a-z][a-z0-9_]{1,39}$/.test(name)) invalid('name', 'ชื่อบทบาทใช้ได้เฉพาะ a-z 0-9 และ _ ขึ้นต้นด้วยตัวอักษร');
  if (roles().find((role) => role.name === name)) fail(409, 'role_exists', 'มีบทบาทชื่อนี้แล้ว', { field: 'name' });
  const role = roles().insert({ id: `rol_${uuid().slice(0, 8)}`, version: 1, name, is_system: false, scopes: scopeSet(ctx, ctx.body?.scopes), assignable: true });
  audit(ctx, 'role.created', 'role', `เพิ่มบทบาท ${name}`, role.id);
  return created(roleAnswer(role));
});

route('PUT', '/staff/roles/:id/permissions', (ctx) => {
  requireStaff(ctx, 'role:manage');
  const role = roles().get(ctx.params.id);
  if (!role) notFound('บทบาท');
  if (!role.assignable) stateConflict('บทบาทนี้แก้ไขไม่ได้');
  checkVersion(ctx, role);
  const scopes = scopeSet(ctx, ctx.body?.scopes);
  if (role.scopes.includes('*') && !scopes.includes('*')) {
    const others = staffMembers().all().some(
      (member) => member.status === 'active' && member.roles.some((name) => name !== role.name && roles().find((row) => row.name === name)?.scopes.includes('*')),
    );
    if (!others) stateConflict('ต้องมีอย่างน้อยหนึ่งคนที่ถือสิทธิ์เต็ม (*) ไว้เสมอ');
  }
  const saved = roles().update(role.id, { scopes })!;
  audit(ctx, 'role.permissions_replaced', 'role', `แก้สิทธิ์ของบทบาท ${saved.name}`, saved.id);
  return roleAnswer(saved);
});

// ---- Public team page ------------------------------------------------------------------------------

route('GET', '/team', (ctx) => {
  const department = (ctx.query.get('department') ?? '').trim().toLowerCase();
  if (department && !DEPARTMENTS.some((entry) => entry.department === department)) invalid('department', 'แผนกไม่ถูกต้อง');
  const year = Number(today().slice(0, 4));
  const listed = staffMembers().where(
    (member) =>
      member.status === 'active' &&
      member.public_profile?.show_on_website === true &&
      users().get(member.user_id)?.status === 'active' &&
      (!department || member.department === department),
  );
  const data = DEPARTMENTS.filter((entry) => listed.some((member) => member.department === entry.department)).map((entry) => ({
    department: entry.department,
    name: departmentName(entry.department),
    members: listed
      .filter((member) => member.department === entry.department)
      .sort((a, b) => (a.public_profile?.career_start_year ?? 9999) - (b.public_profile?.career_start_year ?? 9999) || a.display_name.localeCompare(b.display_name, 'th'))
      .map((member) => {
        const profile = member.public_profile!;
        const fileId = photoFileId(profile.photo_path);
        return compact({
          id: member.id,
          display_name: member.display_name,
          job_title: profile.job_title,
          specialty: profile.specialty,
          bio: profile.bio,
          photo_path: profile.photo_path?.startsWith('/') ? profile.photo_path : undefined,
          years_of_experience: profile.career_start_year ? Math.max(0, year - profile.career_start_year) : undefined,
          // Sample staff have no photo; a drawn portrait stands in, never a real person's face.
          photo_url: fileId ? (fileUrl(fileId) ?? undefined) : profile.photo_path?.startsWith('/') ? undefined : portraitUrl(member),
        });
      }),
  }));
  return { data };
});

// ---- Accounts and the audit trail ------------------------------------------------------------------

/** `GET /staff/users`: the sign-in accounts behind the staff list, for `user:manage`. No page calls it yet. */
route('GET', '/staff/users', (ctx) => {
  requireStaff(ctx, 'user:manage');
  const staffUserIds = new Set(staffMembers().all().map((member) => member.user_id));
  const rows = users()
    .where((user) => staffUserIds.has(user.id))
    .map((user) => ({ id: user.id, display_name: user.display_name, email: user.email ?? undefined, status: user.status, two_factor_enabled: user.two_factor.enabled, created_at: user.created_at }));
  return page(rows, ctx.query);
});

route('GET', '/staff/audit-logs', (ctx) => {
  requireStaff(ctx, 'audit:read');
  const action = ctx.query.get('action');
  const actor = ctx.query.get('actor_id');
  const from = ctx.query.get('from');
  const to = ctx.query.get('to');
  const rows = auditLogs()
    .where((row) => (!action || row.action.startsWith(action)) && (!actor || row.actor_id === actor) && (!from || row.occurred_at >= from) && (!to || row.occurred_at <= `${to}T23:59:59`))
    .sort((a, b) => b.occurred_at.localeCompare(a.occurred_at));
  return page(rows, ctx.query, 50);
});
