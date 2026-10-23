import { findUserByIdentifier, staffOf as staffMembershipOf, customerOf as customerOfUser, startSession, tokenPayload, users } from '../../core/auth';
import { minutesFromNow, nowIso, uuid } from '../../core/db';
import { created, fail, invalid, page, Reply, requireAuth, requireCustomer, route, type Ctx } from '../../core/http';
import { checkOtp, digitsOf, issueOtp } from '../../core/otp';
import { createCustomerForUser, registerPet } from '../customers/store';
import { appointmentsOfCustomer, createAppointment } from './appointments';
import { availability, resolveSlot } from './availability';
import { branchOf, livingPetsOf, serviceOf } from './catalog';
import { recordMarketingConsent } from './pets';
import { once } from './replay';
import { appointmentDto, appointments, appointmentTypes, holdDto, holds, type HoldRow } from './store';
import { clinicToday } from './time';

/**
 * Booking from outside the counter: free times (public), holding a time for ten minutes, the
 * portal's own booking and list, and the guest booking whose code signs the visitor in
 * (docs/contract-extensions.md § 27).
 */

const HOLD_MINUTES = 10;

const isStaff = (ctx: Ctx) => Boolean(ctx.auth?.staffId);

route('GET', '/booking/availability', (ctx) => {
  const serviceId = ctx.query.get('service') ?? ctx.query.get('service_id');
  const branchId = ctx.query.get('branch') ?? ctx.query.get('branch_id');
  const date = ctx.query.get('date');
  if (!serviceId || !serviceOf(serviceId)) invalid('service', 'ไม่พบบริการนี้');
  if (!branchId || !branchOf(branchId)) invalid('branch', 'ไม่พบสาขานี้');
  if (!date || !/^\d{4}-\d{2}-\d{2}$/.test(date)) invalid('date', 'กรุณาระบุวันที่ในรูปแบบ YYYY-MM-DD');
  const free =
    date < clinicToday()
      ? { duration_minutes: Number(serviceOf(serviceId)?.duration_minutes) || 30, slots: [] }
      : // The counter books past the clinic's notice and the online switch, as POST /staff/appointments does.
        availability({ serviceId, branchId, date, online: !isStaff(ctx) });
  return { branch_id: branchId, service_id: serviceId, date, duration_minutes: free.duration_minutes, slots: free.slots };
});

function newHold(slot: { starts_at: string; ends_at: string; staff_id: string }, body: Record<string, string>, holder: string): HoldRow {
  return holds().insert({
    branch_id: body.branch_id!,
    service_id: body.service_id!,
    staff_id: slot.staff_id,
    starts_at: slot.starts_at,
    ends_at: slot.ends_at,
    expires_at: minutesFromNow(HOLD_MINUTES),
    holder,
    consumed: false,
  });
}

function slotFor(ctx: Ctx, body: Record<string, string>, online: boolean) {
  if (!body.service_id) invalid('service_id', 'กรุณาเลือกบริการ');
  if (!body.branch_id) invalid('branch_id', 'กรุณาเลือกสาขา');
  if (!body.starts_at || Number.isNaN(Date.parse(body.starts_at))) invalid('starts_at', 'กรุณาเลือกเวลา');
  return resolveSlot({ serviceId: body.service_id, branchId: body.branch_id, startsAt: body.starts_at, staffId: body.staff_id || undefined, online: online && !isStaff(ctx) });
}

route('POST', '/booking/holds', (ctx) => {
  const auth = requireAuth(ctx);
  const body = ctx.body ?? {};
  const slot = slotFor(ctx, body, true);
  return created(holdDto(newHold(slot, body, auth.customerId ?? auth.userId)));
});

/** A hold this customer may still turn into an appointment, or the refusal the form branches on. */
function liveHold(id: string | undefined, holder: string): HoldRow {
  const hold = id ? holds().get(id) : undefined;
  if (!hold) fail(404, 'not_found', 'ไม่พบการจองค้างนี้ กรุณาเลือกเวลาใหม่');
  if (hold.holder !== holder) fail(409, 'hold_not_yours', 'การจองค้างนี้เป็นของผู้ใช้อื่น');
  if (hold.consumed || hold.expires_at <= nowIso()) fail(409, 'hold_expired', 'การจองค้างหมดอายุแล้ว กรุณาเลือกเวลาใหม่');
  return hold;
}

// ---- The portal -------------------------------------------------------------------------------------

route('POST', '/portal/appointments', (ctx) => {
  const auth = requireCustomer(ctx);
  const body = ctx.body ?? {};
  const row = once(
    ctx,
    'portal-appointment',
    () => {
      const hold = liveHold(body.hold_id, auth.customerId);
      let petId: string | undefined = body.pet_id;
      if (petId) {
        if (!livingPetsOf(auth.customerId).some((pet) => pet.id === petId)) invalid('pet_id', 'ไม่พบสัตว์เลี้ยงนี้ในบัญชีของคุณ');
      } else if (body.new_pet?.name) {
        petId = registerPet(auth.customerId, { name: String(body.new_pet.name), species: String(body.new_pet.species ?? 'อื่น ๆ') }).id;
      } else {
        invalid('pet_id', 'กรุณาเลือกสัตว์เลี้ยง');
      }
      return createAppointment({
        petId,
        serviceId: hold.service_id,
        branchId: hold.branch_id,
        staffId: hold.staff_id,
        startsAt: hold.starts_at,
        holdId: hold.id,
        type: body.type || 'regular',
        reason: body.reason,
        source: 'online',
      });
    },
    (id) => appointments().get(id),
  );
  return created(appointmentDto(row));
});

route('GET', '/portal/appointments', (ctx) => {
  const auth = requireCustomer(ctx);
  const upcoming = ctx.query.get('upcoming');
  const rows = appointmentsOfCustomer(auth.customerId, { upcoming: upcoming === null ? undefined : upcoming === 'true' });
  return page(rows.map(appointmentDto), ctx.query, 10);
});

route('GET', '/portal/appointment-types', (ctx) => {
  requireAuth(ctx);
  return {
    data: appointmentTypes
      .get()
      .types.filter((type) => type.online_booking)
      .map((type) => ({ code: type.code, name: type.name, duration_minutes: type.duration_minutes ?? null })),
  };
});

// ---- Booking as a guest -----------------------------------------------------------------------------

const guestHolder = (phone: string) => `guest:${phone}`;

route('POST', '/booking/guest/start', (ctx) => {
  const body = ctx.body ?? {};
  const phone = digitsOf(String(body.phone ?? ''));
  if (!/^0\d{8,9}$/.test(phone)) invalid('phone', 'เบอร์โทรศัพท์ไม่ถูกต้อง');
  // Starting again lets go of the time this number held before.
  for (const old of holds().where((row) => row.holder === guestHolder(phone) && !row.consumed)) holds().remove(old.id);
  const slot = slotFor(ctx, body, true);
  const hold = newHold(slot, body, guestHolder(phone));
  const { expires_at } = issueOtp(phone, 'guest_booking', 'รหัสยืนยันการจองนัด');
  return created({ hold: holdDto(hold), code_expires_at: expires_at });
});

route('POST', '/booking/guest/verify', (ctx) => {
  const body = ctx.body ?? {};
  const phone = digitsOf(String(body.phone ?? ''));
  const hold = liveHold(body.hold_id, guestHolder(phone));
  if (!checkOtp(phone, String(body.code ?? ''), 'guest_booking')) invalid('code', 'รหัสยืนยันไม่ถูกต้องหรือหมดอายุ');
  if (!body.accept_privacy_notice) invalid('accept_privacy_notice', 'กรุณายอมรับประกาศความเป็นส่วนตัว');

  let user = findUserByIdentifier(phone);
  if (user && staffMembershipOf(user.id)) fail(409, 'contact_taken', 'เบอร์นี้เป็นของบัญชีพนักงาน กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้า');
  const accountCreated = !user;
  if (!user) {
    user = users().insert({
      display_name: String(body.display_name ?? '').trim() || 'ลูกค้าใหม่',
      email: null,
      phone,
      password: uuid(),
      status: 'active',
      two_factor: { enabled: false, method: 'none' },
      language: 'th',
      notifications: { email: true, sms: true, line: false },
      version: 1,
      created_at: nowIso(),
    });
  }
  const customer = customerOfUser(user.id) ?? createCustomerForUser(user);
  if (!customer) fail(500, 'internal_error', 'สร้างบัญชีลูกค้าไม่สำเร็จ');
  if (body.marketing_consent) recordMarketingConsent(customer.id);

  const claimed = holds().update(hold.id, { holder: customer.id, expires_at: minutesFromNow(HOLD_MINUTES) })!;
  const { body: tokens, setCookies } = tokenPayload(startSession(user.id));
  return new Reply(
    200,
    {
      ...tokens,
      account_created: accountCreated,
      pets: livingPetsOf(customer.id).map((pet) => ({ id: pet.id, name: pet.name, species: pet.species })),
      hold: holdDto(claimed),
    },
    {},
    setCookies,
  );
});
