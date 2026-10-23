import { users } from '../../core/auth';
import { at, day, table, today, uuid } from '../../core/db';
import { DEMO_ACCOUNTS, DEMO_PASSWORD } from '../../core/demo-accounts';
import {
  addDays,
  clinicProfile,
  clinicSettings,
  openingHours,
  auditLogs,
  branches,
  clinicInstant,
  contacts,
  holidays,
  invitations,
  mondayOf,
  roles,
  services,
  shifts,
  staffMembers,
  STARTER_ROLES,
  timeOff,
  WEEKDAYS,
  weekdayOf,
  type PriceTier,
  type PublicProfile,
  type ServiceRow,
  type StaffRow,
} from './data';

/**
 * The clinic everything else hangs off: profile, two branches, hours, roles, the whole staff and
 * their roster, the service menu and the operating rules. Order 0, so every later seed can look
 * these rows up.
 */
export const order = 0;

/** Fixed so the guide can link the sample invitation: `/invitations/<id>#token=<token>`. */
export const SAMPLE_INVITATION_ID = 'sin_5f0c2a9e-7d41-4b8e-9a42-1d6e0b7c9a10';
export const SAMPLE_INVITATION_TOKEN = 'demo-invite-7d41';

export const BRANCH_SUKHUMVIT = 'brn_sukhumvit';
export const BRANCH_ARI = 'brn_ari';

/** The next date (from tomorrow on) that is month-day `md` ("12-05"). */
function nextDate(md: string): string {
  const year = Number(today().slice(0, 4));
  const thisYear = `${year}-${md}`;
  return thisYear > today() ? thisYear : `${year + 1}-${md}`;
}

function seedClinic() {
  clinicProfile().get();
  openingHours().get();

  branches().insertMany([
    {
      id: BRANCH_SUKHUMVIT,
      version: 1,
      code: 'sukhumvit',
      name: 'สาขาสุขุมวิท',
      address: '88/12 ถนนสุขุมวิท ซอย 39 แขวงคลองตันเหนือ เขตวัฒนา กรุงเทพฯ 10110',
      latitude: 13.7367,
      longitude: 100.5729,
      exam_rooms: ['ห้องตรวจ 1', 'ห้องตรวจ 2', 'ห้องตรวจ 3'],
    },
    {
      id: BRANCH_ARI,
      version: 1,
      code: 'ari',
      name: 'สาขาอารีย์',
      address: '19 ซอยพหลโยธิน 7 (อารีย์) แขวงพญาไท เขตพญาไท กรุงเทพฯ 10400',
      latitude: 13.7797,
      longitude: 100.5446,
      exam_rooms: ['ห้องตรวจ 1', 'ห้องตรวจ 2'],
    },
  ]);

  holidays().insertMany([
    { date: day(12), name: 'ปิดปรับปรุงห้องผ่าตัด', branch_id: BRANCH_ARI },
    { date: nextDate('12-05'), name: 'วันพ่อแห่งชาติ' },
    { date: nextDate('12-31'), name: 'วันสิ้นปี' },
    { date: nextDate('01-01'), name: 'วันขึ้นปีใหม่' },
  ]);

  contacts().insertMany([
    { version: 1, kind: 'phone', label: 'สาขาสุขุมวิท', value: '02-258-3939', branch_id: BRANCH_SUKHUMVIT, sort_order: 1 },
    { version: 1, kind: 'phone', label: 'สาขาอารีย์', value: '02-279-0707', branch_id: BRANCH_ARI, sort_order: 2 },
    { version: 1, kind: 'emergency', label: 'โทรได้ 24 ชั่วโมง ทุกวัน', value: '081-555-0199', sort_order: 3 },
    { version: 1, kind: 'line', label: 'LINE Official', value: '@happypawsclinic', url: 'https://line.me/R/ti/p/@happypawsclinic', sort_order: 4 },
    { version: 1, kind: 'email', label: 'อีเมล', value: 'hello@happypaws.co.th', sort_order: 5 },
    { version: 1, kind: 'facebook', label: 'Facebook', value: 'Happy Paws Clinic', url: 'https://www.facebook.com/happypawsclinic', sort_order: 6 },
    {
      version: 1,
      kind: 'map',
      label: 'แผนที่สาขาสุขุมวิท',
      value: 'สุขุมวิท 39 ใกล้ BTS พร้อมพงษ์',
      url: 'https://maps.google.com/?q=13.7367,100.5729',
      embed_url: 'https://maps.google.com/maps?q=13.7367,100.5729&z=16&output=embed',
      branch_id: BRANCH_SUKHUMVIT,
      sort_order: 7,
    },
    {
      version: 1,
      kind: 'map',
      label: 'แผนที่สาขาอารีย์',
      value: 'ซอยอารีย์ เดินจาก BTS อารีย์ 5 นาที',
      url: 'https://maps.google.com/?q=13.7797,100.5446',
      embed_url: 'https://maps.google.com/maps?q=13.7797,100.5446&z=16&output=embed',
      branch_id: BRANCH_ARI,
      sort_order: 8,
    },
  ]);

  clinicSettings().insertMany([
    { id: 'booking', version: 1, slot_minutes: 30, lead_time_minutes: 60, cancellation_cutoff_hours: 24, max_advance_days: 60, walk_in_slots_per_hour: 2 },
    { id: 'queue', version: 1, number_prefix: 'A', number_digits: 3, reset_daily: true },
    {
      id: 'medical_record',
      version: 1,
      required_fields: ['chief_complaint', 'assessment', 'plan'],
      examination_systems: ['ผิวหนังและขน', 'ตาและหู', 'ช่องปากและฟัน', 'ระบบหัวใจและหลอดเลือด', 'ระบบทางเดินหายใจ', 'ระบบทางเดินอาหาร', 'ระบบกล้ามเนื้อและกระดูก', 'ระบบประสาท', 'ระบบทางเดินปัสสาวะ', 'ต่อมน้ำเหลือง'],
      owner_visible_sections: ['vitals', 'diagnosis', 'treatment', 'prescription', 'lab_result'],
      signer_must_be_case_vet: true,
    },
    {
      id: 'loyalty',
      version: 1,
      satang_per_point: 2500,
      point_value_satang: 100,
      silver_min_points: 500,
      gold_min_points: 2000,
      points_expire_after_months: 24,
      referral_enabled: true,
      referral_referrer_points: 100,
      referral_friend_points: 50,
      referral_monthly_cap: 10,
      updated_at: at(-45, '10:00'),
    },
  ]);
}

function seedRoles() {
  for (const role of STARTER_ROLES) {
    roles().insert({ id: `rol_${role.name}`, version: 1, name: role.name, is_system: true, scopes: role.scopes, assignable: role.name !== 'customer' });
  }
  // One role of the clinic's own, so the roles card shows that roles are data.
  roles().insert({
    id: 'rol_head_nurse',
    version: 2,
    name: 'head_nurse',
    is_system: false,
    scopes: ['queue:read', 'queue:write', 'patient:read', 'vitals:write', 'record:read', 'inventory:read', 'schedule:read'],
    assignable: true,
  });
}

// ---- Staff -----------------------------------------------------------------------------------------

interface SampleStaff {
  key: string;
  name: string;
  type: string;
  department: string;
  role: string | null;
  license?: string;
  branches: string[];
  profile: PublicProfile;
  userId?: string;
  twoFactor?: boolean;
}

const profile = (job_title: string, specialty: string | undefined, bio: string | undefined, career_start_year: number, show = true): PublicProfile => ({
  job_title,
  ...(specialty ? { specialty } : {}),
  ...(bio ? { bio } : {}),
  career_start_year,
  show_on_website: show,
});

/** SampleStaff.cs, plus the owner's own membership. */
/** Exported for the drawn team picture on the team page's banner (demo/src/overrides). */
export const STAFF: SampleStaff[] = [
  {
    key: 'owner',
    name: DEMO_ACCOUNTS.owner.display_name,
    type: 'owner',
    department: 'management',
    role: 'clinic_owner',
    license: 'ส.พ. 03218',
    branches: [],
    userId: DEMO_ACCOUNTS.owner.user_id,
    twoFactor: true,
    profile: profile('ผู้ก่อตั้งและเจ้าของคลินิก', 'อายุรกรรมและเวชศาสตร์ป้องกัน', 'เปิด Happy Paws ด้วยความตั้งใจให้ทุกครอบครัวเข้าถึงการดูแลสัตว์เลี้ยงที่อธิบายได้และไว้ใจได้', 2004),
  },
  { key: 'kitti', twoFactor: true, name: 'น.สพ. กิตติ วรกุล', type: 'veterinarian', department: 'veterinary', role: 'veterinarian', license: 'ส.พ. 04412', branches: ['brn_sukhumvit'],
    profile: profile('ผู้อำนวยการคลินิก', 'ศัลยกรรมกระดูกและข้อ', 'ดูแลงานผ่าตัดกระดูกและข้อของคลินิกมาตั้งแต่วันแรก เชื่อว่าการอธิบายให้เจ้าของเข้าใจสำคัญพอ ๆ กับการรักษา', 2006) },
  { key: 'mint', name: DEMO_ACCOUNTS.vet.display_name, type: 'veterinarian', department: 'veterinary', role: 'veterinarian', license: 'ส.พ. 06120', branches: ['brn_sukhumvit'],
    userId: DEMO_ACCOUNTS.vet.user_id, twoFactor: true,
    profile: profile('หัวหน้าสัตวแพทย์', 'อายุรกรรมสัตว์เล็ก', 'ชอบไขปริศนาโรคเรื้อรังของน้องหมาน้องแมว และเขียนบทความสุขภาพให้เจ้าของอ่านเข้าใจง่าย', 2015) },
  { key: 'preeya', twoFactor: true, name: 'สพ.ญ. ปรียา แก้วมณี', type: 'veterinarian', department: 'veterinary', role: 'veterinarian', license: 'ส.พ. 07385', branches: ['brn_sukhumvit', 'brn_ari'],
    profile: profile('สัตวแพทย์ประจำ', 'ผิวหนังและภูมิแพ้', 'ดูแลน้องที่คัน ขนร่วง และแพ้อาหาร ใจเย็นเป็นพิเศษกับน้องที่กลัวหมอ', 2017) },
  { key: 'thanakorn', twoFactor: true, name: 'น.สพ. ธนกร ศักดิ์ดี', type: 'veterinarian', department: 'veterinary', role: 'veterinarian', license: 'ส.พ. 08127', branches: ['brn_ari'],
    profile: profile('สัตวแพทย์ประจำ', 'กระต่ายและสัตว์พิเศษ', 'รักษากระต่าย หนูแกสบี้ และนก ถ้าน้องตัวเล็กของคุณไม่ค่อยกินอาหาร พามาคุยกันได้', 2019) },
  { key: 'nicha', twoFactor: true, name: 'สพ.ญ. ณิชา พงษ์ไพบูลย์', type: 'veterinarian', department: 'veterinary', role: 'veterinarian', license: 'ส.พ. 08544', branches: ['brn_sukhumvit'],
    profile: profile('สัตวแพทย์ประจำ', 'ทันตกรรมสัตว์', 'ขูดหินปูน ถอนฟัน และสอนเจ้าของแปรงฟันน้องที่บ้าน เพราะฟันดีคือสุขภาพดี', 2020) },
  { key: 'kamonchanok', name: 'กมลชนก ใจดี', type: 'vet_assistant', department: 'nursing', role: 'head_nurse', branches: ['brn_sukhumvit'],
    profile: profile('หัวหน้าผู้ช่วยสัตวแพทย์', 'ดูแลสัตว์ป่วยใน', 'ดูแลน้องที่นอนโรงพยาบาลทั้งกลางวันและกลางคืน และโทรเล่าอาการให้เจ้าของฟังทุกวัน', 2012) },
  { key: 'orauma', name: 'อรอุมา บุญมา', type: 'vet_assistant', department: 'nursing', role: 'vet_assistant', branches: ['brn_sukhumvit'],
    profile: profile('ผู้ช่วยสัตวแพทย์ห้องผ่าตัด', 'เตรียมผ่าตัดและดูแลหลังผ่าตัด', 'เตรียมห้องผ่าตัดและอยู่ข้างน้องจนตื่นจากยาสลบ', 2018) },
  { key: 'worawut', name: 'วรวุฒิ แสงทอง', type: 'vet_assistant', department: 'nursing', role: 'vet_assistant', branches: ['brn_ari'],
    profile: profile('ผู้ช่วยสัตวแพทย์', 'เจาะเลือดและงานห้องแล็บ', 'มือเบาเวลาเจาะเลือด และรู้ว่าขนมชิ้นไหนช่วยให้น้องหายกลัว', 2021) },
  { key: 'siriporn', name: DEMO_ACCOUNTS.reception.display_name, type: 'receptionist', department: 'front_desk', role: 'receptionist', branches: ['brn_sukhumvit'],
    userId: DEMO_ACCOUNTS.reception.user_id,
    profile: profile('หัวหน้าเคาน์เตอร์ต้อนรับ', 'นัดหมายและติดตามวัคซีน', 'จำชื่อน้องได้แทบทุกตัว และเป็นคนโทรเตือนเมื่อถึงเวลาฉีดวัคซีนเข็มต่อไป', 2016) },
  { key: 'panupong', name: 'ภานุพงศ์ ทองดี', type: 'receptionist', department: 'front_desk', role: 'receptionist', branches: ['brn_ari'],
    profile: profile('เจ้าหน้าที่ต้อนรับ', undefined, undefined, 2022, false) },
  { key: 'janpen', twoFactor: true, name: 'จันทร์เพ็ญ รุ่งเรือง', type: 'cashier', department: 'finance', role: 'cashier', branches: ['brn_sukhumvit'],
    profile: profile('เจ้าหน้าที่การเงิน', 'ใบเสร็จและเคลมประกัน', 'ช่วยอธิบายค่าใช้จ่ายก่อนเริ่มรักษา และเตรียมเอกสารเคลมประกันให้ครบ', 2014) },
  { key: 'nattawut', twoFactor: true, name: 'ณัฐวุฒิ ปัญญาดี', type: 'clinic_admin', department: 'management', role: 'clinic_admin', branches: [],
    profile: profile('ผู้จัดการคลินิก', 'งานบริหารและคุณภาพบริการ', 'ดูแลให้ทุกการมาคลินิกเป็นไปอย่างราบรื่น ตั้งแต่ที่จอดรถจนถึงการติดตามหลังกลับบ้าน', 2010) },
  { key: 'somchai', name: 'สมชาย ขยันงาน', type: 'housekeeping', department: 'support', role: null, branches: ['brn_sukhumvit'],
    profile: profile('เจ้าหน้าที่ความสะอาดและฆ่าเชื้อ', 'ห้องผ่าตัดและกรงพักฟื้น', 'ทำให้ทุกห้องสะอาดและปลอดเชื้อก่อนน้องตัวต่อไปจะเข้ามา', 2017) },
  { key: 'piyanuch', name: 'ปิยนุช ศรีงาม', type: 'animal_caretaker', department: 'support', role: null, branches: ['brn_sukhumvit'],
    profile: profile('ผู้ดูแลสัตว์พักฟื้นและฝากเลี้ยง', 'ฝากเลี้ยงและพาเดิน', 'ให้อาหาร พาเดินเล่น และส่งรูปน้องให้เจ้าของดูระหว่างฝากเลี้ยง', 2020) },
];

/** Staff ids by sample key, for the roster and the services below. Filled by `seedStaff`. */
const ids: Record<string, string> = {};

function seedStaff() {
  STAFF.forEach((member, index) => {
    const email = member.key === 'owner' ? DEMO_ACCOUNTS.owner.email! : `${member.key}@staff.happypaws.test`;
    let userId = member.userId;
    if (!userId) {
      userId = users().insert({
        display_name: member.name,
        email,
        phone: null,
        password: DEMO_PASSWORD,
        status: 'active',
        two_factor: member.twoFactor ? { enabled: true, method: 'totp', recovery_codes_left: 10 } : { enabled: false, method: 'none' },
        language: 'th',
        notifications: { email: true, sms: false, line: false },
        version: 1,
        created_at: at(-300 + index * 9, '10:00'),
      }).id;
    }
    const row = staffMembers().insert({
      version: 1,
      user_id: userId,
      display_name: member.name,
      email,
      staff_type: member.type,
      ...(member.license ? { license_no: member.license } : {}),
      roles: member.role ? [member.role] : [],
      branches: member.branches,
      status: 'active',
      two_factor_enabled: Boolean(member.twoFactor),
      created_at: at(-300 + index * 9, '10:00'),
      department: member.department,
      public_profile: member.profile,
    } satisfies Omit<StaffRow, 'id'>);
    ids[member.key] = row.id;
  });

  // Someone who has left: their name stays on old records, they can no longer sign in.
  const leaverUser = users().insert({
    display_name: 'อนุชา มั่นคง',
    email: 'anucha@staff.happypaws.test',
    phone: null,
    password: DEMO_PASSWORD,
    status: 'deactivated',
    two_factor: { enabled: false, method: 'none' },
    language: 'th',
    version: 2,
    created_at: at(-380, '10:00'),
  });
  staffMembers().insert({
    version: 3,
    user_id: leaverUser.id,
    display_name: 'อนุชา มั่นคง',
    email: 'anucha@staff.happypaws.test',
    staff_type: 'receptionist',
    roles: ['receptionist'],
    branches: [BRANCH_ARI],
    status: 'deactivated',
    two_factor_enabled: false,
    created_at: at(-380, '10:00'),
    department: 'front_desk',
    public_profile: profile('เจ้าหน้าที่ต้อนรับ', undefined, undefined, 2021, false),
  });

  invitations().insert({
    id: SAMPLE_INVITATION_ID,
    version: 1,
    display_name: 'ภัทรา สายใจ',
    email: 'pattra@staff.happypaws.test',
    staff_type: 'vet_assistant',
    roles: ['vet_assistant'],
    branches: [BRANCH_ARI],
    status: 'invited',
    two_factor_enabled: false,
    created_at: at(-2, '14:20'),
    invitation_expires_at: at(5, '14:20'),
    department: 'nursing',
    token: SAMPLE_INVITATION_TOKEN,
  });
}

// ---- Roster ----------------------------------------------------------------------------------------

type Block = [string, string];
const FULL: Block[] = [['09:00', '12:00'], ['13:00', '18:00']];
const MORNING: Block[] = [['09:00', '15:00']];
const AFTERNOON: Block[] = [['14:00', '20:00']];

/** Per person: weekday (0 = Monday) → branch and blocks. A day missing is a day off. */
const ROSTER: Record<string, Record<number, [string, Block[]]>> = {
  kitti: { 0: [BRANCH_SUKHUMVIT, FULL], 1: [BRANCH_SUKHUMVIT, FULL], 2: [BRANCH_SUKHUMVIT, FULL], 3: [BRANCH_SUKHUMVIT, FULL], 4: [BRANCH_SUKHUMVIT, FULL] },
  mint: { 0: [BRANCH_SUKHUMVIT, AFTERNOON], 1: [BRANCH_SUKHUMVIT, AFTERNOON], 3: [BRANCH_SUKHUMVIT, AFTERNOON], 4: [BRANCH_SUKHUMVIT, AFTERNOON], 5: [BRANCH_SUKHUMVIT, AFTERNOON], 6: [BRANCH_SUKHUMVIT, MORNING] },
  nicha: { 2: [BRANCH_SUKHUMVIT, AFTERNOON], 3: [BRANCH_SUKHUMVIT, MORNING], 4: [BRANCH_SUKHUMVIT, MORNING], 5: [BRANCH_SUKHUMVIT, MORNING], 6: [BRANCH_SUKHUMVIT, AFTERNOON] },
  preeya: { 0: [BRANCH_SUKHUMVIT, MORNING], 1: [BRANCH_SUKHUMVIT, MORNING], 3: [BRANCH_ARI, AFTERNOON], 4: [BRANCH_ARI, AFTERNOON], 5: [BRANCH_ARI, FULL], 6: [BRANCH_ARI, AFTERNOON] },
  thanakorn: { 0: [BRANCH_ARI, FULL], 1: [BRANCH_ARI, FULL], 2: [BRANCH_ARI, FULL], 3: [BRANCH_ARI, MORNING], 4: [BRANCH_ARI, MORNING], 6: [BRANCH_ARI, MORNING] },
  kamonchanok: { 0: [BRANCH_SUKHUMVIT, FULL], 1: [BRANCH_SUKHUMVIT, FULL], 2: [BRANCH_SUKHUMVIT, FULL], 3: [BRANCH_SUKHUMVIT, FULL], 4: [BRANCH_SUKHUMVIT, FULL] },
  orauma: { 2: [BRANCH_SUKHUMVIT, AFTERNOON], 3: [BRANCH_SUKHUMVIT, AFTERNOON], 4: [BRANCH_SUKHUMVIT, AFTERNOON], 5: [BRANCH_SUKHUMVIT, FULL], 6: [BRANCH_SUKHUMVIT, FULL] },
  worawut: { 0: [BRANCH_ARI, FULL], 1: [BRANCH_ARI, FULL], 2: [BRANCH_ARI, FULL], 3: [BRANCH_ARI, FULL], 5: [BRANCH_ARI, FULL], 6: [BRANCH_ARI, MORNING] },
  siriporn: { 0: [BRANCH_SUKHUMVIT, FULL], 1: [BRANCH_SUKHUMVIT, FULL], 2: [BRANCH_SUKHUMVIT, FULL], 3: [BRANCH_SUKHUMVIT, FULL], 4: [BRANCH_SUKHUMVIT, FULL], 5: [BRANCH_SUKHUMVIT, MORNING] },
  panupong: { 1: [BRANCH_ARI, FULL], 2: [BRANCH_ARI, FULL], 3: [BRANCH_ARI, FULL], 4: [BRANCH_ARI, FULL], 5: [BRANCH_ARI, FULL], 6: [BRANCH_ARI, FULL] },
  janpen: { 0: [BRANCH_SUKHUMVIT, FULL], 1: [BRANCH_SUKHUMVIT, FULL], 2: [BRANCH_SUKHUMVIT, FULL], 3: [BRANCH_SUKHUMVIT, FULL], 4: [BRANCH_SUKHUMVIT, FULL], 5: [BRANCH_SUKHUMVIT, FULL] },
};

function seedRoster() {
  const first = addDays(mondayOf(today()), -7);
  const last = addDays(today(), 15);
  const leaveDay = addDays(mondayOf(today()), 8); // next Tuesday
  const rows = [];

  for (let date = first; date <= last; date = addDays(date, 1)) {
    const weekday = WEEKDAYS.indexOf(weekdayOf(date));
    for (const [key, week] of Object.entries(ROSTER)) {
      const entry = week[weekday];
      if (!entry) continue;
      if (key === 'preeya' && date === leaveDay) continue;
      const [branchId, blocks] = entry;
      for (const [from, to] of blocks) {
        rows.push({ id: uuid(), staff_id: ids[key]!, branch_id: branchId, starts_at: clinicInstant(date, from), ends_at: clinicInstant(date, to), version: 1 });
      }
    }
  }
  shifts().insertMany(rows);

  timeOff().insertMany([
    { staff_id: ids.preeya!, starts_at: clinicInstant(leaveDay, '00:00'), ends_at: clinicInstant(addDays(leaveDay, 1), '00:00'), reason: 'ลาพักร้อน' },
    { staff_id: ids.kamonchanok!, starts_at: clinicInstant(addDays(mondayOf(today()), 11), '13:00'), ends_at: clinicInstant(addDays(mondayOf(today()), 11), '18:00'), reason: 'อบรมการพยาบาลสัตว์ฉุกเฉิน' },
  ]);
  shifts().removeWhere((shift) => shift.staff_id === ids.kamonchanok && shift.starts_at === clinicInstant(addDays(mondayOf(today()), 11), '13:00'));

  table('assignment_rules').insert({
    id: 'singleton',
    version: 1,
    rules: [
      { species: 'กระต่าย', service_id: null, staff_ids: [ids.thanakorn!] },
      { species: null, service_id: '$dental', staff_ids: [ids.nicha!] },
      { species: null, service_id: '$skin', staff_ids: [ids.preeya!, ids.mint!] },
    ],
  });
}

// ---- Services --------------------------------------------------------------------------------------

const baht = (value: number) => value * 100;
const byWeight = (...prices: number[]): PriceTier[] => {
  const bands = [[0, 10], [10, 25], [25, undefined]] as const;
  return prices.map((price, index) => ({ min_weight_kg: bands[index]![0], ...(bands[index]![1] ? { max_weight_kg: bands[index]![1] } : {}), price_satang: baht(price) }));
};
const bySpecies = (dog: number, cat: number): PriceTier[] => [
  { species: 'สุนัข', price_satang: baht(dog) },
  { species: 'แมว', price_satang: baht(cat) },
];

function service(code: string, name: string, category: string, minutes: number, price: number, extra: Partial<ServiceRow> = {}): Omit<ServiceRow, 'id'> {
  return {
    version: 1,
    code,
    name,
    category_code: category,
    duration_minutes: minutes,
    price_satang: baht(price),
    online_booking_enabled: true,
    is_active: true,
    eligible_staff_ids: [],
    branch_ids: [],
    booking_windows: [],
    pricing: { base_price_satang: baht(price), tiers: extra.pricing?.tiers ?? [] },
    upcoming_pricing: [],
    ...extra,
    ...(extra.pricing ? { pricing: { ...extra.pricing, base_price_satang: baht(price) } } : {}),
  };
}

const tiers = (list: PriceTier[]) => ({ pricing: { base_price_satang: 0, tiers: list } });
const daily = (from: string, to: string) => WEEKDAYS.map((weekday) => ({ day: weekday, from, to }));

function seedServices() {
  const rows = services().insertMany([
    service('GEN-CHECK', 'ตรวจสุขภาพทั่วไป', 'checkup', 30, 500, { description: 'ตรวจร่างกายครบทุกระบบ ชั่งน้ำหนัก วัดไข้ และปรึกษาเรื่องอาหารกับการดูแลที่บ้าน' }),
    service('ANNUAL', 'ตรวจสุขภาพประจำปี', 'checkup', 45, 1800, { ...tiers(bySpecies(1800, 1600)), description: 'ตรวจร่างกาย ตรวจเลือดพื้นฐาน และสรุปผลพร้อมคำแนะนำเป็นรายลักษณ์อักษร' }),
    service('SKIN', 'ตรวจโรคผิวหนังและภูมิแพ้', 'checkup', 30, 600, { eligible_staff_ids: [ids.preeya!], description: 'ขูดตรวจผิวหนัง ส่องเชื้อรา และวางแผนคุมอาการคันระยะยาว' }),
    service('EXOTIC', 'ตรวจกระต่ายและสัตว์พิเศษ', 'checkup', 30, 600, { eligible_staff_ids: [ids.thanakorn!], branch_ids: [BRANCH_ARI], description: 'สำหรับกระต่าย หนูแกสบี้ แฮมสเตอร์ และนก โดยสัตวแพทย์เฉพาะทาง' }),
    service('VAC-DOG', 'วัคซีนรวมสุนัข (DHPPi+L)', 'vaccine', 15, 650, { description: 'ป้องกันไข้หัด ลำไส้อักเสบ ตับอักเสบ หวัด และฉี่หนู พร้อมสมุดวัคซีน' }),
    service('VAC-CAT', 'วัคซีนรวมแมว (FVRCP)', 'vaccine', 15, 600, { description: 'ป้องกันไข้หัดแมว หวัดแมว และโรคทางเดินหายใจ' }),
    service('VAC-RAB', 'วัคซีนพิษสุนัขบ้า', 'vaccine', 15, 350, {
      upcoming_pricing: [{ effective_from: day(30), base_price_satang: baht(380), tiers: [] }],
      description: 'ฉีดได้ทั้งสุนัขและแมว แนะนำกระตุ้นทุกปี',
    }),
    service('VAC-FELV', 'วัคซีนลิวคีเมียแมว (FeLV)', 'vaccine', 15, 850, { description: 'สำหรับแมวที่ออกนอกบ้านหรืออยู่ร่วมกับแมวหลายตัว ตรวจเลือดก่อนฉีด' }),
    service('DEN-SCALE', 'ขูดหินปูนและขัดฟัน', 'dental', 60, 2500, {
      ...tiers(byWeight(2500, 3200, 4000)),
      eligible_staff_ids: [ids.nicha!],
      branch_ids: [BRANCH_SUKHUMVIT],
      booking_windows: [{ day: 'thursday', from: '09:00', to: '12:00' }, { day: 'saturday', from: '09:00', to: '12:00' }],
      description: 'ภายใต้ยาสลบ รวมตรวจเลือดก่อนวางยา ต้องงดอาหาร 8 ชั่วโมง',
    }),
    service('DEN-EXTRACT', 'ถอนฟัน', 'dental', 60, 1500, { online_booking_enabled: false, eligible_staff_ids: [ids.nicha!] }),
    service('SUR-NEUTER-DOG', 'ทำหมันสุนัขเพศผู้', 'surgery', 90, 3000, {
      ...tiers(byWeight(3000, 4500, 6000)),
      eligible_staff_ids: [ids.kitti!],
      branch_ids: [BRANCH_SUKHUMVIT],
      description: 'รวมยาสลบ ยาแก้ปวด และนัดตัดไหมหลังผ่าตัด 10 วัน',
    }),
    service('SUR-SPAY-CAT', 'ทำหมันแมวเพศเมีย', 'surgery', 90, 2500, { eligible_staff_ids: [ids.kitti!], branch_ids: [BRANCH_SUKHUMVIT], description: 'รวมยาสลบ ปลอกคอกันเลีย และยากลับบ้าน' }),
    service('SUR-ORTHO', 'ผ่าตัดกระดูกและข้อ (ประเมินก่อนผ่าตัด)', 'surgery', 120, 15000, { online_booking_enabled: false, eligible_staff_ids: [ids.kitti!] }),
    service('LAB-CBC', 'ตรวจความสมบูรณ์ของเม็ดเลือด (CBC)', 'lab', 20, 450, { description: 'รู้ผลภายใน 30 นาทีจากเครื่องตรวจในคลินิก' }),
    service('LAB-CHEM', 'ตรวจค่าตับและไต', 'lab', 20, 900, { description: 'ALT, ALP, BUN, Creatinine เหมาะสำหรับสัตว์สูงวัยหรือก่อนวางยาสลบ' }),
    service('LAB-XRAY', 'เอกซเรย์ดิจิทัล', 'lab', 30, 800, { online_booking_enabled: false, branch_ids: [BRANCH_SUKHUMVIT] }),
    service('LAB-FECAL', 'ตรวจอุจจาระหาพยาธิ', 'lab', 15, 250, { is_active: false, online_booking_enabled: false }),
    service('GRM-BATH', 'อาบน้ำ ตัดเล็บ เช็ดหู', 'grooming', 60, 350, {
      ...tiers(byWeight(350, 500, 700)),
      booking_windows: daily('10:00', '17:00'),
      description: 'แชมพูอ่อนโยนสำหรับผิวแพ้ง่าย พร้อมเป่าขนและแปรงขน',
    }),
    service('GRM-FULL', 'ตัดแต่งขนทั้งตัว', 'grooming', 120, 600, { ...tiers(byWeight(600, 800, 1100)), booking_windows: daily('10:00', '16:00'), description: 'รวมอาบน้ำ ตัดเล็บ และตัดขนตามทรงที่เจ้าของเลือก' }),
    service('GRM-CAT', 'อาบน้ำแมว', 'grooming', 60, 500, { branch_ids: [BRANCH_SUKHUMVIT], booking_windows: daily('10:00', '16:00'), description: 'ห้องแยกสำหรับแมวโดยเฉพาะ เงียบและไม่มีเสียงสุนัข' }),
  ]);

  // The assignment rules name services by placeholder until the services exist.
  const byCode = Object.fromEntries(rows.map((row) => [row.code, row.id]));
  const rules = table('assignment_rules').get('singleton');
  if (rules) {
    table('assignment_rules').put({
      ...rules,
      rules: (rules.rules as Array<{ service_id: string | null }>).map((rule) => ({
        ...rule,
        service_id: rule.service_id === '$dental' ? byCode['DEN-SCALE'] : rule.service_id === '$skin' ? byCode.SKIN : rule.service_id,
      })),
    });
  }
}

function seedAudit() {
  const actor = (key: string) => ({ actor_id: ids[key], actor_name: STAFF.find((member) => member.key === key)!.name });
  auditLogs().insertMany([
    { occurred_at: at(-20, '09:12'), ...actor('owner'), action: 'role.permissions_replaced', resource_type: 'role', resource_id: 'rol_head_nurse', summary: 'แก้สิทธิ์ของบทบาท head_nurse', ip: '203.150.12.44' },
    { occurred_at: at(-9, '16:40'), ...actor('nattawut'), action: 'service.pricing_updated', resource_type: 'service', summary: 'ตั้งราคาใหม่ของวัคซีนพิษสุนัขบ้า', ip: '203.150.12.51' },
    { occurred_at: at(-6, '11:05'), ...actor('owner'), action: 'staff.deactivated', resource_type: 'staff_member', summary: 'ปิดบัญชีของ อนุชา มั่นคง', ip: '203.150.12.44' },
    { occurred_at: at(-2, '14:20'), ...actor('nattawut'), action: 'staff.invited', resource_type: 'staff_invitation', resource_id: SAMPLE_INVITATION_ID, summary: 'เชิญ ภัทรา สายใจ (pattra@staff.happypaws.test)', ip: '203.150.12.51' },
    { occurred_at: at(-1, '18:30'), ...actor('owner'), action: 'clinic.hours_replaced', resource_type: 'clinic', summary: 'แก้เวลาทำการ', ip: '203.150.12.44' },
  ]);
}

export function seed() {
  seedClinic();
  seedRoles();
  seedStaff();
  seedRoster();
  seedServices();
  seedAudit();
}
