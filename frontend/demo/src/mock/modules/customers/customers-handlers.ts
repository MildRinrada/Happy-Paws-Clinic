import type { CustomerDetail, MergeResult, PortalInvitation } from '@/features/customers/types/customer.types';
import { minutesFromNow, nowIso, table } from '../../core/db';
import { checkVersion, created, fail, invalid, noContent, notFound, page, requireStaff, route, stateConflict, type Ctx } from '../../core/http';
import { send } from '../../core/outbox';
import {
  carers,
  consents,
  customerDto,
  customerLine,
  customers,
  dataRequests,
  invitations,
  matchesSearch,
  pets,
  petsOf,
  petSummary,
  referralCodes,
  runMergeListeners,
  type CustomerRow,
  type InvitationRow,
} from './store';

/** The customer register at the counter: `/staff/customers*`. */

const digits = (value: unknown) => String(value ?? '').replace(/\D/g, '').replace(/^66(?=\d{9}$)/, '0');
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const INVITE_DAYS = 7;

function customerOr404(id: string): CustomerRow {
  const row = customers().get(id);
  if (!row || row.deleted_at) notFound('ลูกค้า');
  return row;
}

/** Whether a customer matches the counter's one box: name, phone, email, a pet's name or HN. */
function matchesQuery(row: CustomerRow, query: string, petIndex: Map<string, string>): boolean {
  return matchesSearch(query, [row.display_name, row.email, petIndex.get(row.id)], [row.phone]);
}

route('GET', '/staff/customers', (ctx) => {
  requireStaff(ctx, 'customer:read');
  const query = (ctx.query.get('q') ?? '').trim();
  const petIndex = new Map<string, string>();
  for (const pet of pets().all()) {
    const owners = [pet.primary_customer_id, ...carers().where((link) => link.pet_id === pet.id && !link.revoked_at).map((link) => link.customer_id)];
    for (const owner of owners) petIndex.set(owner, `${petIndex.get(owner) ?? ''}|${pet.name}|${pet.hospital_number}|${pet.microchip_number ?? ''}`);
  }
  const rows = customers()
    .where((row) => !row.merged_into_customer_id && !row.deleted_at && matchesQuery(row, query, petIndex))
    .sort((a, b) => b.created_at.localeCompare(a.created_at));
  const answer = page(rows, ctx.query, 20);
  return { data: answer.data.map(customerDto), page: answer.page };
});

route('POST', '/staff/customers', (ctx) => {
  requireStaff(ctx, 'customer:write');
  const body = ctx.body ?? {};
  const name = String(body.display_name ?? '').trim();
  const phone = digits(body.phone);
  const email = String(body.email ?? '').trim();
  const errors: Record<string, string[]> = {};
  if (!name) errors.display_name = ['กรุณากรอกชื่อลูกค้า'];
  else if (name.length > 200) errors.display_name = ['ชื่อยาวได้ไม่เกิน 200 ตัวอักษร'];
  if (!phone) errors.phone = ['กรุณากรอกเบอร์โทรศัพท์'];
  else if (!/^0\d{8,9}$/.test(phone)) errors.phone = ['เบอร์โทรศัพท์ไม่ถูกต้อง ใช้รูปแบบ 0812345678'];
  if (email && !EMAIL.test(email)) errors.email = ['อีเมลไม่ถูกต้อง'];
  if (Object.keys(errors).length) fail(422, 'validation_failed', 'ข้อมูลที่ส่งมาไม่ถูกต้อง', { details: { errors } });

  const existing = customers().where((row) => !row.merged_into_customer_id && !row.deleted_at && digits(row.phone) === phone);
  if (existing.length && !body.allow_duplicate_phone) {
    fail(409, 'duplicate_customer', 'เบอร์นี้มีลูกค้าอยู่ในระบบแล้ว', {
      field: 'phone',
      details: { existing: existing.map(customerDto) },
    });
  }
  const row = customers().insert({
    version: 1,
    display_name: name,
    phone,
    ...(email ? { email } : {}),
    has_portal_account: false,
    is_active: true,
    created_at: nowIso(),
  } as CustomerRow);
  return created(customerDto(row));
});

function invitationDto(row: InvitationRow): PortalInvitation {
  return {
    id: row.id,
    sent_to: row.sent_to,
    invited_by_staff_id: row.invited_by_staff_id,
    sent_at: row.sent_at,
    expires_at: row.expires_at,
    ...(row.accepted_at ? { accepted_at: row.accepted_at } : {}),
    is_live: !row.accepted_at && row.expires_at > nowIso(),
  };
}

function latestInvitation(customerId: string): InvitationRow | undefined {
  return invitations()
    .where((row) => row.customer_id === customerId)
    .sort((a, b) => b.sent_at.localeCompare(a.sent_at))[0];
}

/** What the customer owes: the open balance of their invoices, read from the billing module's table. */
function outstandingSatang(customerId: string): number {
  return table('invoices')
    .where((invoice) => invoice.customer_id === customerId && (invoice.status === 'open' || invoice.status === 'partially_paid'))
    .reduce((sum, invoice) => sum + (Number(invoice.balance_satang) || 0), 0);
}

export function customerDetail(row: CustomerRow): CustomerDetail {
  const invitation = latestInvitation(row.id);
  return {
    customer: customerDto(row),
    pets: petsOf(row.id).map(petSummary),
    outstanding_satang: outstandingSatang(row.id),
    ...(invitation ? { invitation: invitationDto(invitation) } : {}),
    line: customerLine(row),
  };
}

route('GET', '/staff/customers/:id', (ctx) => {
  requireStaff(ctx, 'customer:read');
  return customerDetail(customerOr404(ctx.params.id!));
});

/** Not called by the product's pages today; kept for scripts and other modules (name, phone, email). */
route('PATCH', '/staff/customers/:id', (ctx) => {
  requireStaff(ctx, 'customer:write');
  const row = customerOr404(ctx.params.id!);
  checkVersion(ctx, row);
  if (row.merged_into_customer_id) stateConflict('เรกคอร์ดนี้ถูกรวมเข้ากับลูกค้าอื่นแล้ว');
  const body = ctx.body ?? {};
  const patch: Partial<CustomerRow> = {};
  if (body.display_name !== undefined) {
    const name = String(body.display_name).trim();
    if (!name) invalid('display_name', 'กรุณากรอกชื่อลูกค้า');
    patch.display_name = name;
  }
  if (body.phone !== undefined) {
    const phone = digits(body.phone);
    if (!/^0\d{8,9}$/.test(phone)) invalid('phone', 'เบอร์โทรศัพท์ไม่ถูกต้อง');
    patch.phone = phone;
  }
  if (body.email !== undefined) {
    const email = String(body.email ?? '').trim();
    if (email && !EMAIL.test(email)) invalid('email', 'อีเมลไม่ถูกต้อง');
    patch.email = email || undefined;
  }
  if (typeof body.is_active === 'boolean') patch.is_active = body.is_active;
  return customerDto(customers().update(row.id, patch)!);
});

route('POST', '/staff/customers/:id/invite', (ctx) => {
  const auth = requireStaff(ctx, 'customer:write');
  const row = customerOr404(ctx.params.id!);
  if (row.merged_into_customer_id) stateConflict('เรกคอร์ดนี้ถูกรวมเข้ากับลูกค้าอื่นแล้ว');
  if (row.has_portal_account) stateConflict('ลูกค้ารายนี้มีบัญชีออนไลน์อยู่แล้ว');
  const live = latestInvitation(row.id);
  const fields = { sent_to: row.phone, invited_by_staff_id: auth.staffId, sent_at: nowIso(), expires_at: minutesFromNow(INVITE_DAYS * 24 * 60) };
  // Sending again refreshes the live invitation rather than stacking a second one.
  const invitation = live && !live.accepted_at && live.expires_at > nowIso() ? invitations().update(live.id, fields)! : invitations().insert({ customer_id: row.id, ...fields });
  send({
    channel: 'sms',
    to: row.phone,
    body: `Happy Paws: คุณ${row.display_name.replace(/^คุณ/, '')} ติดตามคิว นัดหมาย และประวัติวัคซีนของสัตว์เลี้ยงได้ทางออนไลน์ สมัครด้วยเบอร์นี้ที่ ${location.origin}/register (ลิงก์ใช้ได้ ${INVITE_DAYS} วัน)`,
  });
  if (row.email) {
    send({
      channel: 'email',
      to: row.email,
      subject: 'เชิญใช้งานบัญชีออนไลน์ของ Happy Paws',
      body: `สวัสดีคุณ${row.display_name.replace(/^คุณ/, '')}\n\nคลินิกเชิญให้คุณเปิดบัญชีออนไลน์ เพื่อดูนัดหมาย คิว และประวัติของสัตว์เลี้ยง สมัครด้วยเบอร์ ${row.phone} ที่ ${location.origin}/register`,
    });
  }
  return invitationDto(invitation);
});

route('POST', '/staff/customers/:id/merge', (ctx) => {
  requireStaff(ctx, 'customer:merge');
  const survivor = customerOr404(ctx.params.id!);
  const duplicateId = String(ctx.body?.duplicate_customer_id ?? '');
  if (!duplicateId) invalid('duplicate_customer_id', 'ต้องระบุเรกคอร์ดที่จะรวม');
  if (duplicateId === survivor.id) invalid('duplicate_customer_id', 'ไม่สามารถรวมเรกคอร์ดเข้ากับตัวเองได้');
  const duplicate = customerOr404(duplicateId);
  checkVersion(ctx, survivor);
  if (survivor.merged_into_customer_id || duplicate.merged_into_customer_id) stateConflict('เรกคอร์ดใดเรกคอร์ดหนึ่งถูกรวมไปแล้ว');
  if (survivor.has_portal_account && duplicate.has_portal_account) stateConflict('ทั้งสองเรกคอร์ดมีบัญชีออนไลน์ รวมกันไม่ได้');

  const moved: Record<string, number> = {};
  const count = (key: string, value: number) => {
    if (value) moved[key] = (moved[key] ?? 0) + value;
  };

  for (const pet of pets().where((row) => row.primary_customer_id === duplicate.id)) {
    pets().update(pet.id, { primary_customer_id: survivor.id });
    count('pets', 1);
  }
  for (const link of carers().where((row) => row.customer_id === duplicate.id)) {
    // A pet both records care for keeps one link: the survivor's.
    const twin = carers().find((row) => row.pet_id === link.pet_id && row.customer_id === survivor.id && !row.revoked_at);
    if (twin) {
      carers().update(twin.id, { is_primary: twin.is_primary || link.is_primary, may_view_history: twin.may_view_history || link.may_view_history });
      carers().remove(link.id);
    } else carers().update(link.id, { customer_id: survivor.id });
    count('carer_links_moved', 1);
  }
  for (const consent of consents().where((row) => row.customer_id === duplicate.id)) {
    consents().update(consent.id, { customer_id: survivor.id });
    count('consents', 1);
  }
  for (const request of dataRequests().where((row) => row.customer_id === duplicate.id)) {
    dataRequests().update(request.id, { customer_id: survivor.id });
    count('data_requests', 1);
  }
  for (const code of referralCodes().where((row) => row.customer_id === duplicate.id)) {
    if (referralCodes().find((row) => row.customer_id === survivor.id)) referralCodes().remove(code.id);
    else referralCodes().update(code.id, { customer_id: survivor.id });
  }
  for (const [key, value] of Object.entries(runMergeListeners(survivor.id, duplicate.id))) count(key, value);

  const now = nowIso();
  const kept = customers().update(survivor.id, {
    ...(!survivor.email && duplicate.email ? { email: duplicate.email } : {}),
    ...(!survivor.line && duplicate.line ? { line: duplicate.line } : {}),
    ...(duplicate.has_portal_account ? { has_portal_account: true, user_id: duplicate.user_id } : {}),
  })!;
  const merged = customers().update(duplicate.id, {
    merged_into_customer_id: survivor.id,
    merged_at: now,
    is_active: false,
    has_portal_account: false,
    user_id: undefined,
    line: undefined,
  })!;
  const answer: MergeResult = { surviving: customerDto(kept), merged: customerDto(merged), moved };
  return answer;
});

// ---- LINE address (docs/contract-extensions.md § 18) ---------------------------------------------

function lineChange(ctx: Ctx): CustomerRow {
  requireStaff(ctx, 'customer:write');
  const row = customerOr404(ctx.params.id!);
  if (ctx.ifMatch === undefined) fail(428, 'precondition_required', 'ต้องส่ง If-Match');
  checkVersion(ctx, row);
  if (row.merged_into_customer_id) stateConflict('เรกคอร์ดนี้ถูกรวมเข้ากับลูกค้าอื่นแล้ว');
  return row;
}

route('PUT', '/staff/customers/:id/line', (ctx) => {
  const row = lineChange(ctx);
  const lineUserId = String(ctx.body?.line_user_id ?? '').trim();
  if (!lineUserId) invalid('line_user_id', 'ต้องเลือกผู้ติดตาม LINE');
  // The follower list belongs to the LINE integration (crm module); its row gives the name to show.
  const follower = table('line_followers').find((entry) => entry.line_user_id === lineUserId || entry.id === lineUserId);
  const taken = customers().find((other) => other.id !== row.id && other.line?.line_user_id === lineUserId && !other.merged_into_customer_id);
  if (taken) stateConflict(`บัญชี LINE นี้ผูกกับคุณ${taken.display_name.replace(/^คุณ/, '')} อยู่แล้ว`);
  const next = customers().update(row.id, {
    line: {
      line_user_id: lineUserId,
      ...(follower?.display_name ? { display_name: follower.display_name } : {}),
      ...(follower?.picture_url ? { picture_url: follower.picture_url } : {}),
      linked_at: nowIso(),
      ...(follower?.unfollowed_at ? { unfollowed_at: follower.unfollowed_at } : {}),
    },
  })!;
  return customerLine(next);
});

route('DELETE', '/staff/customers/:id/line', (ctx) => {
  const row = lineChange(ctx);
  customers().update(row.id, { line: undefined });
  return noContent();
});
