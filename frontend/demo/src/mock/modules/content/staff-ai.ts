import { day, nowIso, singleton, table, type Row } from '../../core/db';
import { invalid, notFound, requireStaff, route, type Auth, type Ctx } from '../../core/http';
import { publishedReviews, reviewSummary } from './reviews';
import { baht, callRoute, rowsOf, rowsOfTable } from './peer';

/**
 * The staff AI assistant (docs/contract-extensions.md § 34): ask the numbers, a patient summary
 * with citations, a SOAP draft from short notes, aftercare and reply drafts, and the morning
 * brief. The demo has no model. Every answer is put together from the demo's own rows, in the
 * same shapes, so what it says is true of the data on screen, and each one says it is a demo answer.
 */

const NOTE = '(คำตอบตัวอย่างของเดโม สรุปจากข้อมูลในระบบโดยไม่ได้ใช้ AI จริง)';
const SCOPE = 'ai:use';

type Feature = 'pet_summary' | 'soap_draft' | 'owner_message' | 'ask' | 'daily_brief';

interface Usage {
  month: string;
  limit: number;
  by_feature: Record<Feature, number>;
}

const thisMonth = () => day(0).slice(0, 7);

/** A month already part-used, so the meter looks like a clinic's. */
const usage = singleton<Usage>('staff_ai_usage', () => ({
  month: thisMonth(),
  limit: 500,
  by_feature: { pet_summary: 41, soap_draft: 27, owner_message: 18, ask: 33, daily_brief: new Date().getDate() },
}));

function count(feature: Feature): void {
  const current = usage.get();
  const fresh = current.month === thisMonth() ? current.by_feature : { pet_summary: 0, soap_draft: 0, owner_message: 0, ask: 0, daily_brief: 0 };
  usage.set({ month: thisMonth(), by_feature: { ...fresh, [feature]: (fresh[feature] ?? 0) + 1 } });
}

route('GET', '/staff/ai/usage', (ctx) => {
  requireStaff(ctx, SCOPE);
  const current = usage.get();
  const by_feature = current.month === thisMonth() ? current.by_feature : { pet_summary: 0, soap_draft: 0, owner_message: 0, ask: 0, daily_brief: 0 };
  return { enabled: true, month: thisMonth(), limit: current.limit, used: Object.values(by_feature).reduce((a, b) => a + b, 0), by_feature };
});

// ---- Helpers -------------------------------------------------------------------------------------

const has = (text: string, words: string[]) => words.some((word) => text.includes(word));

function localDay(iso: string | undefined): string {
  if (!iso) return '';
  const date = new Date(iso);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

const dateTh = (iso: string) => new Date(iso.length === 10 ? `${iso}T12:00:00` : iso).toLocaleDateString('th-TH', { day: 'numeric', month: 'short', year: '2-digit' });
const num = (value: number) => value.toLocaleString('th-TH');

function ageText(birth?: string): string | undefined {
  if (!birth) return undefined;
  const months = Math.floor((Date.now() - new Date(birth).getTime()) / (30.44 * 86_400_000));
  if (months < 12) return `${Math.max(months, 1)} เดือน`;
  return months % 12 ? `${Math.floor(months / 12)} ปี ${months % 12} เดือน` : `${Math.floor(months / 12)} ปี`;
}

const SEX: Record<string, string> = { male: 'เพศผู้', female: 'เพศเมีย' };
const SEVERITY: Record<string, string> = { mild: 'เล็กน้อย', moderate: 'ปานกลาง', severe: 'รุนแรง' };

/** The pet the question names, the longest name first so "ส้มโอ" is not read as "ส้ม". */
function petNamedIn(question: string): Row | undefined {
  return rowsOfTable('pets')
    .filter((pet) => !pet.is_deceased && pet.name && question.includes(pet.name))
    .sort((a, b) => b.name.length - a.name.length)[0];
}

// ---- Numbers -------------------------------------------------------------------------------------

interface Today {
  appointments: { total: number; upcoming: number; arrived: number; completed: number; cancelled: number; no_show: number; walk_in: number; online: number };
  queue: { issued: number; waiting: number; served: number; average_wait_minutes?: number };
}

/** Today's appointments and queue, from the dashboard's own answer when it is there, else from the rows. */
async function todayNumbers(auth: Auth, branchId?: string): Promise<Today> {
  const date = day(0);
  const appointments = rowsOfTable('appointments').filter((row) => localDay(row.starts_at) === date && (!branchId || row.branch_id === branchId));
  const tickets = rowsOfTable('queue_tickets').filter((row) => row.service_date === date && (!branchId || row.branch_id === branchId));
  const waits = tickets.filter((row) => row.called_at).map((row) => (Date.parse(row.called_at) - Date.parse(row.issued_at)) / 60_000);
  const own = {
    appointments: {
      total: appointments.length,
      upcoming: appointments.filter((row) => ['booked', 'confirmed'].includes(row.status)).length,
      arrived: appointments.filter((row) => ['checked_in', 'in_consultation'].includes(row.status)).length,
      completed: appointments.filter((row) => row.status === 'completed').length,
      cancelled: appointments.filter((row) => row.status === 'cancelled').length,
      no_show: appointments.filter((row) => row.status === 'no_show').length,
      walk_in: appointments.filter((row) => row.walk_in).length,
      online: appointments.filter((row) => !row.walk_in && (row.source === 'online' || row.booked_by === 'customer' || row.channel === 'online')).length,
    },
    queue: {
      issued: tickets.length,
      waiting: tickets.filter((row) => ['waiting', 'called'].includes(row.status)).length,
      served: tickets.filter((row) => row.status === 'served').length,
      average_wait_minutes: waits.length ? Math.round(waits.reduce((a, b) => a + b, 0) / waits.length) : undefined,
    },
  };

  const dashboard = await callRoute<any>('GET', '/staff/dashboard/today', { auth, query: { branch: branchId } });
  if (dashboard?.appointments) {
    own.appointments = { ...own.appointments, ...dashboard.appointments };
    own.queue = { ...own.queue, ...dashboard.queue };
  }
  return own;
}

interface StockAlert {
  kind: string;
  item_name: string;
  available?: number;
  reorder_level?: number;
  unit?: string;
  expires_on?: string;
}

async function stockAlerts(auth: Auth, branchId?: string): Promise<StockAlert[]> {
  const answer = await callRoute('GET', '/staff/inventory/alerts', { auth, query: { branch: branchId } });
  const alerts = rowsOf<StockAlert>(answer);
  if (alerts.length) return alerts;
  return rowsOfTable('inventory_items')
    .filter((item) => typeof item.available === 'number' && typeof item.reorder_level === 'number' && item.available <= item.reorder_level)
    .map((item) => ({ kind: item.available <= 0 ? 'out_of_stock' : 'low_stock', item_name: item.name, available: item.available, reorder_level: item.reorder_level, unit: item.unit }));
}

/** Money collected between two local days, from the invoices' payments. */
function collected(from: string, to: string): { collected: number; refunded: number; payments: number } {
  let total = 0;
  let refunded = 0;
  let payments = 0;
  for (const invoice of rowsOfTable('invoices')) {
    for (const payment of invoice.payments ?? []) {
      const on = localDay(payment.collected_at);
      if (on >= from && on <= to) {
        total += payment.amount_satang ?? 0;
        payments += 1;
      }
    }
    for (const refund of invoice.refunds ?? []) {
      const on = localDay(refund.refunded_at ?? refund.created_at);
      if (on >= from && on <= to) refunded += refund.amount_satang ?? 0;
    }
  }
  return { collected: total, refunded, payments };
}

/** Services by what they billed this month, from the analytics answer or the invoice lines. */
async function topServices(auth: Auth): Promise<Array<{ name: string; quantity: number; revenue: number }>> {
  const from = `${thisMonth()}-01`;
  const answer = await callRoute<any>('GET', '/staff/analytics/services', { auth, query: { from, to: day(0) } });
  if (answer?.services?.length) {
    return answer.services
      .map((row: any) => ({ name: row.name, quantity: row.quantity, revenue: row.revenue_satang ?? row.billed_satang ?? 0 }))
      .sort((a: any, b: any) => b.revenue - a.revenue);
  }
  const totals = new Map<string, { name: string; quantity: number; revenue: number }>();
  for (const invoice of rowsOfTable('invoices')) {
    if (invoice.status === 'void' || invoice.voided_at || localDay(invoice.issued_at ?? invoice.created_at) < from) continue;
    for (const line of invoice.lines ?? []) {
      if (line.kind && line.kind !== 'service') continue;
      const name = line.description ?? line.name;
      if (!name) continue;
      const entry = totals.get(name) ?? { name, quantity: 0, revenue: 0 };
      entry.quantity += line.quantity ?? 1;
      entry.revenue += line.line_total_satang ?? line.total_satang ?? 0;
      totals.set(name, entry);
    }
  }
  return [...totals.values()].sort((a, b) => b.revenue - a.revenue);
}

// ---- Pet summary ---------------------------------------------------------------------------------

interface Citation {
  ref: string;
  kind: 'allergy' | 'visit' | 'medicine' | 'weight' | 'lab' | 'vaccination';
  id: string;
  date: string;
}

export function summarisePet(pet: Row): { text: string; citations: Citation[] } {
  const citations: Citation[] = [];
  const cite = (kind: Citation['kind'], id: string, date: string) => {
    const ref = `R${citations.length + 1}`;
    citations.push({ ref, kind, id, date: localDay(date) || date });
    return `[${ref}]`;
  };
  const lines: string[] = [];

  const facts = [
    pet.breed ? `${pet.species}พันธุ์${pet.breed}` : pet.species,
    SEX[pet.sex],
    pet.neutered === true ? 'ทำหมันแล้ว' : pet.neutered === false ? 'ยังไม่ทำหมัน' : undefined,
    ageText(pet.birth_date) ? `อายุ ${ageText(pet.birth_date)}${pet.birth_date_estimated ? ' (ประมาณ)' : ''}` : undefined,
  ].filter(Boolean);
  const weights = rowsOfTable('pet_weights')
    .filter((row) => row.pet_id === pet.id)
    .sort((a, b) => b.recorded_at.localeCompare(a.recorded_at));
  const latest = weights[0];
  let weightText = '';
  if (latest) {
    const marker = latest.consultation_id ? ` ${cite('weight', latest.consultation_id, latest.recorded_at)}` : '';
    const before = weights.find((row) => row.recorded_at < latest.recorded_at && Date.parse(latest.recorded_at) - Date.parse(row.recorded_at) > 60 * 86_400_000);
    const trend = before ? `, ${before.weight_kg < latest.weight_kg ? 'เพิ่มขึ้น' : before.weight_kg > latest.weight_kg ? 'ลดลง' : 'คงที่'}จาก ${before.weight_kg} กก. เมื่อ ${dateTh(before.recorded_at)}` : '';
    weightText = ` น้ำหนักล่าสุด ${latest.weight_kg} กก. (${dateTh(latest.recorded_at)})${trend}${marker}`;
  }
  lines.push(`${pet.name} ${facts.join(' ')}${weightText}`.trim() + '.');

  const allergies = rowsOfTable('pet_allergies').filter((row) => row.pet_id === pet.id);
  if (allergies.length) {
    lines.push(
      `ข้อควรระวัง: แพ้ ${allergies
        .map((row) => `${row.substance} (${SEVERITY[row.severity] ?? row.severity}${row.reaction ? `: ${row.reaction}` : ''}) ${cite('allergy', row.id, row.recorded_at ?? pet.created_at ?? nowIso())}`)
        .join(', ')}`,
    );
  } else {
    lines.push('ไม่มีประวัติแพ้ยาหรืออาหารในระบบ');
  }
  if (pet.behaviour) lines.push(`หมายเหตุพฤติกรรม: ${pet.behaviour}`);

  const visits = rowsOfTable('consultations')
    .filter((row) => row.pet_id === pet.id)
    .sort((a, b) => (b.opened_at ?? '').localeCompare(a.opened_at ?? ''))
    .slice(0, 3);
  for (const visit of visits) {
    const diagnosis = (visit.diagnoses ?? []).find((row: any) => row.is_primary)?.description ?? visit.assessment;
    lines.push(
      `${dateTh(visit.opened_at)} มาด้วย${visit.chief_complaint ? ` "${visit.chief_complaint}"` : 'การตรวจ'}${diagnosis ? ` ผลประเมิน: ${diagnosis}` : ''}${visit.status === 'draft' ? ' (เคสยังเปิดอยู่)' : ''} ${cite('visit', visit.id, visit.opened_at)}`,
    );
  }
  if (!visits.length) lines.push('ยังไม่มีประวัติการตรวจในระบบ');

  const medicines = rowsOfTable('prescriptions')
    .filter((row) => row.pet_id === pet.id)
    .sort((a, b) => (b.prescribed_at ?? '').localeCompare(a.prescribed_at ?? ''))
    .slice(0, 2);
  if (medicines.length) {
    lines.push(`ยาล่าสุด: ${medicines.map((row) => `${row.drug_name} ${row.dose ?? ''} ${row.frequency ?? ''}`.replace(/\s+/g, ' ').trim() + ` ${cite('medicine', row.id, row.prescribed_at)}`).join(', ')}`);
  }

  const labs = rowsOfTable('lab_requests')
    .filter((row) => row.pet_id === pet.id)
    .sort((a, b) => (b.requested_at ?? '').localeCompare(a.requested_at ?? ''))
    .slice(0, 1);
  for (const lab of labs) {
    const tests = (lab.tests ?? []).map((test: any) => test.name ?? test.code).filter(Boolean).slice(0, 3).join(', ');
    lines.push(`แล็บล่าสุด ${dateTh(lab.requested_at)}${tests ? `: ${tests}` : ''}${lab.abnormal_count ? ` ผิดปกติ ${lab.abnormal_count} ค่า` : ''} ${cite('lab', lab.id, lab.requested_at)}`);
  }

  const doses = rowsOfTable('pet_vaccinations')
    .filter((row) => row.pet_id === pet.id)
    .sort((a, b) => b.administered_at.localeCompare(a.administered_at));
  // What is due next: each vaccine's latest dose only, since an older dose's date was replaced by it.
  const latestByName = new Map<string, Row>();
  for (const dose of doses) if (!latestByName.has(dose.vaccine_name)) latestByName.set(dose.vaccine_name, dose);
  const nextDue = [...latestByName.values()].filter((row) => row.next_due_on).sort((a, b) => a.next_due_on.localeCompare(b.next_due_on))[0];
  if (doses[0]) {
    const overdue = nextDue && nextDue.next_due_on < day(0);
    lines.push(
      `วัคซีนล่าสุด ${doses[0].vaccine_name} เมื่อ ${dateTh(doses[0].administered_at)} ${cite('vaccination', doses[0].id, doses[0].administered_at)}` +
        (nextDue ? `, ${overdue ? 'เลยกำหนด' : 'ครบกำหนดครั้งถัดไป'} ${nextDue.vaccine_name} ${dateTh(nextDue.next_due_on)}` : ''),
    );
  }

  return { text: `${lines.join('\n')}\n\n${NOTE}`, citations };
}

route('POST', '/staff/ai/pets/:id/summary', (ctx) => {
  requireStaff(ctx, SCOPE);
  const pet = table('pets').get(ctx.params.id);
  if (!pet) notFound('สัตว์เลี้ยง');
  count('pet_summary');
  const { text, citations } = summarisePet(pet);
  return { pet_id: pet.id, text, citations, cached: false, generated_at: nowIso() };
});

// ---- Ask the numbers -----------------------------------------------------------------------------

interface AskReply {
  reply: string;
  actions: Array<{ label: string; href: string }>;
}

async function ask(question: string, auth: Auth): Promise<AskReply> {
  const q = question.toLowerCase();
  const pet = petNamedIn(question);
  if (pet && has(q, ['สรุป', 'ประวัติ', 'เป็นอะไร', 'แพ้', 'วัคซีน', 'น้ำหนัก']) ) {
    return { reply: summarisePet(pet).text.replace(`\n\n${NOTE}`, ''), actions: [{ label: `เปิดประวัติ${pet.name}`, href: `/pets/${pet.id}` }] };
  }

  if (has(q, ['สต๊อก', 'สต็อก', 'สั่ง', 'หมด', 'ใกล้หมด', 'หมดอายุ', 'คลัง'])) {
    const alerts = await stockAlerts(auth);
    if (!alerts.length) return { reply: 'ตอนนี้ไม่มีรายการสต๊อกที่ต่ำกว่าจุดสั่งซื้อหรือใกล้หมดอายุค่ะ', actions: [{ label: 'ดูสต๊อก', href: '/inventory' }] };
    const label: Record<string, string> = { out_of_stock: 'หมด', low_stock: 'ต่ำกว่าจุดสั่งซื้อ', expired: 'หมดอายุแล้ว', near_expiry: 'ใกล้หมดอายุ' };
    const lines = alerts.slice(0, 6).map((row) => {
      const amount = row.available !== undefined ? ` เหลือ ${num(row.available)}${row.unit ? ` ${row.unit}` : ''}${row.reorder_level !== undefined ? ` (จุดสั่งซื้อ ${num(row.reorder_level)})` : ''}` : '';
      return `• ${row.item_name}: ${label[row.kind] ?? row.kind}${amount}${row.expires_on ? ` หมดอายุ ${dateTh(row.expires_on)}` : ''}`;
    });
    return {
      reply: [`มี ${num(alerts.length)} รายการที่ต้องดูแล:`, ...lines, alerts.length > 6 ? `และอีก ${num(alerts.length - 6)} รายการ` : ''].filter(Boolean).join('\n'),
      actions: [{ label: 'ดูสต๊อกที่ต้องดูแล', href: '/inventory/alerts' }],
    };
  }

  if (has(q, ['ทำเงิน', 'บริการไหน', 'ขายดี', 'บริการยอดนิยม'])) {
    const services = (await topServices(auth)).slice(0, 5);
    if (!services.length) return { reply: 'เดือนนี้ยังไม่มีบิลที่ออกแล้วให้สรุปค่ะ', actions: [{ label: 'ดูบริการทำเงิน', href: '/dashboard?tab=business' }] };
    return {
      reply: [`บริการที่ทำรายได้มากที่สุดเดือนนี้ (ถึงวันนี้):`, ...services.map((row, index) => `${index + 1}. ${row.name} ${baht(row.revenue)} (${num(row.quantity)} ครั้ง)`)].join('\n'),
      actions: [{ label: 'ดูบริการทำเงิน', href: '/dashboard?tab=business' }],
    };
  }

  if (has(q, ['รายได้', 'ยอดขาย', 'เงินเข้า', 'ยอดเก็บ'])) {
    const yesterday = has(q, ['เมื่อวาน']);
    const month = has(q, ['เดือน']);
    const from = month ? `${thisMonth()}-01` : yesterday ? day(-1) : day(0);
    const to = yesterday ? day(-1) : day(0);
    const money = collected(from, to);
    const when = month ? 'เดือนนี้' : yesterday ? 'เมื่อวาน' : 'วันนี้';
    return {
      reply: `${when}รับชำระแล้ว ${baht(money.collected)} จาก ${num(money.payments)} รายการ${money.refunded ? ` คืนเงิน ${baht(money.refunded)} สุทธิ ${baht(money.collected - money.refunded)}` : ''}`,
      actions: [{ label: 'ดูรายได้', href: '/dashboard?tab=business' }],
    };
  }

  if (has(q, ['คิว', 'รอนาน', 'รอกี่'])) {
    const today = await todayNumbers(auth);
    const week = await callRoute<any>('GET', '/staff/stats/queue', { auth, query: { from: day(-6), to: day(0) } });
    const weekLine = week?.totals?.average_wait_minutes !== undefined ? `เฉลี่ย 7 วันที่ผ่านมา รอ ${num(Math.round(week.totals.average_wait_minutes))} นาที จาก ${num(week.totals.tickets)} คิว${week.totals.peak_hour !== undefined ? ` ช่วงคนเยอะที่สุด ${week.totals.peak_hour}:00 น.` : ''}` : '';
    return {
      reply: [
        `วันนี้ออกคิวไปแล้ว ${num(today.queue.issued)} คิว รออยู่ ${num(today.queue.waiting)} ตรวจเสร็จ ${num(today.queue.served)}`,
        today.queue.average_wait_minutes !== undefined ? `เวลารอเฉลี่ยวันนี้ ${num(Math.round(today.queue.average_wait_minutes))} นาที` : '',
        weekLine,
      ]
        .filter(Boolean)
        .join('\n'),
      actions: [
        { label: 'ดูคิววันนี้', href: '/queue' },
        { label: 'ดูสถิติคิว', href: '/dashboard?tab=operations' },
      ],
    };
  }

  if (has(q, ['นัด', 'ลูกค้ามา', 'เคส'])) {
    const date = has(q, ['พรุ่งนี้']) ? day(1) : day(0);
    if (date !== day(0)) {
      const rows = rowsOfTable('appointments').filter((row) => localDay(row.starts_at) === date && !['cancelled', 'draft'].includes(row.status));
      return { reply: `พรุ่งนี้มีนัด ${num(rows.length)} ราย`, actions: [{ label: 'ดูตารางนัด', href: '/appointments' }] };
    }
    const today = await todayNumbers(auth);
    const a = today.appointments;
    return {
      reply: [
        `วันนี้มีนัดทั้งหมด ${num(a.total)} ราย (ทุกสาขา)`,
        `• รอมาถึง ${num(a.upcoming)} · มาถึงแล้ว ${num(a.arrived)} · ตรวจเสร็จ ${num(a.completed)}`,
        a.cancelled || a.no_show ? `• ยกเลิก ${num(a.cancelled)} · ไม่มาตามนัด ${num(a.no_show)}` : '',
        a.walk_in ? `• walk-in ${num(a.walk_in)} ราย` : '',
      ]
        .filter(Boolean)
        .join('\n'),
      actions: [
        { label: 'ดูภาพรวมวันนี้', href: '/dashboard' },
        { label: 'ดูตารางนัด', href: '/appointments' },
      ],
    };
  }

  if (has(q, ['รีวิว', 'คะแนน'])) {
    const summary = reviewSummary(publishedReviews());
    const waiting = table('reviews').where((row) => row.status === 'pending').length;
    return {
      reply: `คะแนนรีวิวเฉลี่ย ${summary.average ?? '-'} จาก ${num(summary.count)} รีวิวที่เผยแพร่ ห้าดาว ${num(summary.by_rating['5'] ?? 0)} รีวิว รอตรวจ ${num(waiting)} รีวิว`,
      actions: [{ label: 'ตรวจรีวิว', href: '/settings/reviews' }],
    };
  }

  if (pet) {
    return { reply: summarisePet(pet).text.replace(`\n\n${NOTE}`, ''), actions: [{ label: `เปิดประวัติ${pet.name}`, href: `/pets/${pet.id}` }] };
  }

  return {
    reply: 'ในเดโมนี้ผู้ช่วยตอบได้เรื่องนัดวันนี้ คิวและเวลารอ สต๊อกที่ต้องสั่ง รายได้ บริการที่ทำเงิน รีวิว และสรุปประวัติสัตว์ (เช่น "สรุปประวัติของโมจิ") ลองถามใหม่ได้เลยค่ะ',
    actions: [],
  };
}

route('POST', '/staff/ai/ask', async (ctx) => {
  const auth = requireStaff(ctx, SCOPE);
  const messages = Array.isArray(ctx.body?.messages) ? (ctx.body.messages as Array<{ role: string; text: string }>) : [];
  if (!messages.length || messages.length > 20) invalid('messages', 'ต้องมีข้อความ 1 ถึง 20 ข้อความ');
  const last = [...messages].reverse().find((message) => message.role === 'user');
  if (!last?.text?.trim()) invalid('messages', 'ไม่พบคำถาม');
  count('ask');
  const result = await ask(last.text.trim(), auth);
  return { reply: `${result.reply}\n\n${NOTE}`, actions: result.actions };
});

// ---- SOAP draft ----------------------------------------------------------------------------------

const SYSTEMS: Array<{ system: string; words: string[] }> = [
  { system: 'ผิวหนังและขน', words: ['ผิว', 'ขน', 'คัน', 'ผื่น', 'แผล'] },
  { system: 'ตา', words: ['ตา', 'ขี้ตา'] },
  { system: 'หู', words: ['หู'] },
  { system: 'ช่องปากและฟัน', words: ['ฟัน', 'เหงือก', 'ปาก', 'หินปูน'] },
  { system: 'หัวใจและหลอดเลือด', words: ['หัวใจ', 'murmur', 'ชีพจร'] },
  { system: 'ระบบหายใจ', words: ['ไอ', 'จาม', 'หายใจ', 'ปอด', 'น้ำมูก'] },
  { system: 'ระบบทางเดินอาหาร', words: ['อาเจียน', 'ท้องเสีย', 'ถ่าย', 'ท้อง', 'เบื่ออาหาร'] },
  { system: 'กระดูกและกล้ามเนื้อ', words: ['ขา', 'เดินกะเผลก', 'ข้อ', 'กระดูก'] },
];

const NORMAL_WORDS = ['ปกติ', 'ไม่พบ', 'ไม่มี'];

route('POST', '/staff/ai/consultations/:id/soap-draft', (ctx: Ctx) => {
  requireStaff(ctx, SCOPE);
  const consultation = table('consultations').get(ctx.params.id);
  if (!consultation) notFound('เคสการตรวจ');
  const notes = String(ctx.body?.notes ?? '').trim();
  if (!notes) invalid('notes', 'ต้องมีบันทึกสั้น ๆ ให้ร่าง');
  count('soap_draft');

  // A full stop ends a sentence only before a space or the end, so "T 39.4" stays whole.
  const sentences = notes.split(/\n|;|\.(?=\s|$)|\s{2,}/).map((part) => part.trim()).filter(Boolean);
  // Each sentence goes to one field: assessment first, then plan, then findings; the rest is history.
  const bucket = (part: string): 'assessment' | 'plan' | 'objective' | 'subjective' => {
    const lower = part.toLowerCase();
    if (has(lower, ['สงสัย', 'น่าจะ', 'วินิจฉัย', 'dx', 'เข้าได้กับ'])) return 'assessment';
    if (has(lower, ['ให้ยา', 'นัด', 'ฉีด', 'แนะนำ', 'ตรวจเพิ่ม', 'ส่งแล็บ', 'mg', 'งด'])) return 'plan';
    if (/\b(t|hr|rr|bcs|crt)\b/.test(lower) || has(lower, ['อุณหภูมิ', 'ชีพจร', 'น้ำหนัก', 'กก', 'กดเจ็บ', 'คลำ', 'ฟังปอด', 'ตรวจพบ', 'เหงือก'])) return 'objective';
    return 'subjective';
  };
  const measured = sentences.filter((part) => bucket(part) === 'objective');
  const assessed = sentences.filter((part) => bucket(part) === 'assessment');
  const planned = sentences.filter((part) => bucket(part) === 'plan');
  const history = sentences.filter((part) => bucket(part) === 'subjective');
  const pet = table('pets').get(consultation.pet_id);

  // Only the systems the notes speak of: a system not listed was not examined, so none is invented.
  const examination = SYSTEMS.flatMap(({ system, words }) => {
    const said = sentences.find((part) => has(part, words));
    return said ? [{ body_system: system, is_normal: has(said, NORMAL_WORDS), note: said }] : [];
  });

  return {
    consultation_id: consultation.id,
    draft: {
      chief_complaint: history[0] ?? sentences[0] ?? '',
      subjective: [pet ? `${pet.name} (${pet.species}${pet.breed ? ` ${pet.breed}` : ''})` : '', ...history].filter(Boolean).join(' '),
      objective: measured.join(' ') || 'ตรวจร่างกายทั่วไป (เติมค่าที่วัดได้)',
      assessment: assessed.join(' ') || 'รอผลประเมินจากสัตวแพทย์',
      plan: planned.join(' ') || 'ระบุแผนการรักษาและนัดติดตาม',
      examination,
    },
    warnings: [],
  };
});

// ---- Owner messages ------------------------------------------------------------------------------

route('POST', '/staff/ai/drafts/owner-message', (ctx) => {
  requireStaff(ctx, SCOPE);
  const kind = ctx.body?.kind;
  count('owner_message');

  if (kind === 'aftercare') {
    const consultation = table('consultations').get(ctx.body?.consultation_id);
    if (!consultation) notFound('เคสการตรวจ');
    const pet = table('pets').get(consultation.pet_id);
    const name = pet?.name ? `น้อง${pet.name.replace(/^น้อง/, '')}` : 'น้อง';
    const medicines = (consultation.prescriptions ?? []) as Array<{ drug_name: string; dose: string; frequency: string; duration_days: number; instructions?: string }>;
    const diagnosis = (consultation.diagnoses ?? []).find((row: any) => row.is_primary)?.description ?? consultation.assessment;
    const text = [
      `คำแนะนำการดูแล${name}ที่บ้านค่ะ`,
      diagnosis ? `ผลการตรวจวันนี้: ${diagnosis}` : '',
      medicines.length
        ? `ยาที่ได้รับ:\n${medicines.map((row) => `• ${row.drug_name} ${row.dose} ${row.frequency} ${row.duration_days ? `นาน ${row.duration_days} วัน` : ''}${row.instructions ? ` (${row.instructions})` : ''}`.replace(/\s+\(/, ' (').trim()).join('\n')}\nให้ยาจนครบตามที่หมอสั่ง แม้อาการจะดีขึ้นแล้ว`
        : '',
      consultation.plan ? `แผนการดูแล: ${consultation.plan}` : '',
      `ให้${name}พักในที่สงบ มีน้ำสะอาดให้ตลอด สังเกตการกินและการขับถ่าย`,
      'ถ้ามีอาการซึมมาก ไม่กินอาหารเกิน 1 วัน อาเจียนซ้ำ หรือหายใจลำบาก ให้โทรหาคลินิกหรือพามาทันทีค่ะ',
      NOTE,
    ]
      .filter(Boolean)
      .join('\n\n');
    return { text };
  }

  if (kind === 'thread_reply') {
    const thread = table('msg_threads').get(ctx.body?.thread_id);
    if (!thread) notFound('บทสนทนา');
    const customer = table('customers').get(thread.customer_id);
    const last = table('msg_thread_messages')
      .where((row) => row.thread_id === thread.id && row.direction === 'inbound' && row.text)
      .sort((a, b) => b.sent_at.localeCompare(a.sent_at))[0];
    const asked = String(last?.text ?? '');
    const greeting = `สวัสดีค่ะ${customer?.display_name ? ` คุณ${String(customer.display_name).replace(/^คุณ/, '').split(' ')[0]}` : ''}`;
    const body = has(asked, ['นัด', 'จอง', 'เลื่อน'])
      ? 'เรื่องนัดหมาย ทางคลินิกตรวจสอบเวลาให้แล้วค่ะ สะดวกวันและช่วงเวลาไหน แจ้งได้เลย หรือจองผ่านหน้าเว็บของคลินิกได้ตลอด 24 ชั่วโมงค่ะ'
      : has(asked, ['ราคา', 'เท่าไร', 'ค่า'])
        ? 'เรื่องค่าใช้จ่าย ราคาขึ้นกับน้ำหนักและการตรวจที่น้องต้องได้รับค่ะ หมอจะแจ้งประมาณการให้ก่อนเริ่มทุกครั้ง'
        : has(asked, ['ยา', 'กิน', 'อาการ', 'อาเจียน', 'ซึม'])
          ? 'ขอบคุณที่แจ้งอาการมาค่ะ ขอให้ส่งรูปหรือคลิปอาการของน้องเพิ่มเติม หมอจะดูและติดต่อกลับโดยเร็ว ถ้าอาการแย่ลง โทรหาคลินิกหรือพามาได้ทันทีค่ะ'
          : 'ขอบคุณที่ติดต่อมาค่ะ ทางคลินิกได้รับข้อความแล้ว และจะช่วยดูแลเรื่องนี้ให้นะคะ';
    return { text: `${greeting}\n\n${body}\n\nทีมงาน Happy Paws\n\n${NOTE}` };
  }

  invalid('kind', 'kind ต้องเป็น aftercare หรือ thread_reply');
});

// ---- Daily brief ---------------------------------------------------------------------------------

route('GET', '/staff/ai/daily-brief', async (ctx) => {
  const auth = requireStaff(ctx, 'dashboard:read');
  const branchId = ctx.query.get('branch_id') ?? undefined;
  if (!branchId) invalid('branch_id', 'ต้องระบุสาขา');
  const date = day(0);

  const today = await todayNumbers(auth, branchId);
  const alerts = await stockAlerts(auth, branchId);
  const money = collected(day(-1), day(-1));
  const mayReadMoney = auth.scopes.includes('*') || auth.scopes.some((scope) => scope.startsWith('analytics') || scope.startsWith('invoice'));
  const inventory = {
    expired: alerts.filter((row) => row.kind === 'expired').length,
    out_of_stock: alerts.filter((row) => row.kind === 'out_of_stock').length,
    near_expiry: alerts.filter((row) => row.kind === 'near_expiry').length,
    low_stock: alerts.filter((row) => row.kind === 'low_stock').length,
    top: alerts.slice(0, 3).map((row) => ({ kind: row.kind, item_name: row.item_name })),
  };

  const a = today.appointments;
  const text = [
    `วันนี้มีนัด ${num(a.total)} ราย ยังรอมาถึง ${num(a.upcoming)} ราย${a.walk_in ? ` และ walk-in ${num(a.walk_in)} ราย` : ''}${a.cancelled ? ` ยกเลิกไปแล้ว ${num(a.cancelled)} ราย` : ''}`,
    today.queue.issued ? `คิวออกแล้ว ${num(today.queue.issued)} คิว ยังรอ ${num(today.queue.waiting)} คิว` : 'ยังไม่มีคิวออกวันนี้',
    alerts.length
      ? `สต๊อกที่ต้องดูแล ${num(alerts.length)} รายการ เช่น ${inventory.top.map((row) => row.item_name).join(', ')}`
      : 'สต๊อกไม่มีรายการที่ต้องเร่งสั่ง',
    mayReadMoney && money.payments ? `เมื่อวานรับชำระ ${baht(money.collected)} จาก ${num(money.payments)} รายการ` : '',
    NOTE,
  ]
    .filter(Boolean)
    .join('\n');

  return {
    status: 'ready',
    branch_id: branchId,
    date,
    generates_at: '07:00',
    text,
    numbers: {
      appointments: { total: a.total, upcoming: a.upcoming, arrived: a.arrived, cancelled: a.cancelled, walk_in: a.walk_in, booked_online: a.online },
      queue: { tickets: today.queue.issued, outstanding: today.queue.waiting, served: today.queue.served },
      inventory,
      ...(mayReadMoney
        ? { yesterday_revenue: { date: day(-1), collected_satang: money.collected, refunded_satang: money.refunded, net_satang: money.collected - money.refunded, payments: money.payments } }
        : {}),
    },
    generated_at: (() => {
      const at7 = new Date();
      at7.setHours(7, 0, 0, 0);
      return at7.toISOString();
    })(),
  };
});
