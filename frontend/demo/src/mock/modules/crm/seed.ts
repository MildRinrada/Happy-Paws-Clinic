import { DEMO_ACCOUNTS } from '../../core/demo-accounts';
import { at, day, table, uuid, type Row } from '../../core/db';
import { createInvoice, takePayment } from '../billing/invoices';
import { countSegment, normaliseCriteria } from './insights';
import { addPoints, earnPointsForInvoice, refreshPackage } from './loyalty';
import { lineSettings, wordsOf } from './notify';
import { markDueAsSent } from './reminders';
import {
  activeCustomers,
  apiKeys,
  automations,
  calendarLinks,
  coupons,
  deliveries,
  hasMarketingConsent,
  homeBranchOf,
  inbox,
  lineFollowers,
  maskEmail,
  maskLine,
  maskPhone,
  packages,
  petsOf,
  rng,
  segments,
  services,
  stableId,
  staffByUser,
  threadMessages,
  threads,
  webhookDeliveries,
  webhooks,
} from './store';
import { TEMPLATES } from './templates';

/**
 * The CRM's sample data, planted after customers, appointments, clinical and billing (order 50):
 * a connected LINE account and its followers, loyalty points and packages, coupons, segments,
 * conversations with customers, a fortnight of sent messages, the reminder rules, two webhooks with
 * their delivery history and an API key. Other modules' rows are looked up, never assumed.
 */
export const order = 50;

const random = rng(`crm-${day(0)}`);
const pick = <T>(list: T[]): T => list[Math.floor(random() * list.length)]!;

export function seed() {
  const customers = activeCustomers();
  if (customers.length === 0) {
    console.warn('[mock] crm seed: no customers yet, the CRM pages start empty');
    return;
  }
  const staff = {
    owner: staffByUser(DEMO_ACCOUNTS.owner.user_id)?.id ?? 'staff-owner',
    vet: staffByUser(DEMO_ACCOUNTS.vet.user_id)?.id ?? 'staff-vet',
    reception: staffByUser(DEMO_ACCOUNTS.reception.user_id)?.id ?? 'staff-reception',
  };
  const demo = customers.find((c) => c.user_id === DEMO_ACCOUNTS.customer.user_id);
  // The demo customer first, then those with animals, then the rest: the order conversations and packages are dealt in.
  const ordered = [
    ...(demo ? [demo] : []),
    ...customers.filter((c) => c !== demo && petsOf(c.id).length > 0),
    ...customers.filter((c) => c !== demo && petsOf(c.id).length === 0),
  ];

  seedLine(customers);
  seedProviders(staff.owner);
  seedLoyalty(ordered, staff);
  seedPackages(ordered, staff);
  seedCoupons(staff);
  seedSegments(staff);
  seedThreads(ordered, staff);
  seedMessages(ordered, staff);
  seedAutomations(staff);
  markDueAsSent();
  seedWebhooks(staff.owner);
  seedApiKeys(staff.owner);
}

// ---- LINE and providers -----------------------------------------------------------------------------

const UNLINKED_FOLLOWERS = ['ต้นข้าว', 'Pim P.', 'แม่น้องถั่วงอก', 'Kwan Kwan', 'ป้าแดง', 'Arm Sitthichai', 'โอ๊ต & ลาเต้'];

function seedLine(customers: Row[]) {
  lineSettings.set({
    version: 4,
    channel_id: '1657283940',
    channel_secret: 'demo-line-channel-secret',
    channel_access_token: 'demo-line-access-token',
    enabled: true,
    notification_types: Object.fromEntries(TEMPLATES.map((t) => [t.key, t.key !== 'customer.portal_invitation'])),
    broadcast_monthly_limit: 500,
    updated_at: at(-38, '16:20'),
  });

  for (const c of customers.filter((x) => x.line?.line_user_id)) {
    lineFollowers().insert({
      line_user_id: c.line.line_user_id,
      display_name: c.line.display_name ?? c.display_name,
      followed_at: new Date(Date.parse(c.line.linked_at ?? at(-200, '10:00')) - 3 * 86_400_000).toISOString(),
      last_seen_at: at(-Math.floor(random() * 20), '19:30'),
      ...(c.line.unfollowed_at ? { unfollowed_at: c.line.unfollowed_at } : {}),
    });
  }
  UNLINKED_FOLLOWERS.forEach((name, i) => {
    lineFollowers().insert({
      line_user_id: `U${uuid().replace(/-/g, '')}`,
      display_name: name,
      followed_at: at(-(i * 2 + 1), `${10 + i}:${String(i * 7).padStart(2, '0')}`),
      last_seen_at: at(-i, `${12 + (i % 6)}:15`),
    });
  });
}

function seedProviders(ownerStaff: string) {
  table('payment_settings').insert({
    id: 'singleton',
    version: 3,
    provider: 'opn',
    mode: 'live',
    enabled: true,
    public_key: 'pkey_5xk2m9qz8w3hpc7a1d0',
    secret_key: 'demo-secret',
    webhook_secret: 'demo-webhook-secret',
    updated_at: at(-61, '11:05'),
  });
  table('lab_settings').insert({
    id: 'singleton',
    version: 2,
    provider: 'vetlab_asia',
    enabled: true,
    account_id: 'HPC-0021',
    base_url: 'https://api.vetlab-asia.co.th/v2',
    api_key: 'demo-lab-key',
    updated_at: at(-120, '14:40'),
  });
  calendarLinks().insert({ staff_id: ownerStaff, token: `cal_${uuid().replace(/-/g, '').slice(0, 24)}`, created_at: at(-33, '08:50'), last_used_at: at(0, '06:30') });
}

// ---- Loyalty ----------------------------------------------------------------------------------------

function seedLoyalty(ordered: Row[], staff: { owner: string; reception: string }) {
  // Points for every bill billing has settled (its invoice events give them too; this is idempotent).
  for (const invoice of table('invoices').where((i) => i.status === 'paid' || i.status === 'refunded')) {
    const paidAt = (invoice.payments ?? []).map((p: Row) => p.collected_at).sort().pop() ?? invoice.issued_at;
    earnPointsForInvoice(invoice as never, paidAt);
  }

  // What each customer brought over from the paper stamp cards, so tiers spread like a real clinic's.
  ordered.forEach((c, i) => {
    const opening = i === 1 || i === 3 ? 2_150 + Math.floor(random() * 600) : i < 8 ? 300 + Math.floor(random() * 500) : Math.floor(random() * 380);
    if (opening > 0) addPoints(c.id, 'adjust', opening, { note: 'ยอดยกมาจากบัตรสะสมแต้มเดิม', staff_id: staff.owner, occurred_at: at(-420 - (i % 5) * 7, '10:00') });
  });

  const demo = ordered[0];
  if (demo) {
    addPoints(demo.id, 'adjust', 120, { note: 'แต้มโปรโมชันเปิดสาขาอารีย์', staff_id: staff.owner, occurred_at: at(-760, '11:00') });
    addPoints(demo.id, 'adjust', 100, { note: 'ของขวัญวันเกิดน้อง', staff_id: staff.reception, occurred_at: at(-26, '09:10') });
    const bill = table('invoices').where((i) => i.customer_id === demo.id && i.status === 'paid').sort((a, b) => b.issued_at.localeCompare(a.issued_at))[0];
    addPoints(demo.id, 'redeem', -200, { invoice_id: bill?.id ?? null, invoice_number: bill?.number ?? null, note: 'แลกแต้มเป็นส่วนลด', occurred_at: at(-12, '15:20') });
  }
  if (ordered[5]) addPoints(ordered[5].id, 'adjust', 50, { note: 'ชดเชยรอคิวนานเกิน 1 ชั่วโมง', staff_id: staff.reception, occurred_at: at(-9, '17:45') });
  if (ordered[6]) addPoints(ordered[6].id, 'adjust', -80, { note: 'แก้แต้มที่บันทึกซ้ำจากบิลเดียวกัน', staff_id: staff.owner, occurred_at: at(-18, '12:00') });

  // Refer a friend: the customers module records who came with whose code; the points are ours.
  // One paid during the billing seed (it has `reward_invoice_id`) got its points from the bill already.
  for (const r of table('referrals').where((x) => x.status === 'rewarded' && !x.reward_invoice_id)) {
    const when = r.rewarded_at ?? r.created_at;
    if (r.referrer_points > 0) addPoints(r.referrer.customer_id, 'referral', r.referrer_points, { note: `แนะนำเพื่อน ${r.friend.name ?? ''}`.trim(), occurred_at: when });
    if (r.friend_points > 0) addPoints(r.friend.customer_id, 'referral', r.friend_points, { note: 'ลูกค้าใหม่จากการแนะนำของเพื่อน', occurred_at: when });
  }
}

// ---- Packages ---------------------------------------------------------------------------------------

function serviceIds(...codes: string[]): string[] {
  const all = services();
  const ids = codes.map((code) => all.find((s) => s.code === code)?.id).filter(Boolean) as string[];
  return ids.length ? ids : all.slice(0, 1).map((s) => s.id);
}

interface PackagePlan {
  owner: Row;
  name: string;
  codes: string[];
  uses: number;
  used: number;
  baht: number;
  boughtDaysAgo: number;
  validDays: number | null;
  paid: boolean;
  petMatch?: RegExp;
}

function sellPackage(plan: PackagePlan, staffId: string) {
  const pets = petsOf(plan.owner.id);
  const pet = plan.petMatch ? pets.find((p) => plan.petMatch!.test(p.species ?? '')) : pets[0];
  if (plan.petMatch && !pet) return;
  const branchId = homeBranchOf(plan.owner.id);
  if (!branchId) return;
  const bought = at(-plan.boughtDaysAgo, '11:30');
  let invoice: Row;
  try {
    invoice = createInvoice({
      customerId: plan.owner.id,
      branchId,
      staffId,
      petId: pet?.id,
      issuedAt: bought,
      lines: [{ description: `แพ็กเกจ ${plan.name} (${plan.uses} ครั้ง)`, quantity: 1, unit_price_satang: plan.baht * 100, kind: 'other' }],
    });
  } catch (error) {
    console.warn('[mock] crm seed: package bill not raised', error);
    return;
  }
  const row = packages().insert({
    name: plan.name,
    customer_id: plan.owner.id,
    pet_id: pet?.id ?? null,
    service_ids: serviceIds(...plan.codes),
    total_uses: plan.uses,
    used_count: plan.used,
    remaining: plan.uses - plan.used,
    price_satang: plan.baht * 100,
    branch_id: branchId,
    invoice_id: invoice.id,
    purchased_at: bought,
    expires_on: plan.validDays === null ? null : day(plan.validDays - plan.boughtDaysAgo),
    activated_at: null,
    cancelled_at: null,
    status: 'pending_payment',
  });
  if (plan.paid) {
    const paidAt = at(-plan.boughtDaysAgo, '11:40');
    try {
      takePayment(invoice as never, { method: pick(['card', 'promptpay', 'cash'] as const), amountSatang: invoice.total_satang, staffId, at: paidAt });
    } catch (error) {
      console.warn('[mock] crm seed: package bill not paid', error);
    }
    packages().update(row.id, { status: 'active', activated_at: paidAt });
  }
  refreshPackage(packages().get(row.id)!);
}

function seedPackages(ordered: Row[], staff: { reception: string }) {
  const [demo, a, b, c, d, e] = ordered;
  const plans: Array<PackagePlan | undefined> = [
    demo && { owner: demo, name: 'อาบน้ำตัดขน 10 ครั้ง', codes: ['GRM-BATH', 'GRM-FULL'], uses: 10, used: 4, baht: 3_000, boughtDaysAgo: 95, validDays: 365, paid: true },
    demo && { owner: demo, name: 'วัคซีนประจำปีครบชุด', codes: ['VAC-DOG', 'VAC-CAT', 'VAC-RAB'], uses: 2, used: 1, baht: 900, boughtDaysAgo: 41, validDays: 365, paid: true },
    a && { owner: a, name: 'วัคซีนลูกสุนัข 3 เข็ม', codes: ['VAC-DOG', 'VAC-RAB'], uses: 3, used: 2, baht: 1_650, boughtDaysAgo: 52, validDays: 180, paid: true, petMatch: /สุนัข|dog/i },
    b && { owner: b, name: 'ตรวจสุขภาพประจำปี 2 ครั้ง', codes: ['ANNUAL'], uses: 2, used: 0, baht: 3_200, boughtDaysAgo: 3, validDays: 365, paid: false },
    c && { owner: c, name: 'อาบน้ำแมว 5 ครั้ง', codes: ['GRM-CAT'], uses: 5, used: 5, baht: 2_200, boughtDaysAgo: 160, validDays: 365, paid: true, petMatch: /แมว|cat/i },
    d && { owner: d, name: 'อาบน้ำตัดขน 10 ครั้ง', codes: ['GRM-BATH', 'GRM-FULL'], uses: 10, used: 7, baht: 3_000, boughtDaysAgo: 400, validDays: 365, paid: true },
    e && { owner: e, name: 'อาบน้ำตัดขน 10 ครั้ง', codes: ['GRM-BATH', 'GRM-FULL'], uses: 10, used: 1, baht: 3_000, boughtDaysAgo: 20, validDays: 365, paid: true },
  ];
  for (const plan of plans) if (plan) sellPackage(plan, staff.reception);
}

// ---- Coupons ----------------------------------------------------------------------------------------

function seedCoupons(staff: { owner: string; reception: string }) {
  const branchesList = table('branches').all();
  const second = branchesList[1]?.id ?? null;
  const base = { per_customer_limit: null as number | null, branch_id: null as string | null, created_by_staff_id: staff.owner };
  coupons().insertMany([
    { ...base, code: 'WELCOME200', name: 'ลูกค้าใหม่ลด 200 บาท', percent: null, amount_satang: 20_000, service_ids: [], max_redemptions: null, per_customer_limit: 1, valid_from: day(-180), valid_until: day(185), redeemed_count: 42, created_at: at(-180, '09:00') },
    { ...base, code: 'VACCINE10', name: 'วัคซีนลด 10%', percent: 10, amount_satang: null, service_ids: serviceIds('VAC-DOG', 'VAC-CAT', 'VAC-RAB', 'VAC-FELV'), max_redemptions: 300, valid_from: day(-22), valid_until: day(39), redeemed_count: 58, created_at: at(-23, '15:30') },
    { ...base, code: 'BDAY300', name: 'สุขสันต์วันเกิดน้อง ลด 300 บาท', percent: null, amount_satang: 30_000, service_ids: [], max_redemptions: 100, per_customer_limit: 1, valid_from: day(-60), valid_until: day(305), redeemed_count: 37, created_at: at(-60, '10:10') },
    { ...base, code: 'GROOM15', name: 'อาบน้ำตัดขนลด 15% เปิดสาขาใหม่', percent: 15, amount_satang: null, service_ids: serviceIds('GRM-BATH', 'GRM-FULL', 'GRM-CAT'), max_redemptions: 150, valid_from: day(9), valid_until: day(40), branch_id: second, redeemed_count: 0, created_at: at(-2, '13:00') },
    { ...base, code: 'SONGKRAN25', name: 'สงกรานต์ ตรวจสุขภาพลด 25%', percent: 25, amount_satang: null, service_ids: serviceIds('ANNUAL', 'GEN-CHECK'), max_redemptions: 200, valid_from: day(-200), valid_until: day(-180), redeemed_count: 133, created_at: at(-205, '11:00') },
    { ...base, code: 'DENTAL500', name: 'ขูดหินปูนลด 500 บาท', percent: null, amount_satang: 50_000, service_ids: serviceIds('DEN-SCALE'), max_redemptions: 20, valid_from: day(-45), valid_until: day(15), redeemed_count: 20, created_at: at(-45, '09:30') },
    { ...base, code: 'PAWS50', name: 'ลด 50 บาท (แจกหน้าร้านผิดเงื่อนไข)', percent: null, amount_satang: 5_000, service_ids: [], max_redemptions: 500, valid_from: day(-15), valid_until: day(45), redeemed_count: 6, created_at: at(-15, '10:00'), created_by_staff_id: staff.reception, deactivated_at: at(-13, '18:20'), deactivated_by_staff_id: staff.owner, deactivation_reason: 'พิมพ์แจกโดยไม่ได้กำหนดสาขา ปิดก่อนแล้วออกคูปองใหม่' },
  ]);
}

// ---- Segments ---------------------------------------------------------------------------------------

function seedSegments(staff: { owner: string; reception: string }) {
  const first = table('branches').all()[0]?.id ?? null;
  const plans: Array<[string, Record<string, unknown>, number, string]> = [
    ['ไม่ได้มาเกิน 3 สัปดาห์', { inactive_days: 21 }, 34, staff.owner],
    ['เจ้าของสุนัขที่มาในเดือนนี้', { species: ['สุนัข'], visited_within_days: 30 }, 27, staff.reception],
    ['เจ้าของแมวทั้งหมด', { species: ['แมว'] }, 30, staff.owner],
    ['สมาชิกขั้นทองและขั้นเงิน', { tiers: ['gold', 'silver'] }, 21, staff.owner],
    ['ใช้จ่ายเกิน 5,000 บาท', { min_spend_satang: 500_000 }, 14, staff.owner],
    ['มาภายใน 60 วัน ยังไม่มีแพ็กเกจ', { visited_within_days: 60, has_active_package: false, branch_id: first }, 6, staff.reception],
    ['ลูกค้าที่มีแพ็กเกจใช้อยู่', { has_active_package: true }, 2, staff.owner],
  ];
  for (const [name, input, daysAgo, by] of plans) {
    const criteria = normaliseCriteria(input);
    const counted = at(-daysAgo, '10:30');
    segments().insert({ id: stableId(`segment-${name}`), name, criteria, ...countSegment(criteria), counted_at: counted, created_by_staff_id: by, created_at: counted });
  }
}

// ---- Conversations ----------------------------------------------------------------------------------

type Line = [direction: 'in' | 'out', daysAgo: number, time: string, text: string | null, by?: 'vet' | 'reception' | 'owner'];

function conversation(owner: Row, index: number): { channel: string; lines: Line[] } | undefined {
  const pets = petsOf(owner.id);
  const pet = pets[0]?.name ?? 'น้อง';
  const isCat = /แมว|cat/i.test(pets[0]?.species ?? '');
  const name = String(owner.display_name).replace(/^คุณ/, '');
  const scripts: Array<{ channel: string; lines: Line[] }> = [
    {
      channel: 'portal',
      lines: [
        ['in', 41, '19:05', `สวัสดีค่ะ อยากนัดฉีดวัคซีนประจำปีให้${pet} ช่วงเสาร์หน้าได้ไหมคะ`],
        ['out', 40, '09:20', `สวัสดีค่ะคุณ${name} เสาร์หน้ามีคิวว่างช่วง 10:30 และ 14:00 ค่ะ จองผ่านหน้านัดหมายได้เลย หรือแจ้งเวลาที่สะดวกไว้ แอดมินจองให้ค่ะ`, 'reception'],
        ['in', 40, '09:42', 'ขอ 10:30 ค่ะ ขอบคุณค่ะ'],
        ['out', 40, '09:50', `จองให้แล้วค่ะ เสาร์ 10:30 น. ${pet}ไม่ต้องงดอาหารนะคะ`, 'reception'],
        ['in', 3, '20:14', `สวัสดีค่ะ ${pet}กินยาครบ 5 วันแล้ว ผื่นที่ท้องยุบลงเยอะเลยค่ะ ต้องพาไปตรวจซ้ำไหมคะ`],
        ['out', 2, '09:12', `ดีใจที่อาการดีขึ้นนะคะ ถ้าผื่นยุบหมดและไม่คันแล้วไม่ต้องตรวจซ้ำค่ะ ให้ใช้แชมพูยาต่ออีก 2 สัปดาห์ สัปดาห์ละ 2 ครั้ง ถ้ากลับมาแดงหรือเกาอีกทักมาได้เลยค่ะ`, 'vet'],
        ['in', 2, '09:30', 'รับทราบค่ะ ขอบคุณคุณหมอมากค่ะ'],
        ['out', 2, '09:41', 'ยินดีค่ะ ถ้ามีอะไรผิดปกติทักมาได้ตลอดนะคะ', 'reception'],
      ],
    },
    {
      channel: 'line',
      lines: [
        ['in', 0, '07:52', `${pet}อาเจียน 2 ครั้งตั้งแต่เมื่อคืน ยังกินน้ำได้ปกติ แต่ไม่ยอมกินข้าวเลยครับ`],
        ['in', 0, '07:53', null],
        ['in', 0, '07:55', 'วันนี้ช่วงสายมีคิวว่างไหมครับ'],
      ],
    },
    {
      channel: 'line',
      lines: [
        ['in', 1, '21:40', isCat ? 'ขอสอบถามราคาทำหมันแมวตัวเมียค่ะ ต้องงดอาหารกี่ชั่วโมงคะ' : 'ขอสอบถามราคาทำหมันสุนัขตัวผู้ น้ำหนักประมาณ 12 กิโลค่ะ ต้องงดอาหารกี่ชั่วโมงคะ'],
        ['in', 1, '21:41', 'ต้องนอนค้างที่คลินิกไหมคะ'],
      ],
    },
    {
      channel: 'line',
      lines: [
        ['in', 6, '13:10', 'แพ็กเกจอาบน้ำ 10 ครั้ง ใช้กับน้องได้ 2 ตัวไหมคะ'],
        ['out', 6, '13:25', 'สวัสดีค่ะ แพ็กเกจผูกกับน้องตัวที่ซื้อไว้ค่ะ ถ้าซื้อแบบไม่ระบุตัว ใช้ได้กับน้องทุกตัวในบัญชีเดียวกัน อายุแพ็กเกจ 1 ปีนับจากวันชำระค่ะ', 'reception'],
        ['in', 6, '13:31', 'โอเคค่ะ เดี๋ยวพรุ่งนี้แวะไปซื้อที่เคาน์เตอร์นะคะ'],
        ['out', 6, '13:33', 'ยินดีค่ะ แจ้งชื่อน้องที่เคาน์เตอร์ได้เลยค่ะ', 'reception'],
      ],
    },
    {
      channel: 'line',
      lines: [
        ['in', 0, '09:05', `พรุ่งนี้นัด${pet} 10 โมง ขอเลื่อนเป็นบ่ายสองได้ไหมครับ ติดประชุมด่วน`],
      ],
    },
    {
      channel: 'portal',
      lines: [
        ['in', 4, '18:22', `ผลเลือดของ${pet}ออกหรือยังครับ`],
        ['out', 4, '18:40', `ผลออกแล้วค่ะ ค่าตับปกติ ค่าไตสูงกว่าเกณฑ์เล็กน้อย คุณหมอแนะนำให้เปลี่ยนเป็นอาหารสูตรดูแลไตและนัดตรวจซ้ำอีก 1 เดือน รายละเอียดดูได้ในประวัติการรักษาในระบบออนไลน์ค่ะ`, 'vet'],
        ['in', 4, '19:02', 'ขอบคุณครับ อาหารสูตรไตที่คลินิกมีขายไหมครับ'],
        ['out', 3, '08:45', 'มีค่ะ ทั้งแบบเม็ดและแบบเปียก แวะรับได้ที่เคาน์เตอร์ทั้งสองสาขาค่ะ', 'reception'],
      ],
    },
    {
      channel: 'line',
      lines: [
        ['in', 1, '16:30', 'วันหยุดนักขัตฤกษ์ที่จะถึงนี้คลินิกเปิดไหมคะ'],
      ],
    },
    {
      channel: 'line',
      lines: [
        ['in', 9, '10:02', `ขอบคุณคุณหมอที่ดูแล${pet}เมื่อวานนะคะ ตอนนี้ร่าเริงแล้วค่ะ`],
        ['out', 9, '10:15', `ดีใจด้วยค่ะ ฝากหอมแก้ม${pet}ด้วยนะคะ`, 'vet'],
        ['in', 9, '10:16', null],
      ],
    },
  ];
  return scripts[index];
}

function seedThreads(ordered: Row[], staff: { owner: string; vet: string; reception: string }) {
  const people = ordered.filter((c) => petsOf(c.id).length > 0).slice(0, 8);
  people.forEach((owner, index) => {
    const script = conversation(owner, index);
    if (!script) return;
    // Someone without LINE writes from the portal; someone with it, mostly on LINE.
    const channel = script.channel === 'line' && !owner.line?.line_user_id ? 'portal' : script.channel;
    const lineOk = Boolean(owner.line?.line_user_id && !owner.line?.unfollowed_at);
    const thread = threads().insert({ id: stableId(`thread-${index}`), customer_id: owner.id, branch_id: homeBranchOf(owner.id), version: 1, created_at: at(-(script.lines[0]![1] + 1), '08:00') });
    for (const [direction, daysAgo, time, text, by] of script.lines) {
      const sentAt = at(-daysAgo, time);
      if (direction === 'in') {
        threadMessages().insert({ thread_id: thread.id, direction: 'inbound', channel, kind: text === null ? (index === 1 ? 'image' : 'sticker') : 'text', ...(text !== null ? { text } : {}), sent_at: sentAt });
      } else {
        const staffId = staff[by ?? 'reception'];
        const replyChannel = lineOk ? 'line' : owner.phone ? 'sms' : 'email';
        threadMessages().insert({
          thread_id: thread.id,
          direction: 'outbound',
          channel: replyChannel,
          kind: 'text',
          text: text!,
          sent_at: sentAt,
          sent_by_staff_id: staffId,
          sent_by_name: table('staff_members').get(staffId)?.display_name ?? 'คลินิก',
          delivery: { status: 'sent', channel: replyChannel },
        });
        inbox().insert({ customer_id: owner.id, template: 'thread.reply', title: 'ข้อความตอบกลับจากคลินิก', body: text!, sent_at: sentAt, read_at: index === 0 && daysAgo < 3 ? null : sentAt });
        logDelivery(owner, 'thread.reply', replyChannel as never, 'system', sentAt, { staffId });
      }
    }
  });
}

// ---- The sent-message history -----------------------------------------------------------------------

type Ch = 'line' | 'sms' | 'email';

function recipientOf(owner: Row, channel: Ch): string | undefined {
  return channel === 'line' ? maskLine(owner.line?.line_user_id) : channel === 'sms' ? maskPhone(owner.phone) : maskEmail(owner.email);
}

function logDelivery(
  owner: Row,
  template: string,
  channel: Ch | undefined,
  source: string,
  attemptedAt: string,
  extra: { status?: string; error?: string; fallbackFrom?: Ch; batchId?: string; staffId?: string; automationId?: string; subjectKey?: string } = {},
) {
  deliveries().insert({
    customer_id: owner.id,
    customer_name: owner.display_name,
    template,
    ...(channel ? { channel, recipient: recipientOf(owner, channel) } : {}),
    status: extra.status ?? 'sent',
    ...(extra.error ? { error_code: extra.error } : {}),
    ...(extra.fallbackFrom ? { fallback_from: extra.fallbackFrom } : {}),
    source,
    ...(extra.batchId ? { batch_id: extra.batchId } : {}),
    ...(extra.staffId ? { sent_by_staff_id: extra.staffId } : {}),
    branch_id: homeBranchOf(owner.id),
    attempted_at: attemptedAt,
    ...(extra.automationId ? { automation_id: extra.automationId } : {}),
    ...(extra.subjectKey ? { subject_key: extra.subjectKey } : {}),
  });
}

/** One message as the dispatcher would have sent it then: LINE first, SMS after a refusal. */
function sentBefore(owner: Row, template: string, values: Record<string, string>, source: string, attemptedAt: string, extra: { batchId?: string; staffId?: string; read?: boolean } = {}) {
  const words = wordsOf(template, values);
  inbox().insert({ customer_id: owner.id, template, title: words.title, body: words.line, sent_at: attemptedAt, read_at: extra.read === false ? null : attemptedAt });
  if (owner.line?.line_user_id && owner.line.unfollowed_at && owner.line.unfollowed_at < attemptedAt) {
    logDelivery(owner, template, 'line', source, attemptedAt, { status: 'failed', error: 'unfollowed', ...extra });
    logDelivery(owner, template, 'sms', source, attemptedAt, { fallbackFrom: 'line', ...extra });
  } else if (owner.line?.line_user_id) {
    logDelivery(owner, template, 'line', source, attemptedAt, extra);
  } else if (owner.phone) {
    logDelivery(owner, template, 'sms', source, attemptedAt, extra);
  } else {
    logDelivery(owner, template, owner.email ? 'email' : undefined, source, attemptedAt, owner.email ? extra : { status: 'failed', error: 'no_channel', ...extra });
  }
}

const thaiDay = (offset: number) => new Date(`${day(offset)}T00:00:00`).toLocaleDateString('th-TH', { day: 'numeric', month: 'short', year: 'numeric' });

function seedMessages(ordered: Row[], staff: { owner: string; vet: string; reception: string }) {
  const demo = ordered[0];
  const withPets = ordered.filter((c) => petsOf(c.id).length > 0);

  // The demo customer's inbox, so the portal bell has something real in it.
  if (demo) {
    const pet = petsOf(demo.id)[0]?.name ?? 'น้อง';
    sentBefore(demo, 'clinic.announcement', { message: 'Happy Paws สาขาอารีย์เปิดบริการอาบน้ำตัดขนทุกวัน 10:00-17:00 แล้ว จองคิวผ่านระบบออนไลน์ได้เลย' }, 'broadcast', at(-10, '11:00'));
    sentBefore(demo, 'consultation.summary', {}, 'system', at(-5, '15:42'));
    sentBefore(demo, 'receipt.issued', { receipt_number: `RC${day(-5).replace(/-/g, '').slice(2)}-0012`, amount: '1,280.00' }, 'system', at(-5, '15:50'));
    sentBefore(demo, 'vaccine.due', { pet_name: pet, date: thaiDay(6), vaccine_name: 'วัคซีนพิษสุนัขบ้า' }, 'automation', at(-1, '09:00'), { read: false });
  }

  // A fortnight of the clinic's own messages to everyone else.
  const vaccines = ['วัคซีนรวม', 'วัคซีนพิษสุนัขบ้า', 'วัคซีนไข้หวัดแมว'];
  withPets.slice(1).forEach((owner, i) => {
    const pet = petsOf(owner.id)[0]!.name;
    const d = 1 + (i % 13);
    sentBefore(owner, 'appointment.reminder', { pet_name: pet, date: thaiDay(-d + 1), time: `${9 + (i % 8)}:${i % 2 ? '30' : '00'}` }, 'automation', at(-d, '18:00'));
    if (i % 2 === 0) sentBefore(owner, 'queue.called', { number: `A${String(10 + i).padStart(3, '0')}`, room: `ห้องตรวจ ${1 + (i % 3)}` }, 'system', at(-d + 1, `${9 + (i % 8)}:${i % 2 ? '42' : '12'}`));
    if (i % 3 === 0) sentBefore(owner, 'receipt.issued', { receipt_number: `RC${day(-d + 1).replace(/-/g, '').slice(2)}-${String(20 + i).padStart(4, '0')}`, amount: `${(650 + i * 135).toLocaleString('th-TH')}.00` }, 'system', at(-d + 1, '11:05'));
    if (i % 4 === 1) sentBefore(owner, 'vaccine.due', { pet_name: pet, date: thaiDay(7 - d), vaccine_name: pick(vaccines) }, 'automation', at(-d, '09:00'));
    if (i % 5 === 2) sentBefore(owner, 'consultation.summary', {}, 'system', at(-d + 1, '16:20'));
  });

  // Two counter sends.
  const manualBatch = uuid();
  for (const owner of withPets.slice(2, 5)) {
    sentBefore(owner, 'clinic.message', { message: 'คลินิกขอเลื่อนเวลาเปิดวันเสาร์นี้เป็น 10:00 น. เนื่องจากปรับปรุงระบบไฟฟ้า ขออภัยในความไม่สะดวก' }, 'manual', at(-4, '17:30'), { batchId: manualBatch, staffId: staff.reception });
  }

  // Last week's LINE announcement: marketing, so those who did not agree were left out.
  const batch = uuid();
  const message = 'เดือนนี้วัคซีนพิษสุนัขบ้าลด 10% ทั้งสองสาขา แสดงคูปอง VACCINE10 ที่เคาน์เตอร์ ถึงสิ้นเดือนหน้า';
  for (const owner of ordered.filter((c) => c.line?.line_user_id)) {
    if (!hasMarketingConsent(owner.id)) {
      logDelivery(owner, 'clinic.announcement', undefined, 'broadcast', at(-6, '10:00'), { status: 'skipped', error: 'no_consent', batchId: batch, staffId: staff.owner });
    } else if (owner.line.unfollowed_at) {
      logDelivery(owner, 'clinic.announcement', 'line', 'broadcast', at(-6, '10:00'), { status: 'skipped', error: 'unfollowed', batchId: batch, staffId: staff.owner });
    } else {
      sentBefore(owner, 'clinic.announcement', { message }, 'broadcast', at(-6, '10:00'), { batchId: batch, staffId: staff.owner });
    }
  }
}

// ---- Reminder rules ---------------------------------------------------------------------------------

function seedAutomations(staff: { owner: string }) {
  const rule = (name: string, trigger: Row | Record<string, unknown>, channel: string, enabled: boolean, stats: Record<string, number>, createdDaysAgo: number, condition: Record<string, unknown> = {}, starter = false) =>
    automations().insert({
      id: stableId(`automation-${name}`),
      version: 1 + Math.floor(createdDaysAgo / 60),
      name,
      enabled,
      trigger,
      condition: { unconfirmed_only: false, ...condition },
      action: { type: 'send_notification', channel },
      ...(starter ? {} : { created_by_staff_id: staff.owner }),
      created_at: at(-createdDaysAgo, '10:00'),
      seed_stats: { ...stats, last_fired_at: at(-1, '18:00') },
    });
  rule('เตือนวัคซีนล่วงหน้า 7 วัน', { type: 'vaccine_due', offset_days: 7, send_time: '09:00' }, 'auto', true, { total: 412, sent: 389, skipped: 17, failed: 6, lost: 0, last_30_days: 61 }, 300, {}, true);
  rule('เตือนนัดหมายล่วงหน้า 1 วัน', { type: 'appointment_upcoming', offset_days: 1, send_time: '18:00' }, 'auto', true, { total: 1_284, sent: 1_241, skipped: 31, failed: 12, lost: 0, last_30_days: 176 }, 300, {}, true);
  rule('เตือนนัดที่ยังไม่ยืนยัน 3 ชั่วโมงก่อน', { type: 'appointment_upcoming', offset_hours: 3 }, 'sms', true, { total: 96, sent: 92, skipped: 3, failed: 1, lost: 0, last_30_days: 18 }, 75, { unconfirmed_only: true });
  rule('คิดถึงน้อง (ลูกค้าที่หายไป)', { type: 'customer_lapsed', offset_days: -7, send_time: '10:00' }, 'line', true, { total: 58, sent: 41, skipped: 17, failed: 0, lost: 0, last_30_days: 9 }, 120);
  rule('ชวนรีวิวหลังมาใช้บริการ', { type: 'visit_completed', offset_days: -1, send_time: '19:00' }, 'line', false, { total: 23, sent: 19, skipped: 4, failed: 0, lost: 0, last_30_days: 0 }, 50);
}

// ---- Webhooks and API keys --------------------------------------------------------------------------

function seedWebhooks(ownerStaff: string) {
  const zap = webhooks().insert({
    id: stableId('webhook-zapier'),
    url: 'https://hooks.zapier.com/hooks/catch/1846203/bx7k2pq/',
    events: ['appointment.created', 'appointment.rescheduled', 'appointment.cancelled'],
    description: 'ส่งนัดใหม่เข้า Google Sheet ของทีมต้อนรับผ่าน Zapier',
    active: true,
    version: 2,
    secret: 'whsec_demo_zapier',
    created_by_staff_id: ownerStaff,
    created_at: at(-74, '14:10'),
    updated_at: at(-30, '09:00'),
  });
  const erp = webhooks().insert({
    id: stableId('webhook-erp'),
    url: 'https://erp.happypaws-account.co.th/api/webhooks/clinic',
    events: ['invoice.issued', 'payment.succeeded', 'payment.refunded'],
    description: 'ระบบบัญชีรับยอดขายและการคืนเงินรายวัน',
    active: true,
    version: 3,
    secret: 'whsec_demo_erp',
    created_by_staff_id: ownerStaff,
    created_at: at(-150, '10:30'),
    updated_at: at(-3, '08:15'),
  });

  const deliver = (hookId: string, type: string, created: string, outcome: 'ok' | 'down' | 'recovered') => {
    const createdMs = Date.parse(created);
    const lastAt = new Date(createdMs + (outcome === 'ok' ? 800 : outcome === 'recovered' ? 26 * 60_000 : 156 * 60_000)).toISOString();
    webhookDeliveries().insert({
      webhook_id: hookId,
      event_id: `evt_${uuid().replace(/-/g, '').slice(0, 24)}`,
      event_type: type,
      status: outcome === 'down' ? 'failed' : 'succeeded',
      attempts: outcome === 'ok' ? 1 : outcome === 'recovered' ? 3 : 6,
      max_attempts: 6,
      last_attempt_at: lastAt,
      last_status_code: outcome === 'down' ? 503 : 200,
      ...(outcome === 'down' ? { last_error: 'ปลายทางตอบ 503 Service Unavailable', last_response_body: '<html><body>Service Unavailable - maintenance</body></html>' } : { last_response_body: '{"received":true}' }),
      last_duration_ms: outcome === 'down' ? 10_000 : 140 + Math.floor(random() * 380),
      completed_at: lastAt,
      created_at: created,
    });
  };

  const zapEvents = ['appointment.created', 'appointment.created', 'appointment.rescheduled', 'appointment.cancelled'];
  for (let i = 0; i < 26; i++) {
    const daysAgo = Math.floor(i / 3);
    deliver(zap.id, pick(zapEvents), at(-daysAgo, `${9 + (i % 9)}:${String((i * 13) % 60).padStart(2, '0')}`), 'ok');
  }
  const erpEvents = ['invoice.issued', 'payment.succeeded', 'payment.succeeded', 'payment.refunded'];
  for (let i = 0; i < 34; i++) {
    const daysAgo = Math.floor(i / 4);
    // The accounting server was down for maintenance three nights ago: those deliveries ran out of tries.
    const outcome = daysAgo === 3 && i % 4 !== 0 ? 'down' : daysAgo === 2 && i % 4 === 1 ? 'recovered' : 'ok';
    deliver(erp.id, i % 9 === 8 ? 'payment.refunded' : pick(erpEvents.slice(0, 3)), at(-daysAgo, `${10 + (i % 8)}:${String((i * 17) % 60).padStart(2, '0')}`), outcome);
  }
}

function seedApiKeys(ownerStaff: string) {
  apiKeys().insertMany([
    {
      name: 'Looker Studio รายงานผู้บริหาร',
      prefix: 'hp_7f3a9c21',
      scopes: ['analytics:read', 'appointment:read'],
      branch_ids: [],
      created_by_staff_id: ownerStaff,
      created_at: at(-64, '15:00'),
      expires_at: at(301, '15:00'),
      last_used_at: at(0, '07:00'),
    },
    {
      name: 'ทดสอบเชื่อมระบบบัญชี (เลิกใช้)',
      prefix: 'hp_b21e04d8',
      scopes: ['crm:read'],
      branch_ids: [],
      created_by_staff_id: ownerStaff,
      created_at: at(-160, '11:20'),
      expires_at: at(205, '11:20'),
      last_used_at: at(-151, '16:45'),
      revoked_at: at(-150, '10:25'),
    },
  ]);
}
