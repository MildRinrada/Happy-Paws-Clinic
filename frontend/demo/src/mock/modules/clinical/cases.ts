/**
 * The sample clinic's casebook: the kinds of visit a Thai small-animal clinic writes up every week,
 * each with its SOAP notes, examination, diagnosis, procedures, drugs and lab work. The seed picks
 * one per visit by the service booked and the species. Drug doses are left as the vet would write
 * them on a label, not as advice.
 */

export type Species = 'dog' | 'cat' | 'other';

/** `[body system, normal?, what was found]`; systems not listed were examined and normal. */
export type Finding = [system: string, normal: boolean, note?: string];

export interface CaseSpec {
  key: string;
  species: Species[];
  /** Which booked service names lead here. */
  services?: RegExp;
  /** Which reasons for the visit, as the owner gave them at booking, lead here. */
  reasons?: RegExp;
  complaint: string;
  subjective: string;
  objective: string;
  assessment: string;
  plan: string;
  diagnoses: { description: string; code?: string }[];
  findings: Finding[];
  /** `[name, quantity, price in baht]`. */
  treatments: [string, number, number][];
  /** `[drug, dose, frequency, days, quantity, instructions?]`. */
  drugs: [string, string, string, number, number, string?][];
  /** Tests sent, and the values that come back out of range. */
  lab?: { tests: { name: string; code?: string }[]; destination?: 'external'; abnormal?: Record<string, number | string> };
  /** A raised temperature, for a fever. */
  fever?: boolean;
  /** A follow-up in this many days, written into the plan. */
  followUpDays?: number;
}

export const EXAM_SYSTEMS = ['ผิวหนังและขน', 'ตา', 'หู', 'ช่องปากและฟัน', 'หัวใจและหลอดเลือด', 'ระบบหายใจ', 'ระบบทางเดินอาหาร', 'กล้ามเนื้อและกระดูก', 'ต่อมน้ำเหลือง'];

export const CASES: CaseSpec[] = [
  {
    key: 'vaccine',
    species: ['dog', 'cat', 'other'],
    services: /วัคซีน|vaccin/i,
    complaint: 'ฉีดวัคซีนประจำปี',
    subjective: 'มาฉีดวัคซีนตามนัด เจ้าของแจ้งว่ากินดี ขับถ่ายปกติ ร่าเริง ไม่มีอาการป่วยในช่วงสัปดาห์ที่ผ่านมา',
    objective: 'ร่าเริง ตอบสนองดี BCS 5/9 เยื่อเมือกสีชมพู CRT < 2 วินาที ฟังเสียงหัวใจและปอดปกติ ไม่พบเห็บหมัด',
    assessment: 'สุขภาพแข็งแรง พร้อมรับวัคซีน',
    plan: 'ฉีดวัคซีนตามโปรแกรม สังเกตอาการแพ้ 30 นาทีที่คลินิก งดอาบน้ำ 3 วัน นัดเข็มถัดไปตามสมุดวัคซีน',
    diagnoses: [{ description: 'ตรวจสุขภาพก่อนฉีดวัคซีน สุขภาพแข็งแรง', code: 'Z23' }],
    findings: [],
    treatments: [['ตรวจร่างกายก่อนฉีดวัคซีน', 1, 200], ['ฉีดวัคซีน', 1, 650]],
    drugs: [],
  },
  {
    key: 'checkup',
    species: ['dog', 'cat'],
    services: /ตรวจสุขภาพประจำปี/,
    reasons: /ประจำปี|วัยชรา/,
    complaint: 'ตรวจสุขภาพประจำปี',
    subjective: 'ตรวจสุขภาพประจำปี เจ้าของสังเกตว่ากินน้ำมากขึ้นเล็กน้อยในช่วงอากาศร้อน ไม่มีอาเจียนหรือท้องเสีย',
    objective: 'BCS 6/9 น้ำหนักเพิ่มจากครั้งก่อนเล็กน้อย มีหินปูนเกรด 1 ที่ฟันกรามบน หัวใจและปอดปกติ คลำช่องท้องไม่พบก้อน',
    assessment: 'สุขภาพโดยรวมดี น้ำหนักเกินเล็กน้อย หินปูนระยะแรก',
    plan: 'ตรวจเลือดประจำปี ปรับอาหารลดปริมาณ 10% แปรงฟันสัปดาห์ละ 3 ครั้ง ถ่ายพยาธิทุก 3 เดือน',
    diagnoses: [{ description: 'ตรวจสุขภาพประจำปี', code: 'Z00.0' }, { description: 'น้ำหนักเกินเล็กน้อย (BCS 6/9)', code: 'E66' }],
    findings: [['ช่องปากและฟัน', false, 'หินปูนเกรด 1 ฟันกรามบน']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['ตรวจเลือด CBC และเคมีเลือด', 1, 1200], ['ยาถ่ายพยาธิ', 1, 150]],
    drugs: [['ยาถ่ายพยาธิชนิดเม็ด Drontal', '1 เม็ด', 'ครั้งเดียว', 1, 1, 'ให้หลังอาหาร']],
    lab: { tests: [{ name: 'CBC', code: 'CBC' }, { name: 'เคมีเลือด', code: 'CHEM10' }] },
  },
  {
    key: 'skin',
    species: ['dog'],
    reasons: /คัน|เกา|ขนร่วง|ผื่น|เลียเท้า/,
    services: /ผิวหนัง|skin/i,
    complaint: 'คันมาก เกาจนขนร่วงเป็นหย่อม',
    subjective: 'คันมาก 2 สัปดาห์ เกาและเลียอุ้งเท้าบ่อย ขนร่วงที่ท้องและขาหนีบ อาบน้ำที่บ้านด้วยแชมพูทั่วไป ไม่ดีขึ้น',
    objective: 'ผิวหนังแดง มีตุ่มหนองและสะเก็ดที่ท้องและขาหนีบ ขนร่วงเป็นวงขนาด 1-2 ซม. หลายจุด ขูดตรวจผิวหนังไม่พบไร พบแบคทีเรีย cocci จำนวนมาก',
    assessment: 'ผิวหนังอักเสบติดเชื้อแบคทีเรียชั้นตื้น (superficial pyoderma) สงสัยมีภูมิแพ้ผิวหนังเป็นพื้นฐาน',
    plan: 'ยาฆ่าเชื้อ 14 วัน อาบแชมพูยาสัปดาห์ละ 2 ครั้ง ใส่ปลอกคอกันเลีย นัดติดตาม 2 สัปดาห์ ถ้าไม่ดีขึ้นเพาะเชื้อและทดสอบความไวต่อยา',
    diagnoses: [{ description: 'ผิวหนังอักเสบติดเชื้อแบคทีเรีย (Superficial pyoderma)', code: 'L08.0' }, { description: 'สงสัยภูมิแพ้ผิวหนัง (Atopic dermatitis)', code: 'L20' }],
    findings: [['ผิวหนังและขน', false, 'ตุ่มหนอง สะเก็ด ขนร่วงเป็นวงที่ท้องและขาหนีบ']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['ขูดตรวจผิวหนังและส่องกล้อง', 1, 350], ['อาบน้ำยาฆ่าเชื้อ', 1, 400]],
    drugs: [
      ['Cephalexin 500 mg', '1 เม็ด', 'วันละ 2 ครั้ง หลังอาหาร', 14, 28, 'ให้ยาจนหมดแม้อาการดีขึ้น'],
      ['แชมพู Chlorhexidine 2%', 'อาบทั้งตัว', 'สัปดาห์ละ 2 ครั้ง', 14, 1, 'ทิ้งไว้ 10 นาทีก่อนล้างออก'],
    ],
    followUpDays: 14,
  },
  {
    key: 'ear',
    species: ['dog'],
    complaint: 'สะบัดหัวบ่อย เกาหู มีกลิ่น',
    subjective: 'สะบัดหัวและเกาหูข้างซ้ายมา 5 วัน มีกลิ่นเหม็น ชอบว่ายน้ำในสระที่บ้าน',
    objective: 'ช่องหูซ้ายแดง บวม มีขี้หูสีน้ำตาลเข้มกลิ่นเหม็น แก้วหูมองเห็นได้ ไม่ทะลุ ตรวจขี้หูพบยีสต์ Malassezia จำนวนมาก หูขวาปกติ',
    assessment: 'หูชั้นนอกอักเสบจากยีสต์ข้างซ้าย (Otitis externa, Malassezia)',
    plan: 'ล้างหูที่คลินิก หยอดยา 10 วัน ล้างหูวันเว้นวัน เช็ดหูให้แห้งหลังว่ายน้ำ นัดตรวจหูซ้ำ 10 วัน',
    diagnoses: [{ description: 'หูชั้นนอกอักเสบจากยีสต์ (Otitis externa)', code: 'H60' }],
    findings: [['หู', false, 'หูซ้ายแดง บวม ขี้หูสีน้ำตาลเข้ม พบยีสต์จำนวนมาก']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['ตรวจขี้หูด้วยกล้องจุลทรรศน์', 1, 200], ['ล้างหู', 1, 250]],
    drugs: [
      ['ยาหยอดหู Otomax', '4 หยด', 'วันละ 2 ครั้ง', 10, 1, 'หยอดหูซ้าย นวดโคนหูเบา ๆ'],
      ['น้ำยาล้างหู Epi-Otic', 'เติมให้เต็มช่องหู', 'วันเว้นวัน', 10, 1],
    ],
    followUpDays: 10,
  },
  {
    key: 'gastro',
    species: ['dog', 'cat'],
    reasons: /อาเจียน|ถ่ายเหลว|ท้องเสีย/,
    complaint: 'อาเจียนและถ่ายเหลว',
    subjective: 'อาเจียน 3 ครั้งเมื่อวาน ถ่ายเหลว 2 วัน ยังกินน้ำได้ กินอาหารน้อยลง เจ้าของสงสัยว่าแอบกินกระดูกไก่',
    objective: 'ซึมเล็กน้อย ขาดน้ำประมาณ 5% กดเจ็บช่องท้องเล็กน้อย ไม่พบก้อน เสียงลำไส้เพิ่มขึ้น ตรวจอุจจาระไม่พบพยาธิ',
    assessment: 'กระเพาะและลำไส้อักเสบเฉียบพลันจากการกินอาหารผิด (dietary indiscretion)',
    plan: 'ให้สารน้ำใต้ผิวหนัง ยาลดอาเจียน งดอาหาร 12 ชั่วโมงแล้วให้อาหารอ่อนย่อยง่ายมื้อเล็ก ถ้าอาเจียนต่อหรือมีเลือดให้กลับมาทันที',
    diagnoses: [{ description: 'กระเพาะและลำไส้อักเสบเฉียบพลัน (Acute gastroenteritis)', code: 'K52.9' }],
    findings: [['ระบบทางเดินอาหาร', false, 'กดเจ็บช่องท้องเล็กน้อย เสียงลำไส้เพิ่ม']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['ตรวจอุจจาระ', 1, 200], ['ให้สารน้ำใต้ผิวหนัง', 1, 450], ['ฉีดยาลดอาเจียน Maropitant', 1, 380]],
    drugs: [
      ['Metronidazole 200 mg', '1/2 เม็ด', 'วันละ 2 ครั้ง หลังอาหาร', 5, 5],
      ['โพรไบโอติกสำหรับสัตว์', '1 ซอง', 'วันละครั้ง', 5, 5, 'ผสมอาหาร'],
    ],
    lab: { tests: [{ name: 'CBC', code: 'CBC' }], abnormal: { WBC: 18.9, NEU: 14.2 } },
  },
  {
    key: 'uri',
    species: ['cat'],
    reasons: /ไอ|จาม/,
    complaint: 'จาม มีน้ำมูก ตาแฉะ',
    subjective: 'จามและมีน้ำมูกใส 4 วัน ตาแฉะทั้งสองข้าง กินอาหารน้อยลง ในบ้านมีแมวอีกตัวมีอาการคล้ายกัน',
    objective: 'มีไข้ น้ำมูกใสทั้งสองข้าง เยื่อบุตาแดงมีขี้ตา ฟังปอดเสียงหายใจปกติ ช่องปากไม่พบแผล',
    assessment: 'โรคทางเดินหายใจส่วนบนในแมว (Feline upper respiratory tract disease) สงสัยเชื้อไวรัสร่วมกับแบคทีเรียแทรกซ้อน',
    plan: 'ยาฆ่าเชื้อ 7 วัน เสริมไลซีน พ่นละอองยา เช็ดตาและจมูกด้วยน้ำเกลือ แยกจากแมวตัวอื่น ถ้าไม่กินอาหารเกิน 2 วันให้กลับมา',
    diagnoses: [{ description: 'โรคทางเดินหายใจส่วนบนในแมว (URTD)', code: 'J06.9' }, { description: 'เยื่อบุตาอักเสบ', code: 'H10' }],
    findings: [['ตา', false, 'เยื่อบุตาแดง มีขี้ตาทั้งสองข้าง'], ['ระบบหายใจ', false, 'น้ำมูกใส จามบ่อย']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['พ่นละอองยา (Nebulize)', 1, 250]],
    drugs: [
      ['Doxycycline 50 mg', '1/2 เม็ด', 'วันละครั้ง ตามด้วยน้ำ', 7, 4],
      ['L-Lysine เจล', '1 ช้อนชา', 'วันละ 2 ครั้ง', 14, 1],
      ['ยาหยอดตา Tobramycin', '1 หยด', 'วันละ 3 ครั้ง', 7, 1, 'หยอดตาทั้งสองข้าง'],
    ],
    fever: true,
  },
  {
    key: 'flutd',
    species: ['cat'],
    complaint: 'ปัสสาวะบ่อย เบ่งนาน',
    subjective: 'เข้ากระบะทรายบ่อย ปัสสาวะออกทีละน้อย มีสีชมพู 2 วัน เลียอวัยวะเพศบ่อย ยังกินได้',
    objective: 'กระเพาะปัสสาวะเล็ก ไม่ตึง คลำแล้วเจ็บ ไม่มีการอุดตัน ตรวจปัสสาวะพบเลือดและผลึก struvite',
    assessment: 'กระเพาะปัสสาวะอักเสบในแมว (FLUTD) ร่วมกับผลึก struvite ไม่มีการอุดตัน',
    plan: 'เปลี่ยนเป็นอาหารสูตรละลายนิ่ว เพิ่มการกินน้ำด้วยอาหารเปียกและน้ำพุ ยาลดปวด นัดตรวจปัสสาวะซ้ำ 2 สัปดาห์',
    diagnoses: [{ description: 'กระเพาะปัสสาวะอักเสบ (FLUTD) และผลึก struvite', code: 'N30.9' }],
    findings: [['ระบบทางเดินอาหาร', true], ['ต่อมน้ำเหลือง', true]],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['ตรวจปัสสาวะ', 1, 350]],
    drugs: [['Robenacoxib (Onsior) 6 mg', '1 เม็ด', 'วันละครั้ง หลังอาหาร', 3, 3], ['อาหารสูตรละลายนิ่ว Urinary S/O', 'ตามฉลาก', 'แทนอาหารเดิม', 30, 1]],
    lab: { tests: [{ name: 'ตรวจปัสสาวะ', code: 'UA' }], abnormal: { USG: 1.062, pH: 7.8, Blood: '3+', Protein: '1+' } },
    followUpDays: 14,
  },
  {
    key: 'dental',
    species: ['dog', 'cat'],
    services: /ฟัน|ทันต|dental/i,
    reasons: /หินปูน|กลิ่นปาก/,
    complaint: 'ขูดหินปูน มีกลิ่นปาก',
    subjective: 'มีกลิ่นปากมาก 1 เดือน เคี้ยวอาหารข้างเดียว ตรวจเลือดก่อนดมยาผ่านแล้ว งดน้ำงดอาหารมาตามนัด',
    objective: 'หินปูนเกรด 3 เหงือกอักเสบ ฟันกรามน้อยบนซ้าย (208) โยก ดมยาสลบด้วย isoflurane ราบรื่น ขูดหินปูนและขัดฟันทั้งปาก ถอนฟัน 208',
    assessment: 'โรคปริทันต์ระยะที่ 3 (Periodontal disease stage 3)',
    plan: 'ยาฆ่าเชื้อและยาลดปวด ให้อาหารนิ่ม 3 วัน เริ่มแปรงฟันทุกวันหลังแผลหาย นัดตรวจช่องปากทุก 6 เดือน',
    diagnoses: [{ description: 'โรคปริทันต์ระยะที่ 3', code: 'K05.3' }],
    findings: [['ช่องปากและฟัน', false, 'หินปูนเกรด 3 เหงือกอักเสบ ฟัน 208 โยก']],
    treatments: [['ดมยาสลบ', 1, 1500], ['ขูดหินปูนด้วยเครื่องอัลตราโซนิก', 1, 1800], ['ขัดฟัน', 1, 500], ['ถอนฟัน', 1, 600]],
    drugs: [['Amoxicillin-Clavulanate 250 mg', '1/2 เม็ด', 'วันละ 2 ครั้ง หลังอาหาร', 7, 7], ['Tramadol 50 mg', '1/4 เม็ด', 'วันละ 2 ครั้ง', 3, 2]],
  },
  {
    key: 'spay',
    species: ['dog', 'cat'],
    services: /หมัน|ผ่าตัด|spay|neuter|surgery/i,
    complaint: 'ผ่าตัดทำหมันตามนัด',
    subjective: 'มาผ่าตัดทำหมันตามนัด งดน้ำงดอาหาร 8 ชั่วโมง ร่าเริง ไม่มีประวัติป่วย',
    objective: 'ตรวจร่างกายก่อนผ่าตัดปกติ ผลเลือดก่อนผ่าตัดอยู่ในเกณฑ์ ดมยาสลบราบรื่น ผ่าตัดทำหมันเรียบร้อย เสียเลือดน้อย ฟื้นจากยาสลบดี',
    assessment: 'ทำหมันสำเร็จ ไม่มีภาวะแทรกซ้อน',
    plan: 'ใส่ปลอกคอกันเลีย 10 วัน ห้ามแผลโดนน้ำ จำกัดการวิ่งกระโดด ยาลดปวดและยาฆ่าเชื้อตามสั่ง นัดตัดไหม 10 วัน',
    diagnoses: [{ description: 'ผ่าตัดทำหมัน (Elective sterilisation)', code: 'Z30.2' }],
    findings: [],
    treatments: [['ตรวจเลือดก่อนผ่าตัด', 1, 900], ['ดมยาสลบ', 1, 1500], ['ผ่าตัดทำหมัน', 1, 2500], ['ปลอกคอกันเลีย', 1, 150]],
    drugs: [['Meloxicam 1.5 mg/ml', 'ตามน้ำหนักตัว', 'วันละครั้ง หลังอาหาร', 3, 1], ['Amoxicillin 250 mg', '1/2 เม็ด', 'วันละ 2 ครั้ง หลังอาหาร', 5, 5]],
    lab: { tests: [{ name: 'CBC', code: 'CBC' }, { name: 'เคมีเลือด', code: 'CHEM10' }] },
    followUpDays: 10,
  },
  {
    key: 'lame',
    species: ['dog', 'cat'],
    reasons: /กะเผลก/,
    complaint: 'ขาหลังซ้ายกะเผลก',
    subjective: 'เดินกะเผลกขาหลังซ้าย 3 วัน หลังกระโดดลงจากโซฟา ยังกินได้ปกติ',
    objective: 'ลงน้ำหนักขาหลังซ้ายน้อยลง (lameness 2/5) คลำเข่าซ้ายเจ็บ ทดสอบ drawer sign ให้ผลลบ ไม่บวม',
    assessment: 'กล้ามเนื้อและเอ็นรอบข้อเข่าซ้ายอักเสบ ยังไม่พบหลักฐานเอ็นไขว้หน้าขาด',
    plan: 'ยาลดอักเสบ 7 วัน จำกัดการเคลื่อนไหว เดินสายจูงสั้น ๆ ถ้าไม่ดีขึ้นใน 1 สัปดาห์ เอกซเรย์ข้อเข่า',
    diagnoses: [{ description: 'ข้อเข่าซ้ายอักเสบ (Stifle sprain)', code: 'S83.6' }],
    findings: [['กล้ามเนื้อและกระดูก', false, 'เจ็บเข่าซ้าย lameness 2/5 drawer sign ลบ']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['ตรวจระบบกระดูกและข้อ', 1, 300]],
    drugs: [['Carprofen 75 mg', '1/2 เม็ด', 'วันละครั้ง หลังอาหาร', 7, 4], ['อาหารเสริมบำรุงข้อ Glucosamine', '1 เม็ด', 'วันละครั้ง', 30, 30]],
    followUpDays: 7,
  },
  {
    key: 'kidney',
    species: ['cat'],
    reasons: /ซึม|กินอาหารน้อย|น้ำหนักลด/,
    complaint: 'กินน้อย น้ำหนักลด',
    subjective: 'กินน้อยลง 2 สัปดาห์ น้ำหนักลด กินน้ำและปัสสาวะมาก อาเจียนบางวัน',
    objective: 'ขาดน้ำประมาณ 6% ขนหยาบ ไตคลำได้ขนาดเล็กและผิวขรุขระ เหงือกซีดเล็กน้อย',
    assessment: 'สงสัยไตวายเรื้อรัง (CKD) รอผลเลือดเพื่อจัดระยะตาม IRIS',
    plan: 'ให้สารน้ำใต้ผิวหนัง เริ่มอาหารสูตรโรคไต ตรวจเลือดและปัสสาวะ นัดฟังผลและวางแผนให้สารน้ำที่บ้าน',
    diagnoses: [{ description: 'ไตวายเรื้อรัง (Chronic kidney disease)', code: 'N18' }],
    findings: [['ระบบทางเดินอาหาร', false, 'ขาดน้ำ 6%'], ['ผิวหนังและขน', false, 'ขนหยาบ']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['ตรวจเลือด CBC และเคมีเลือด', 1, 1200], ['ให้สารน้ำใต้ผิวหนัง', 1, 450]],
    drugs: [['อาหารสูตรโรคไต Renal', 'ตามฉลาก', 'แทนอาหารเดิม', 30, 2], ['Mirtazapine เจลทาใบหู', '1 เส้น (1.5 นิ้ว)', 'วันละครั้ง', 7, 1, 'ทาที่ใบหูด้านใน สลับข้าง']],
    lab: { tests: [{ name: 'CBC', code: 'CBC' }, { name: 'เคมีเลือด', code: 'CHEM10' }, { name: 'ตรวจปัสสาวะ', code: 'UA' }], abnormal: { BUN: 58, CREA: 3.6, HCT: 23, USG: 1.014 } },
  },
  {
    key: 'rabbit-gi',
    species: ['other'],
    complaint: 'ไม่กินอาหาร ไม่ถ่าย',
    subjective: 'ไม่กินหญ้าและอาหารเม็ดตั้งแต่เมื่อวาน ไม่ถ่ายมา 12 ชั่วโมง นั่งหลังค่อม',
    objective: 'ท้องอืดเล็กน้อย เสียงลำไส้ลดลง ฟันตัดยาวปกติ ไม่พบฟันกรามแหลม อุณหภูมิปกติ',
    assessment: 'ภาวะลำไส้หยุดเคลื่อนไหว (GI stasis)',
    plan: 'ป้อนอาหารเหลวสำหรับสัตว์กินพืชทุก 4 ชั่วโมง ยากระตุ้นลำไส้ ให้หญ้าแห้งไม่จำกัด ถ้าไม่ถ่ายใน 24 ชั่วโมงให้กลับมา',
    diagnoses: [{ description: 'ภาวะลำไส้หยุดเคลื่อนไหว (GI stasis)', code: 'K59.8' }],
    findings: [['ระบบทางเดินอาหาร', false, 'ท้องอืดเล็กน้อย เสียงลำไส้ลดลง']],
    treatments: [['ตรวจร่างกายสัตว์เลี้ยงพิเศษ', 1, 400], ['ให้สารน้ำใต้ผิวหนัง', 1, 350]],
    drugs: [['อาหารเหลว Critical Care', '20 ml', 'ทุก 4 ชั่วโมง', 3, 1], ['Metoclopramide น้ำ', '0.3 ml', 'วันละ 2 ครั้ง', 3, 1]],
  },
  {
    key: 'exotic-check',
    species: ['other'],
    complaint: 'ตรวจสุขภาพสัตว์เลี้ยงพิเศษ',
    subjective: 'ตรวจสุขภาพทั่วไป เจ้าของให้อาหารตามคำแนะนำ ขับถ่ายปกติ',
    objective: 'ตื่นตัว ขนและผิวปกติ น้ำหนักคงที่ ตรวจช่องปากปกติ',
    assessment: 'สุขภาพแข็งแรง',
    plan: 'เลี้ยงตามเดิม ตรวจสุขภาพทุก 6 เดือน',
    diagnoses: [{ description: 'ตรวจสุขภาพ สุขภาพแข็งแรง', code: 'Z00.0' }],
    findings: [],
    treatments: [['ตรวจร่างกายสัตว์เลี้ยงพิเศษ', 1, 400]],
    drugs: [],
  },
  {
    key: 'cat-skin',
    species: ['cat'],
    services: /ผิวหนัง/,
    reasons: /คัน|เกา|ขนร่วง|ผื่น|เลีย/,
    complaint: 'เกาและเลียขนจนขนบาง',
    subjective: 'เกาคอและเลียขนบริเวณหลังจนขนบาง 3 สัปดาห์ ไม่ได้หยอดยากันเห็บหมัดมา 4 เดือน แมวออกนอกบ้านบางครั้ง',
    objective: 'ขนบางบริเวณหลังส่วนท้ายและโคนหาง มีสะเก็ดเล็ก ๆ (miliary dermatitis) หวีพบมูลหมัด',
    assessment: 'ผิวหนังอักเสบจากการแพ้น้ำลายหมัด (Flea allergy dermatitis)',
    plan: 'หยอดยากำจัดเห็บหมัดทุกตัวในบ้าน ทำความสะอาดที่นอน ยาลดคันระยะสั้น นัดติดตาม 3 สัปดาห์',
    diagnoses: [{ description: 'ผิวหนังอักเสบจากการแพ้หมัด (Flea allergy dermatitis)', code: 'L23' }],
    findings: [['ผิวหนังและขน', false, 'ขนบางที่หลังส่วนท้าย สะเก็ดเล็ก พบมูลหมัด']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['หยอดยากำจัดเห็บหมัด', 1, 450]],
    drugs: [['ยาหยอดหลัง Revolution Plus', '1 หลอด', 'เดือนละครั้ง', 30, 3, 'หยอดที่ต้นคอ'], ['Prednisolone 5 mg', '1/2 เม็ด', 'วันละครั้ง หลังอาหาร', 5, 3]],
    followUpDays: 21,
  },
  {
    key: 'kennel-cough',
    species: ['dog'],
    reasons: /ไอ|จาม/,
    complaint: 'ไอแห้ง ๆ เหมือนมีอะไรติดคอ',
    subjective: 'ไอแห้ง 4 วัน ไอมากตอนตื่นเต้นหรือดึงสายจูง เพิ่งไปฝากเลี้ยงที่โรงแรมสุนัขเมื่อสัปดาห์ก่อน ยังกินได้ ร่าเริง',
    objective: 'กดหลอดลมแล้วไอ ฟังปอดเสียงปกติ ไม่มีน้ำมูก ไม่มีไข้',
    assessment: 'หลอดลมอักเสบติดเชื้อในสุนัข (Infectious tracheobronchitis, kennel cough)',
    plan: 'ยาฆ่าเชื้อและยาบรรเทาไอ ใช้สายรัดอกแทนปลอกคอ แยกจากสุนัขตัวอื่น 1 สัปดาห์ ถ้ามีไข้หรือหายใจเร็วให้กลับมา',
    diagnoses: [{ description: 'หลอดลมอักเสบติดเชื้อ (Kennel cough)', code: 'J20.9' }],
    findings: [['ระบบหายใจ', false, 'กดหลอดลมแล้วไอ ปอดปกติ']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300]],
    drugs: [['Doxycycline 100 mg', '1 เม็ด', 'วันละครั้ง ตามด้วยน้ำ', 10, 10], ['ยาแก้ไอ Dextromethorphan', '1/2 เม็ด', 'วันละ 2 ครั้ง', 5, 5]],
  },
  {
    key: 'eye',
    species: ['dog', 'cat'],
    reasons: /ตาแดง|ขี้ตา/,
    complaint: 'ตาแดง มีขี้ตาเขียว',
    subjective: 'ตาข้างขวาแดง มีขี้ตาสีเขียว 2 วัน หรี่ตาเป็นบางครั้ง ไม่มีประวัติอุบัติเหตุ',
    objective: 'เยื่อบุตาขวาแดง บวม ขี้ตาเป็นหนอง ย้อมสีกระจกตา (fluorescein) ไม่พบแผล ความดันตาปกติ',
    assessment: 'เยื่อบุตาอักเสบติดเชื้อแบคทีเรีย ตาขวา ไม่มีแผลที่กระจกตา',
    plan: 'หยอดยาฆ่าเชื้อ 7 วัน เช็ดขี้ตาด้วยน้ำเกลือก่อนหยอด ใส่ปลอกคอถ้าเกาตา นัดตรวจซ้ำถ้าไม่ดีขึ้นใน 3 วัน',
    diagnoses: [{ description: 'เยื่อบุตาอักเสบ ตาขวา (Bacterial conjunctivitis)', code: 'H10.0' }],
    findings: [['ตา', false, 'เยื่อบุตาขวาแดง บวม มีขี้ตาเป็นหนอง ไม่พบแผลที่กระจกตา']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['ย้อมสีกระจกตา (Fluorescein)', 1, 200]],
    drugs: [['ยาหยอดตา Tobramycin', '1 หยด', 'วันละ 4 ครั้ง', 7, 1, 'หยอดตาขวา']],
  },
  {
    key: 'tick-fever',
    species: ['dog'],
    reasons: /ซึม|กินอาหารน้อย|เบื่ออาหาร/,
    complaint: 'ซึม กินอาหารน้อยลง',
    subjective: 'ซึม กินอาหารน้อยลง 2 วัน เจ้าของพบเห็บที่ใบหูเมื่อสัปดาห์ก่อน ไม่อาเจียน',
    objective: 'มีไข้ เหงือกซีดเล็กน้อย มีจุดเลือดออกที่ท้อง พบเห็บ 3 ตัวที่ใบหูและซอกนิ้ว ต่อมน้ำเหลืองใต้ขากรรไกรโต',
    assessment: 'สงสัยโรคพยาธิเม็ดเลือดจากเห็บ (Ehrlichiosis) รอผล CBC และ PCR ยืนยัน',
    plan: 'เริ่ม Doxycycline 28 วัน กำจัดเห็บที่ตัวและบ้าน ส่ง PCR พยาธิเม็ดเลือดที่แล็บภายนอก นัดตรวจเลือดซ้ำ 1 สัปดาห์',
    diagnoses: [{ description: 'สงสัยโรคพยาธิเม็ดเลือด (Ehrlichiosis)', code: 'A79.8' }, { description: 'เกล็ดเลือดต่ำ (Thrombocytopenia)', code: 'D69.6' }],
    findings: [['ผิวหนังและขน', false, 'จุดเลือดออกที่ท้อง พบเห็บ 3 ตัว'], ['ต่อมน้ำเหลือง', false, 'ต่อมน้ำเหลืองใต้ขากรรไกรโต']],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300], ['ตรวจความสมบูรณ์ของเม็ดเลือด (CBC)', 1, 450], ['ส่งตรวจ PCR พยาธิเม็ดเลือด', 1, 1500]],
    drugs: [['Doxycycline 100 mg', '1 เม็ด', 'วันละครั้ง ตามด้วยน้ำ', 28, 28, 'ให้ยาจนครบแม้อาการดีขึ้น'], ['ยากินกำจัดเห็บหมัด NexGard', '1 เม็ด', 'เดือนละครั้ง', 30, 1]],
    lab: { tests: [{ name: 'CBC', code: 'CBC' }], abnormal: { PLT: 62, HCT: 31, WBC: 5.1 } },
    fever: true,
    followUpDays: 7,
  },
  {
    key: 'preop',
    species: ['dog', 'cat'],
    services: /CBC|เม็ดเลือด/,
    reasons: /ก่อนผ่าตัด/,
    complaint: 'ตรวจเลือดก่อนผ่าตัด',
    subjective: 'ตรวจเลือดก่อนนัดผ่าตัดทำหมันสัปดาห์หน้า สุขภาพดี กินได้ ขับถ่ายปกติ',
    objective: 'ร่าเริง BCS 5/9 หัวใจและปอดปกติ เจาะเลือดจากเส้นเลือดขาหน้า',
    assessment: 'พร้อมสำหรับการผ่าตัด รอยืนยันผลเลือด',
    plan: 'งดน้ำงดอาหาร 8 ชั่วโมงก่อนวันผ่าตัด แจ้งผลเลือดทางโทรศัพท์',
    diagnoses: [{ description: 'ตรวจประเมินก่อนการผ่าตัด', code: 'Z01.8' }],
    findings: [],
    treatments: [['ตรวจความสมบูรณ์ของเม็ดเลือด (CBC)', 1, 450]],
    drugs: [],
    lab: { tests: [{ name: 'CBC', code: 'CBC' }] },
  },
  {
    key: 'renal-recheck',
    species: ['dog', 'cat'],
    services: /ตับและไต/,
    reasons: /ค่าไต/,
    complaint: 'ตรวจค่าไตติดตามผล',
    subjective: 'ติดตามค่าไตหลังเปลี่ยนเป็นอาหารสูตรโรคไต 1 เดือน กินอาหารได้ดีขึ้น ปัสสาวะยังมาก',
    objective: 'น้ำหนักคงที่ ขาดน้ำเล็กน้อย ขนดีขึ้น ความดันโลหิต 150 mmHg',
    assessment: 'ไตวายเรื้อรังระยะที่ 2 (IRIS stage 2) ควบคุมได้',
    plan: 'ให้อาหารสูตรโรคไตต่อ เพิ่มน้ำด้วยอาหารเปียก ตรวจค่าไตซ้ำทุก 3 เดือน',
    diagnoses: [{ description: 'ไตวายเรื้อรัง IRIS ระยะที่ 2', code: 'N18.2' }],
    findings: [['ระบบทางเดินอาหาร', false, 'ขาดน้ำเล็กน้อย']],
    treatments: [['ตรวจค่าตับและไต', 1, 900]],
    drugs: [['อาหารสูตรโรคไต Renal', 'ตามฉลาก', 'แทนอาหารเดิม', 30, 2]],
    lab: { tests: [{ name: 'เคมีเลือด', code: 'CHEM10' }], abnormal: { BUN: 34, CREA: 2.3 } },
  },
  {
    key: 'recheck',
    species: ['dog', 'cat'],
    reasons: /ติดตาม/,
    complaint: 'ตรวจติดตามอาการ',
    subjective: 'มาตรวจติดตามตามนัด อาการดีขึ้นมาก กินยาครบตามสั่ง กินได้ ร่าเริง',
    objective: 'ร่าเริง ผิวหนังที่เคยอักเสบหายดี ไม่พบตุ่มใหม่ หัวใจและปอดปกติ',
    assessment: 'อาการหายดี ตอบสนองต่อการรักษา',
    plan: 'หยุดยาได้ ดูแลตามคำแนะนำเดิม ถ้ามีอาการซ้ำให้กลับมา',
    diagnoses: [{ description: 'ตรวจติดตามหลังการรักษา อาการหายดี', code: 'Z09' }],
    findings: [],
    treatments: [['ตรวจร่างกายทั่วไป', 1, 300]],
    drugs: [],
  },
];

/** The illnesses a visit with no telling service is drawn from, per species. */
export const SICK_VISITS: Record<Species, string[]> = {
  dog: ['skin', 'ear', 'gastro', 'lame', 'kennel-cough', 'eye', 'tick-fever'],
  cat: ['uri', 'flutd', 'gastro', 'cat-skin', 'eye', 'kidney'],
  other: ['rabbit-gi'],
};

export function caseByKey(key: string): CaseSpec {
  return CASES.find((spec) => spec.key === key)!;
}

/** The vaccine's notes for a visit where one was given: the case's plan with the vaccine named. */
export function vaccinePlan(vaccines: string[]): string {
  return `ฉีด${vaccines.join(' และ ')} สังเกตอาการแพ้ 30 นาทีที่คลินิก งดอาบน้ำ 3 วัน นัดเข็มถัดไปตามสมุดวัคซีน`;
}
