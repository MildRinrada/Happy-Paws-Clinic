import { DEMO_ACCOUNTS } from '../../core/demo-accounts';
import { at, day, table, uuid } from '../../core/db';
import { signedUpload } from '../../core/files';
import { petAvatar } from './avatars';
import { LEGACY_LABEL, policies, type PolicyRow } from './privacy';
import { TEMPLATE_SECTIONS, TEMPLATE_VERSION } from './privacy-template';
import { referralCodeOf, referrals } from './referrals';
import { DEMO_CUSTOMER_KEY, SEED_CUSTOMERS, SEED_PETS, VACCINES } from './seed-data';
import {
  allergies,
  carers,
  consents,
  customers,
  dataRequests,
  invitations,
  nextHospitalNumber,
  petDocuments,
  pets,
  referralCodes,
  vaccinations,
  weights,
  type ConsentRow,
  type CustomerRow,
} from './store';

/**
 * The register: about forty customers and sixty animals with weights, vaccines, allergies and
 * carers; the privacy notice in two published versions and a draft; consents, data requests and
 * refer-a-friend codes. Runs after the clinic's staff (order 0) so vets can be named on doses.
 */
export const order = 10;

/** A repeatable sequence, so the sample reads the same every day it is planted. */
function random(seed: number) {
  let state = seed >>> 0;
  return () => {
    state = (state + 0x6d2b79f5) >>> 0;
    let t = state;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * Fixed ids for the sample customers and pets, so a link to one (in the guide, in a note, in a
 * test) keeps working after "ล้างข้อมูลเดโม". The demo customer is `seedCustomerId(0)`.
 */
export const seedCustomerId = (index: number) => `7a1d0010-0000-4000-8000-${String(index + 1).padStart(12, '0')}`;
export const seedPetId = (index: number) => `7a1d0011-0000-4000-8000-${String(index + 1).padStart(12, '0')}`;

const roundKg = (kg: number) => (kg < 1 ? Math.round(kg * 100) / 100 : Math.round(kg * 10) / 10);

export function seed() {
  const rand = random(20261023);
  const pick = <T,>(list: T[]): T | undefined => list[Math.floor(rand() * list.length)];
  const staff = table('staff_members').all();
  const vets = staff.filter((member) => member.staff_type === 'veterinarian').map((member) => member.id as string);
  const ownerStaff = staff.find((member) => member.user_id === DEMO_ACCOUNTS.owner.user_id)?.id ?? staff[0]?.id ?? '';
  const counterStaff = staff.find((member) => member.user_id === DEMO_ACCOUNTS.reception.user_id)?.id ?? ownerStaff;
  const vetOf = () => pick(vets) ?? ownerStaff;

  // ---- Customers ------------------------------------------------------------------------------
  const byKey = new Map<string, CustomerRow>();
  for (const [index, entry] of SEED_CUSTOMERS.entries()) {
    const flags = entry.flags ?? '';
    const created = at(-entry.since, `${String(9 + (entry.since % 9)).padStart(2, '0')}:${String((entry.since * 7) % 60).padStart(2, '0')}`);
    const row = customers().insert({
      id: seedCustomerId(index),
      version: 1 + (entry.since % 3),
      display_name: entry.name,
      phone: entry.phone,
      ...(entry.email ? { email: entry.email } : {}),
      has_portal_account: flags.includes('p'),
      is_active: !flags.includes('x'),
      created_at: created,
      ...(entry.key === DEMO_CUSTOMER_KEY ? { user_id: DEMO_ACCOUNTS.customer.user_id } : {}),
      ...(flags.includes('l')
        ? {
            line: {
              line_user_id: `U${uuid().replace(/-/g, '')}`,
              display_name: entry.line,
              linked_at: at(-Math.min(entry.since, 200), '11:00'),
              ...(flags.includes('u') ? { unfollowed_at: at(-20, '21:14') } : {}),
            },
          }
        : {}),
    } as CustomerRow);
    byKey.set(entry.key, row);

    if (flags.includes('i')) {
      invitations().insert({ customer_id: row.id, sent_to: entry.phone, invited_by_staff_id: counterStaff, sent_at: at(-2, '10:15'), expires_at: at(5, '10:15') });
    }
    if (flags.includes('e')) {
      invitations().insert({ customer_id: row.id, sent_to: entry.phone, invited_by_staff_id: counterStaff, sent_at: at(-30, '14:40'), expires_at: at(-23, '14:40') });
    }
  }
  const idOf = (key: string) => byKey.get(key)!.id;

  // ---- Pets -----------------------------------------------------------------------------------
  for (const [index, entry] of SEED_PETS.entries()) {
    const owner = byKey.get(entry.owner)!;
    const ownerSince = SEED_CUSTOMERS.find((customer) => customer.key === entry.owner)!.since;
    const registeredDaysAgo = Math.min(ownerSince, entry.age ?? ownerSince);
    const created = at(-registeredDaysAgo, '10:00');
    const isDemo = entry.owner === DEMO_CUSTOMER_KEY;

    const pet = pets().insert({
      id: seedPetId(index),
      version: 1,
      hospital_number: nextHospitalNumber(new Date(created).getFullYear()),
      name: entry.name,
      species: entry.species,
      ...(entry.breed ? { breed: entry.breed } : {}),
      ...(entry.chip ? { microchip_number: entry.chip } : {}),
      primary_customer_id: owner.id,
      is_deceased: entry.deceasedDaysAgo !== undefined,
      ...(entry.deceasedDaysAgo !== undefined ? { deceased_at: at(-entry.deceasedDaysAgo, '15:20'), deceased_cancelled_ids: [] } : {}),
      ...(entry.nickname ? { nickname: entry.nickname } : {}),
      ...(entry.diet ? { diet: entry.diet } : {}),
      ...(entry.behaviour ? { behaviour: entry.behaviour } : {}),
      ...(entry.sex ? { sex: entry.sex } : {}),
      ...(entry.age !== undefined ? { birth_date: day(-entry.age), birth_date_estimated: Boolean(entry.estimated) } : {}),
      ...(entry.neutered !== undefined ? { neutered: entry.neutered } : {}),
      ...(entry.photo ? { photo_url: petAvatar(entry.photo) } : {}),
      created_at: created,
    });

    carers().insert({ pet_id: pet.id, customer_id: owner.id, is_primary: true, may_view_history: true, created_at: created });
    for (const [key, mayView] of entry.carers ?? []) {
      carers().insert({ pet_id: pet.id, customer_id: idOf(key), is_primary: false, may_view_history: mayView, created_at: at(-Math.floor(registeredDaysAgo / 2), '16:00') });
    }

    for (const [substance, severity, reaction] of entry.allergies ?? []) {
      allergies().insert({ pet_id: pet.id, substance, severity, ...(reaction ? { reaction } : {}), recorded_at: at(-Math.floor(registeredDaysAgo * 0.6), '11:30') });
    }

    // Weighings: monthly-ish for the demo customer's animals, a few for everyone else.
    if (entry.kg !== undefined) {
      const count = isDemo ? 9 : 1 + Math.floor(rand() * 4);
      const last = entry.deceasedDaysAgo !== undefined ? entry.deceasedDaysAgo + 10 : isDemo ? 6 : 5 + Math.floor(rand() * 80);
      const first = Math.max(last, Math.min(registeredDaysAgo, isDemo ? 760 : 540));
      const young = (entry.age ?? 9999) < 420;
      for (let index = 0; index < count; index += 1) {
        const share = count === 1 ? 1 : index / (count - 1);
        const daysAgo = Math.round(first - (first - last) * share);
        // A growing animal gains toward today's weight; a grown one wanders a few per cent around it.
        const age = (entry.age ?? 9999) - daysAgo;
        const growth = young || age < 300 ? Math.min(1, 0.35 + 0.65 * Math.max(0, age) / 300) : 1;
        // A gentle drift up to today's weight with a little scale noise, as real charts look.
        const wobble = index === count - 1 ? 1 : (0.93 + 0.07 * share) * (1 + (rand() - 0.5) * 0.02);
        weights().insert({ pet_id: pet.id, weight_kg: roundKg(entry.kg * growth * wobble), recorded_at: at(-daysAgo, '10:40'), consultation_id: '' });
      }
    }

    // Vaccines: yearly doses, the latest given at a spread of dates so some are due and some overdue.
    const course = VACCINES[entry.species] ?? [];
    if ((entry.age ?? 0) > 90) {
      course.forEach((vaccine, index) => {
        const lastGiven = isDemo
          ? [[345, 200], [372, 150], [100, 0]][SEED_PETS.filter((p) => p.owner === DEMO_CUSTOMER_KEY).indexOf(entry)]![index] ?? 120
          : 20 + Math.floor(rand() * 400);
        const doses = Math.max(1, Math.min(3, Math.floor(((entry.age ?? 0) - lastGiven) / 365) + 1));
        for (let dose = doses - 1; dose >= 0; dose -= 1) {
          const daysAgo = lastGiven + dose * vaccine.days;
          if (entry.deceasedDaysAgo !== undefined && daysAgo < entry.deceasedDaysAgo) continue;
          vaccinations().insert({
            pet_id: pet.id,
            vaccine_name: vaccine.name,
            manufacturer: vaccine.manufacturer,
            lot_number: `${vaccine.manufacturer.slice(0, 2).toUpperCase()}${String(240000 + Math.floor(rand() * 9999))}`,
            vial_expires_on: day(-daysAgo + 300),
            administered_at: at(-daysAgo, '11:10'),
            administered_by_staff_id: vetOf(),
            next_due_on: day(-daysAgo + vaccine.days),
          });
        }
      });
    }

    if (isDemo) seedDocuments(pet.id, entry.name, vetOf);
  }

  seedConsents(byKey, rand);
  seedDataRequests(idOf, ownerStaff);
  seedReferrals(idOf);
  seedPrivacyPolicy(ownerStaff);
}

function seedDocuments(petId: string, name: string, vetOf: () => string) {
  const files: Record<string, Array<[kind: string, title: string, file: string, type: string, size: number, owner: boolean, daysAgo: number]>> = {
    โมจิ: [
      ['lab_result', 'ผลตรวจเลือด CBC และค่าเคมีในเลือด', 'cbc-chem-moji.pdf', 'application/pdf', 182_344, true, 34],
      ['imaging', 'ภาพเอกซเรย์ข้อเข่าหลังขวา', 'xray-knee-r.jpg', 'image/jpeg', 1_204_118, false, 34],
      ['other', 'ใบยินยอมการวางยาสลบ', 'consent-anesthesia.pdf', 'application/pdf', 96_210, true, 210],
    ],
    ส้มจี๊ด: [
      ['referral', 'ใบส่งตัวจากโรงพยาบาลสัตว์เดิม', 'referral-letter.pdf', 'application/pdf', 240_905, false, 400],
      ['lab_result', 'ผลตรวจปัสสาวะ', 'urinalysis.pdf', 'application/pdf', 88_120, true, 75],
    ],
    ขนมปัง: [['photo', 'รูปฟันหน้าก่อนกรอฟัน', 'teeth-before.jpg', 'image/jpeg', 640_552, true, 50]],
  };
  for (const [kind, title, file_name, content_type, size_bytes, visible_to_owner, daysAgo] of files[name] ?? []) {
    const upload = signedUpload({ name: file_name, content_type, size: size_bytes });
    petDocuments().insert({ pet_id: petId, kind, title, file_name, content_type, size_bytes, visible_to_owner, file_id: upload.id, uploaded_by_staff_id: vetOf(), created_at: at(-daysAgo, '13:25') });
  }
}

function seedConsents(byKey: Map<string, CustomerRow>, rand: () => number) {
  const portal = SEED_CUSTOMERS.filter((entry) => entry.flags?.includes('p'));
  for (const entry of portal) {
    const customerId = byKey.get(entry.key)!.id;
    const when = at(-Math.min(entry.since, 300), '20:15');
    const rows: Array<Omit<ConsentRow, 'id'>> = [
      { customer_id: customerId, purpose: 'service_messages', granted: true, recorded_at: when, source: 'portal', policy_version: 'pending' },
      { customer_id: customerId, purpose: 'marketing', granted: entry.key === DEMO_CUSTOMER_KEY || rand() > 0.4, recorded_at: when, source: 'portal', policy_version: 'pending' },
    ];
    if (entry.key === DEMO_CUSTOMER_KEY || rand() > 0.6) {
      rows.push({ customer_id: customerId, purpose: 'medical_record_sharing', granted: entry.key === DEMO_CUSTOMER_KEY ? true : rand() > 0.5, recorded_at: when, source: 'portal', policy_version: 'pending' });
    }
    consents().insertMany(rows);
  }
  // Two signed at the counter on paper and keyed in.
  for (const key of ['wichai', 'somsak']) {
    consents().insert({ customer_id: byKey.get(key)!.id, purpose: 'marketing', granted: false, recorded_at: at(-90, '10:05'), source: 'counter', recorded_by_user_id: DEMO_ACCOUNTS.reception.user_id, note: 'ลูกค้าแจ้งที่เคาน์เตอร์ว่าไม่ต้องการรับโปรโมชัน', policy_version: 'pending' });
  }
}

function seedDataRequests(idOf: (key: string) => string, ownerStaff: string) {
  void ownerStaff;
  dataRequests().insertMany([
    { version: 1, customer_id: idOf('pimchanok'), type: 'access', status: 'open', raised_at: at(-3, '21:40'), detail: 'ขอสำเนาประวัติการรักษาของมะลิและทองหยอดทั้งหมด เพื่อใช้ยื่นประกันสัตว์เลี้ยง' },
    { version: 1, customer_id: idOf('wichai'), type: 'correction', status: 'open', raised_at: at(-9, '09:12'), detail: 'ชื่อในระบบสะกดผิด ที่ถูกคือ "วิชัย ทองคำ" และขอเปลี่ยนอีเมลติดต่อ' },
    { version: 2, customer_id: idOf('orapin'), type: 'erasure', status: 'completed', raised_at: at(-48, '16:30'), detail: 'ย้ายไปต่างจังหวัด ขอให้ลบข้อมูลการติดต่อ', closed_at: at(-40, '11:00'), closed_by_user_id: DEMO_ACCOUNTS.owner.user_id, resolution: 'ลบอีเมลและปิดการรับข้อความแล้ว เวชระเบียนเก็บต่อตามระยะเวลาที่กฎหมายกำหนด แจ้งลูกค้าทางโทรศัพท์แล้ว' },
  ]);
}

function seedReferrals(idOf: (key: string) => string) {
  // The demo customer's code is fixed, so the guide can link to /r/NAPAJ268.
  referralCodes().insert({ customer_id: idOf('napa'), code: 'NAPAJ268', created_at: at(-200, '12:00') });
  for (const key of ['napa', 'pimchanok', 'jiraporn', 'thitima', 'kanokwan', 'somchai']) referralCodeOf(idOf(key), at(-200, '12:00'));
  const name = (key: string) => customers().get(idOf(key))?.display_name;
  const code = (key: string) => referralCodeOf(idOf(key)).code;
  const party = (key: string) => ({ customer_id: idOf(key), name: name(key) });
  referrals().insertMany([
    { referrer: party('napa'), friend: party('lalita'), code: code('napa'), channel: 'guest_booking', status: 'rewarded', referrer_points: 100, friend_points: 100, over_monthly_cap: false, created_at: at(-74, '19:20'), rewarded_at: at(-72, '15:05') },
    { referrer: party('napa'), friend: party('ekkachai'), code: code('napa'), channel: 'register', status: 'pending', referrer_points: 0, friend_points: 0, over_monthly_cap: false, created_at: at(-11, '20:41') },
    { referrer: party('napa'), friend: party('jintana'), code: code('napa'), channel: 'guest_booking', status: 'pending', referrer_points: 0, friend_points: 0, over_monthly_cap: false, created_at: at(-5, '08:55') },
    { referrer: party('jiraporn'), friend: party('krit'), code: code('jiraporn'), channel: 'guest_booking', status: 'rewarded', referrer_points: 100, friend_points: 100, over_monthly_cap: false, created_at: at(-95, '13:10'), rewarded_at: at(-93, '17:45') },
    { referrer: party('thitima'), friend: party('piyawat'), code: code('thitima'), channel: 'register', status: 'pending', referrer_points: 0, friend_points: 0, over_monthly_cap: false, created_at: at(-2, '18:02') },
    { referrer: party('somchai'), friend: party('somchai2'), code: code('somchai'), channel: 'guest_booking', status: 'rejected', reject_reason: 'shared_contact', referrer_points: 0, friend_points: 0, over_monthly_cap: false, created_at: at(-40, '10:30'), rejected_at: at(-40, '10:30') },
    { referrer: party('kanokwan'), friend: party('kanokwan'), code: code('kanokwan'), channel: 'register', status: 'rejected', reject_reason: 'self_referral', referrer_points: 0, friend_points: 0, over_monthly_cap: false, created_at: at(-130, '22:11'), rejected_at: at(-130, '22:11') },
  ]);
}

/** A label from the effective date, `2025-06`, never the legacy one. */
function labelOf(date: string): string {
  const label = date.slice(0, 7);
  return label === LEGACY_LABEL ? `${label}.1` : label;
}

function seedPrivacyPolicy(ownerStaff: string) {
  const sections = TEMPLATE_SECTIONS.map((section) => ({ ...section }));
  // The first notice predates the staff AI assistant; it had no section on it.
  const older = sections
    .filter((section) => section.anchor !== 'ai-assistants')
    .map((section) =>
      section.anchor === 'retention'
        ? { ...section, body: `${section.body}\n\nฉบับนี้ยังไม่รวมข้อมูลที่ระบบผู้ช่วย AI ใช้ ซึ่งคลินิกยังไม่ได้เปิดใช้` }
        : section,
    );

  const firstEffective = day(-485);
  const first = policies().insert({
    status: 'published',
    is_current: false,
    label: labelOf(firstEffective),
    effective_date: firstEffective,
    published_at: at(-490, '17:00'),
    published_by_staff_id: ownerStaff,
    legal_review_done: true,
    created_from: 'template',
    template_version: '2025-05-template-1',
    created_by_staff_id: ownerStaff,
    created_at: at(-500, '10:00'),
    updated_at: at(-490, '17:00'),
    version: 3,
    sections: older,
  } as Omit<PolicyRow, 'id'>);

  const currentEffective = day(-120);
  const current = policies().insert({
    status: 'published',
    is_current: true,
    label: labelOf(currentEffective),
    effective_date: currentEffective,
    published_at: at(-125, '16:30'),
    published_by_staff_id: ownerStaff,
    legal_review_done: true,
    created_from: 'template',
    template_version: TEMPLATE_VERSION,
    created_by_staff_id: ownerStaff,
    created_at: at(-131, '09:20'),
    updated_at: at(-125, '16:30'),
    version: 4,
    sections,
  } as Omit<PolicyRow, 'id'>);
  void first;

  policies().insert({
    status: 'draft',
    is_current: false,
    legal_review_done: false,
    created_from: 'version',
    based_on_version_id: current.id,
    created_by_staff_id: ownerStaff,
    created_at: at(-2, '14:05'),
    updated_at: at(-1, '09:40'),
    version: 2,
    sections: sections.map((section) =>
      section.anchor === 'sharing'
        ? { ...section, body: `${section.body}\n\n- ห้องปฏิบัติการภายนอกที่คลินิกส่งตัวอย่างไปตรวจ จะได้รับเฉพาะชื่อสัตว์ HN และข้อมูลที่จำเป็นต่อการตรวจ` }
        : section,
    ),
  } as Omit<PolicyRow, 'id'>);

  // Consents carry the label of the notice in force when they were given.
  for (const row of consents().where((entry) => entry.policy_version === 'pending')) {
    consents().update(row.id, { policy_version: row.recorded_at < current.published_at! ? first.label : current.label });
  }
}
