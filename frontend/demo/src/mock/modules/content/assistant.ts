import { day } from '../../core/db';
import { invalid, route } from '../../core/http';
import { publishedArticles } from './articles';
import { baht, callRoute, clinicFacts, hoursLine, rowsOfTable, weekdayTh, type ClinicFacts } from './peer';

/**
 * The public chat widget (docs/contract-extensions.md § 16). A static demo has no language model,
 * so the reply is picked by the words in the last question, and every fact in it is read from the
 * same answers the site's own pages show: the hours, the contacts, the service menu and its
 * prices, tomorrow's free times. Each reply says it is a demo answer.
 */

export const DEMO_NOTE = '(คำตอบตัวอย่างของเดโม ในระบบจริงผู้ช่วย AI จะตอบจากข้อมูลคลินิกเดียวกันนี้)';

interface Action {
  label: string;
  href: string;
}

interface Reply {
  reply: string;
  actions: Action[];
}

const has = (text: string, words: string[]) => words.some((word) => text.includes(word));

const timeOf = (iso: string) =>
  new Date(iso).toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit', hour12: false });

const dateTh = (date: string) =>
  new Date(`${date}T12:00:00`).toLocaleDateString('th-TH', { weekday: 'long', day: 'numeric', month: 'long' });

/** The menu, from the public route when it is there, else the service table as the clinic keeps it. */
function menu(facts: ClinicFacts): ClinicFacts['services'] {
  if (facts.services.length) return facts.services;
  return rowsOfTable('services').filter((row) => row.is_active !== false && row.price_satang !== undefined && !row.deleted_at);
}

function phoneOf(facts: ClinicFacts, kind = 'phone'): string | undefined {
  return facts.contacts.find((contact) => contact.kind === kind)?.value;
}

function lineOf(facts: ClinicFacts): string | undefined {
  return facts.contacts.find((contact) => contact.kind === 'line')?.value;
}

function hoursReply(facts: ClinicFacts, question: string): Reply {
  const line = hoursLine(facts.days);
  const todayName = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'][new Date().getDay()]!;
  const todayHours = facts.days.find((entry) => entry.day === todayName);
  const holiday = facts.holidays.filter((entry) => entry.date >= day(0)).sort((a, b) => a.date.localeCompare(b.date))[0];
  const parts = [
    line ? `${facts.name} เปิดทำการ ${line}` : `เวลาทำการของ ${facts.name} ดูได้ที่หน้าติดต่อเรา`,
    todayHours
      ? todayHours.closed
        ? `วันนี้ (วัน${weekdayTh(todayName)}) คลินิกปิดค่ะ`
        : `วันนี้ (วัน${weekdayTh(todayName)}) เปิด ${todayHours.opens}–${todayHours.closes} น. ค่ะ`
      : '',
    holiday ? `วันหยุดถัดไป: ${holiday.name} ${dateTh(holiday.date)}` : '',
    has(question, ['ฉุกเฉิน', 'ดึก', 'กลางคืน'])
      ? `นอกเวลาทำการ ถ้าน้องมีอาการฉุกเฉิน โทร${phoneOf(facts, 'emergency') ? ` ${phoneOf(facts, 'emergency')}` : 'สายด่วนของคลินิก'}ได้ค่ะ`
      : '',
  ];
  return { reply: parts.filter(Boolean).join('\n'), actions: [{ label: 'ดูช่องทางติดต่อและแผนที่', href: '/contact' }] };
}

/** Words a question names a service by; each matches the services whose name holds it. */
const SERVICE_WORDS = ['ทำหมัน', 'ขูดหินปูน', 'หินปูน', 'ฟัน', 'อาบน้ำ', 'ตัดขน', 'ตัดเล็บ', 'ตรวจเลือด', 'เลือด', 'ผ่าตัด', 'ตรวจสุขภาพ', 'เอกซเรย์', 'อัลตร้าซาวด์', 'ไมโครชิป', 'ผิวหนัง', 'กระต่าย', 'วัคซีน'];
const CATEGORY_ORDER = ['checkup', 'vaccine', 'dental', 'surgery', 'lab', 'grooming'];

/** The services a question is about: by the words above, narrowed to the species it names. */
function servicesAskedAbout(services: ClinicFacts['services'], question: string): ClinicFacts['services'] {
  const words = SERVICE_WORDS.filter((word) => question.includes(word));
  let found = services.filter((service) => words.some((word) => service.name.includes(word)));
  const cat = question.includes('แมว');
  const dog = question.includes('หมา') || question.includes('สุนัข');
  if (cat !== dog) {
    const narrowed = found.filter((service) => !service.name.includes(cat ? 'สุนัข' : 'แมว'));
    if (narrowed.length) found = narrowed;
  }
  return found;
}

function priceReply(facts: ClinicFacts, question: string): Reply {
  const services = menu(facts);
  if (!services.length) {
    return { reply: 'ดูบริการและราคาทั้งหมดได้ที่หน้าบริการค่ะ', actions: [{ label: 'ดูบริการและราคา', href: '/services' }] };
  }
  const named = servicesAskedAbout(services, question);
  // Asked in general: the cheapest of each kind, in the order the services page lists them.
  const overview = CATEGORY_ORDER.map((code) =>
    services.filter((service) => service.category_code === code).sort((a, b) => a.price_satang - b.price_satang)[0],
  ).filter((service): service is ClinicFacts['services'][number] => Boolean(service));
  const picked = named.length ? named.slice(0, 5) : overview;
  const lines = picked.map((service) => `• ${service.name} ${baht(service.price_satang)}${service.duration_minutes ? ` (ประมาณ ${service.duration_minutes} นาที)` : ''}`);
  return {
    reply: [
      named.length ? 'ราคาของบริการที่ถามถึงค่ะ' : `บริการของ ${facts.name} แต่ละหมวด ราคาเริ่มต้นค่ะ`,
      ...lines,
      'ราคาจริงอาจต่างตามน้ำหนักและชนิดสัตว์ หมอจะแจ้งก่อนเริ่มทุกครั้ง',
    ].join('\n'),
    actions: [
      { label: 'ดูบริการและราคาทั้งหมด', href: '/services' },
      { label: 'จองนัดออนไลน์', href: '/book' },
    ],
  };
}

function vaccineReply(facts: ClinicFacts, question: string): Reply {
  const vaccines = menu(facts).filter((service) => service.category_code === 'vaccine' || service.name.includes('วัคซีน'));
  const cat = has(question, ['แมว']);
  const dog = has(question, ['หมา', 'สุนัข']);
  const schedule = cat
    ? 'ลูกแมวเริ่มวัคซีนรวมเมื่ออายุประมาณ 6–8 สัปดาห์ กระตุ้นทุก 3–4 สัปดาห์จนอายุราว 16 สัปดาห์ และพิษสุนัขบ้าเมื่อ 12–16 สัปดาห์ จากนั้นกระตุ้นปีละครั้ง'
    : dog
      ? 'ลูกสุนัขเริ่มวัคซีนรวมเมื่ออายุประมาณ 6–8 สัปดาห์ กระตุ้นทุก 3–4 สัปดาห์จนอายุราว 16 สัปดาห์ และพิษสุนัขบ้าเมื่อ 12–16 สัปดาห์ จากนั้นกระตุ้นปีละครั้ง'
      : 'ลูกสุนัขและลูกแมวเริ่มวัคซีนเข็มแรกเมื่ออายุประมาณ 6–8 สัปดาห์ แล้วกระตุ้นทุก 3–4 สัปดาห์จนอายุราว 16 สัปดาห์ สัตว์โตฉีดกระตุ้นปีละครั้ง';
  const article = publishedArticles().find((row) => row.slug === (dog && !cat ? 'puppy-first-year' : 'cat-vaccine-schedule'));
  return {
    reply: [
      schedule,
      vaccines.length ? `ค่าวัคซีนที่คลินิก: ${vaccines.slice(0, 4).map((service) => `${service.name} ${baht(service.price_satang)}`).join(', ')}` : '',
      'ลองใส่วันเกิดของน้องในเครื่องมือวางแผนวัคซีน จะได้วันนัดแต่ละเข็มค่ะ',
    ]
      .filter(Boolean)
      .join('\n'),
    actions: [
      { label: 'วางแผนวัคซีน', href: '/vaccine-schedule' },
      ...(article ? [{ label: 'อ่านบทความเรื่องวัคซีน', href: `/articles/${article.slug}` }] : []),
      { label: 'จองนัดฉีดวัคซีน', href: '/book' },
    ],
  };
}

async function bookingReply(facts: ClinicFacts, question: string): Promise<Reply> {
  const services = menu(facts);
  const service =
    servicesAskedAbout(services, question)[0] ??
    services.find((row) => row.name.includes('ตรวจสุขภาพทั่วไป')) ??
    services.find((row) => row.category_code === 'checkup') ??
    services[0];
  if (!service || !facts.branches.length) {
    return { reply: 'จองนัดออนไลน์ได้ตลอด 24 ชั่วโมงที่หน้าจองนัดค่ะ เลือกบริการ สาขา และเวลาที่สะดวกได้เลย', actions: [{ label: 'จองนัดออนไลน์', href: '/book' }] };
  }
  const offering = (service as { branch_ids?: string[] }).branch_ids ?? [];
  const branches = facts.branches.filter((branch) => !offering.length || offering.includes(branch.id));
  const firstDay = question.includes('วันนี้') ? 0 : 1;

  // The first day from the one asked about that still has a free time, at any branch that offers it.
  for (let offset = firstDay; offset < firstDay + 4; offset += 1) {
    const date = day(offset);
    const found: string[] = [];
    for (const branch of branches) {
      const availability = await callRoute<{ slots?: Array<{ starts_at: string }> }>('GET', '/booking/availability', {
        query: { service: service.id, date, branch: branch.id },
      });
      const times = [...new Set((availability?.slots ?? []).map((slot) => slot.starts_at))].sort().slice(0, 4).map(timeOf);
      if (times.length) found.push(`${branch.name}: ${times.join(', ')} น.`);
    }
    if (found.length) {
      const when = offset === 0 ? 'วันนี้' : offset === 1 ? 'พรุ่งนี้' : dateTh(date);
      const missed = offset > firstDay ? `${firstDay === 0 ? 'วันนี้' : 'พรุ่งนี้'}เต็มแล้วค่ะ ` : '';
      return {
        reply: [`${missed}${when}ยังมีเวลาว่างสำหรับ "${service.name}" เช่น`, ...found.map((line) => `• ${line}`), 'จองออนไลน์ได้เลยค่ะ ระบบจะกันเวลาไว้ให้ 10 นาทีระหว่างยืนยัน'].join('\n'),
        actions: [{ label: 'จองนัดออนไลน์', href: '/book' }],
      };
    }
  }
  return {
    reply: `ช่วงนี้เวลาสำหรับ "${service.name}" เต็มแล้วค่ะ ลองดูวันอื่นในหน้าจองนัด หรือโทรสอบถามคลินิกได้`,
    actions: [{ label: 'จองนัดออนไลน์', href: '/book' }],
  };
}

function emergencyReply(facts: ClinicFacts): Reply {
  const hotline = phoneOf(facts, 'emergency') ?? phoneOf(facts);
  return {
    reply: [
      `ถ้าน้องหายใจลำบาก ชัก เลือดออกไม่หยุด ถูกรถชน หรือกินสารพิษ ให้${hotline ? `โทร ${hotline} ` : 'โทรหาคลินิก'}แล้วพามาทันทีค่ะ ไม่ต้องจองคิว`,
      'ระหว่างทาง ให้น้องอยู่นิ่งที่สุด ห่อด้วยผ้า และนำยาหรือห่อสิ่งที่น้องกินเข้าไปมาด้วย',
      'ผู้ช่วยนี้ให้คำแนะนำทางการแพทย์ไม่ได้ ถ้าไม่แน่ใจ โทรถามหมอได้เลยค่ะ',
    ].join('\n'),
    actions: [{ label: 'ดูเบอร์ฉุกเฉินและอาการที่ต้องรีบมา', href: '/emergency' }],
  };
}

function contactReply(facts: ClinicFacts): Reply {
  const phone = phoneOf(facts);
  const line = lineOf(facts);
  const branch = facts.branches[0];
  return {
    reply: [
      phone ? `โทร ${phone}` : '',
      line ? `LINE ${line}` : '',
      branch?.address ? `ที่อยู่: ${branch.address}` : '',
      'หรือแชทกับเราผ่าน LINE ของคลินิกได้ในเวลาทำการค่ะ',
    ]
      .filter(Boolean)
      .join('\n'),
    actions: [{ label: 'ดูช่องทางติดต่อและแผนที่', href: '/contact' }],
  };
}

function articleReply(question: string): Reply | undefined {
  const words = question.split(/\s+/).filter((word) => word.length >= 3);
  const article = publishedArticles().find((row) => row.tags?.some((tag) => question.includes(tag)) || words.some((word) => row.title.includes(word)));
  if (!article) return undefined;
  return {
    reply: `ทีมสัตวแพทย์เขียนเรื่องนี้ไว้ค่ะ: "${article.title}" ${article.summary}`,
    actions: [{ label: 'อ่านบทความ', href: `/articles/${article.slug}` }],
  };
}

async function answer(question: string, facts: ClinicFacts): Promise<Reply> {
  const q = question.toLowerCase();
  if (has(q, ['ฉุกเฉิน', 'ชัก', 'หายใจไม่', 'หายใจลำบาก', 'เลือดออก', 'รถชน', 'ยาเบื่อ', 'กินช็อกโกแลต', 'สลบ', 'หมดสติ'])) return emergencyReply(facts);
  if (has(q, ['วัคซีน', 'ฉีดยา', 'เข็ม'])) return vaccineReply(facts, q);
  if (has(q, ['จอง', 'คิวว่าง', 'ว่างไหม', 'นัด', 'ว่าง'])) return bookingReply(facts, q);
  if (has(q, ['ราคา', 'เท่าไร', 'เท่าไหร่', 'ค่าใช้จ่าย', 'บริการ', 'ทำหมัน', 'ขูดหินปูน', 'อาบน้ำ', 'ตัดขน', 'ตรวจเลือด'])) return priceReply(facts, q);
  if (has(q, ['เปิด', 'ปิด', 'กี่โมง', 'เวลาทำการ', 'วันหยุด', 'หยุด'])) return hoursReply(facts, q);
  if (has(q, ['ติดต่อ', 'เบอร์', 'โทร', 'line', 'ไลน์', 'ที่อยู่', 'แผนที่', 'อยู่ที่ไหน', 'ไปยังไง', 'จอดรถ'])) return contactReply(facts);
  if (has(q, ['ขอบคุณ', 'thank'])) return { reply: 'ยินดีค่ะ ถ้ามีอะไรอยากถามเพิ่มเติม พิมพ์มาได้เลยนะคะ', actions: [] };
  if (has(q, ['สวัสดี', 'hello', 'hi'])) {
    return { reply: `สวัสดีค่ะ ยินดีต้อนรับสู่ ${facts.name} ถามเรื่องเวลาทำการ บริการและราคา วัคซีน หรือหาเวลาว่างสำหรับนัดได้เลยค่ะ`, actions: [] };
  }
  const article = articleReply(q);
  if (article) return article;
  return {
    reply: 'ขออภัยค่ะ เรื่องนี้ผู้ช่วยยังตอบไม่ได้ ถามเรื่องเวลาทำการ บริการและราคา วัคซีน หรือการจองนัดได้ หรือติดต่อพนักงานโดยตรงค่ะ',
    actions: [{ label: 'ดูช่องทางติดต่อ', href: '/contact' }],
  };
}

route('GET', '/assistant', () => ({ enabled: true }));

route('POST', '/assistant/chat', async ({ body }) => {
  const messages = Array.isArray(body?.messages) ? (body.messages as Array<{ role: string; text: string }>) : [];
  if (!messages.length || messages.length > 20) invalid('messages', 'ต้องมีข้อความ 1 ถึง 20 ข้อความ');
  const last = [...messages].reverse().find((message) => message.role === 'user');
  if (!last?.text?.trim()) invalid('messages', 'ไม่พบคำถาม');
  const result = await answer(last.text.trim(), await clinicFacts());
  return { reply: `${result.reply}\n\n${DEMO_NOTE}`, actions: result.actions };
});
