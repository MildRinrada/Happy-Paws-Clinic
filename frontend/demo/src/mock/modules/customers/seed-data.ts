/**
 * The sample register: who the clinic's customers are and what animals they bring. Written out by
 * hand so the names, breeds and notes read like a real Thai clinic's, not like generated filler.
 *
 * Customer flags: `p` has a portal account, `l` LINE linked, `u` linked but unfollowed since,
 * `i` invited to the portal, `e` invitation expired, `x` inactive.
 */

export interface SeedCustomer {
  key: string;
  name: string;
  phone: string;
  email?: string;
  flags?: string;
  line?: string;
  /** Days ago the record was made. */
  since: number;
}

export interface SeedPet {
  owner: string;
  name: string;
  species: string;
  breed?: string;
  sex?: 'male' | 'female' | 'unknown';
  /** Age in days at today; absent when nobody knows. */
  age?: number;
  estimated?: boolean;
  neutered?: boolean;
  chip?: string;
  nickname?: string;
  diet?: string;
  behaviour?: string;
  /** Current weight in kg; the seed builds a history that ends there. */
  kg?: number;
  allergies?: Array<[substance: string, severity: 'mild' | 'moderate' | 'severe', reaction?: string]>;
  deceasedDaysAgo?: number;
  /** Extra carers, by customer key; `true` may read the history. */
  carers?: Array<[key: string, mayView: boolean]>;
  photo?: string;
}

export const DEMO_CUSTOMER_KEY = 'napa';

export const SEED_CUSTOMERS: SeedCustomer[] = [
  { key: 'napa', name: 'คุณนภา ใจงาม', phone: '0812345678', email: 'napa@example.com', flags: 'pl', line: 'Napa J.', since: 820 },
  { key: 'thana', name: 'ธนา ใจงาม', phone: '0812345679', flags: 'i', since: 600 },
  { key: 'somchai', name: 'สมชาย แก้วดี', phone: '0891112233', email: 'somchai.k@gmail.com', flags: 'l', line: 'Somchai K.', since: 1100 },
  { key: 'somchai2', name: 'คุณสมชาย แก้วดี', phone: '0891112233', since: 40 },
  { key: 'pimchanok', name: 'พิมพ์ชนก ศรีวงศ์', phone: '0863214567', email: 'pimchanok.s@hotmail.com', flags: 'pl', line: 'Pim', since: 760 },
  { key: 'wichai', name: 'วิชัย ทองคำ', phone: '0819876543', since: 1300 },
  { key: 'kanokwan', name: 'กนกวรรณ บุญมา', phone: '0952223344', email: 'kanokwan.b@gmail.com', flags: 'p', since: 540 },
  { key: 'arthit', name: 'อาทิตย์ พงษ์ไพบูลย์', phone: '0841239876', flags: 'lu', line: 'Arthit', since: 980 },
  { key: 'siriwan', name: 'ศิริวรรณ จันทร์เพ็ญ', phone: '0897654321', email: 'siriwan.j@yahoo.com', flags: 'i', since: 120 },
  { key: 'prasert', name: 'ประเสริฐ วงศ์สวัสดิ์', phone: '0816543210', since: 1450 },
  { key: 'jiraporn', name: 'จิราพร แสงทอง', phone: '0928887766', email: 'jiraporn.st@gmail.com', flags: 'pl', line: 'Jira', since: 430 },
  { key: 'nattapong', name: 'ณัฐพงษ์ ศักดิ์สิทธิ์', phone: '0634567890', since: 300 },
  { key: 'malee', name: 'มาลี อินทร์แก้ว', phone: '0871112299', flags: 'e', since: 700 },
  { key: 'teerawat', name: 'ธีรวัฒน์ เจริญสุข', phone: '0985554433', email: 'teerawat.c@outlook.com', flags: 'p', since: 260 },
  { key: 'sasithorn', name: 'ศศิธร มณีรัตน์', phone: '0823334455', email: 'sasithorn.m@gmail.com', flags: 'l', line: 'Sasi', since: 650 },
  { key: 'kittisak', name: 'กิตติศักดิ์ ธรรมรักษ์', phone: '0856667788', since: 880 },
  { key: 'pornthip', name: 'พรทิพย์ สุขเจริญ', phone: '0819990011', flags: 'i', since: 55 },
  { key: 'anucha', name: 'อนุชา รัตนพันธ์', phone: '0948765432', since: 1020 },
  { key: 'wanida', name: 'วนิดา พรหมมา', phone: '0861234455', email: 'wanida.p@gmail.com', flags: 'p', since: 390 },
  { key: 'chaiwat', name: 'ชัยวัฒน์ ลิ้มเจริญ', phone: '0837778899', since: 510 },
  { key: 'rattana', name: 'รัตนา คงสมบูรณ์', phone: '0894443322', flags: 'l', line: 'Rattana', since: 1180 },
  { key: 'supachai', name: 'ศุภชัย นาคสวัสดิ์', phone: '0652221100', email: 'supachai.n@gmail.com', since: 210 },
  { key: 'orapin', name: 'อรพิน ทรัพย์มาก', phone: '0815556677', flags: 'x', since: 1600 },
  { key: 'panupong', name: 'ภานุพงศ์ อุดมสุข', phone: '0927778811', since: 160 },
  { key: 'duangjai', name: 'ดวงใจ เพชรรัตน์', phone: '0868889900', email: 'duangjai.p@gmail.com', flags: 'p', since: 470 },
  { key: 'somsak', name: 'สมศักดิ์ บุญเรือง', phone: '0813332211', since: 1250 },
  { key: 'yupa', name: 'ยุพา ศรีสุวรรณ', phone: '0899998877', flags: 'l', line: 'Yupa S.', since: 830 },
  { key: 'krit', name: 'กฤษณ์ วัฒนกุล', phone: '0641112233', email: 'krit.w@gmail.com', since: 95 },
  { key: 'nantana', name: 'นันทนา ปัญญาดี', phone: '0874445566', since: 610 },
  { key: 'thitima', name: 'ฐิติมา ชัยมงคล', phone: '0953332244', email: 'thitima.c@gmail.com', flags: 'pl', line: 'Thitima', since: 340 },
  { key: 'boonmee', name: 'บุญมี สายทอง', phone: '0817776655', since: 1380 },
  { key: 'chanida', name: 'ชนิดา ภู่ระหงษ์', phone: '0825556699', flags: 'i', since: 20 },
  { key: 'worawut', name: 'วรวุฒิ ประทุมทอง', phone: '0866665544', since: 720 },
  { key: 'achara', name: 'อัจฉรา ทองประเสริฐ', phone: '0931234567', email: 'achara.t@gmail.com', since: 450 },
  { key: 'sompong', name: 'สมปอง ดีมาก', phone: '0812223344', since: 940 },
  { key: 'lalita', name: 'ลลิตา วิเศษสุข', phone: '0897771234', email: 'lalita.w@icloud.com', flags: 'p', since: 75 },
  { key: 'pongsak', name: 'พงศ์ศักดิ์ เรืองศรี', phone: '0843216789', since: 560 },
  { key: 'nuchjaree', name: 'นุชจรีย์ แก้วมณี', phone: '0628889911', flags: 'l', line: 'Nuch', since: 280 },
  { key: 'ekkachai', name: 'เอกชัย ศรีประเสริฐ', phone: '0819993344', since: 12 },
  { key: 'jintana', name: 'จินตนา บัวทอง', phone: '0874441199', email: 'jintana.b@gmail.com', since: 5 },
  { key: 'piyawat', name: 'ปิยวัฒน์ หอมจันทร์', phone: '0956781234', since: 2 },
];

const Y = 365;

export const SEED_PETS: SeedPet[] = [
  // The demo customer's three, with the richest histories.
  { owner: 'napa', name: 'โมจิ', species: 'สุนัข', breed: 'ชิบะ อินุ', sex: 'male', age: 3 * Y + 40, neutered: true, chip: '764098100123456', kg: 10.4, diet: 'อาหารเม็ดสูตรปลาแซลมอน แพ้ไก่', behaviour: 'ตื่นเต้นง่าย ชอบดิ้นตอนตัดเล็บ', allergies: [['เพนิซิลลิน (Penicillin)', 'severe', 'หน้าบวม ผื่นลมพิษ หายใจเร็ว'], ['เนื้อไก่', 'mild', 'คันผิวหนังบริเวณอุ้งเท้า']], carers: [['thana', true]], photo: 'shiba' },
  { owner: 'napa', name: 'ส้มจี๊ด', species: 'แมว', breed: 'ไทยวิเชียรมาศผสม', sex: 'female', age: 2 * Y + 100, neutered: true, chip: '764098100654321', kg: 4.2, nickname: 'จี๊ด', behaviour: 'กลัวคนแปลกหน้า ควรจับในผ้าขนหนู', allergies: [['เมโลซิแคม (Meloxicam)', 'moderate', 'อาเจียนหลังได้ยา']], photo: 'orange' },
  { owner: 'napa', name: 'ขนมปัง', species: 'กระต่าย', breed: 'ฮอลแลนด์ ลอป', sex: 'female', age: 1 * Y + 60, neutered: false, kg: 1.8, diet: 'หญ้าทิมโมธี ผักใบเขียว', photo: 'rabbit' },

  { owner: 'thana', name: 'ลัคกี้', species: 'สุนัข', breed: 'โกลเด้น รีทรีฟเวอร์', sex: 'male', age: 6 * Y, neutered: true, chip: '764098100777001', kg: 32.5 },
  { owner: 'somchai', name: 'บราวนี่', species: 'สุนัข', breed: 'พุดเดิ้ล', sex: 'female', age: 5 * Y + 120, neutered: true, chip: '764098100777002', kg: 6.1, allergies: [['ไอเวอร์เมกติน (Ivermectin)', 'severe', 'ซึม เดินเซ']] },
  { owner: 'somchai', name: 'ไข่ตุ๋น', species: 'แมว', breed: 'เปอร์เซีย', sex: 'male', age: 8 * Y, neutered: true, kg: 5.4, behaviour: 'มีประวัติโรคไตเรื้อรัง ระวังยาที่ขับทางไต' },
  { owner: 'somchai2', name: 'ข้าวตัง', species: 'สุนัข', breed: 'ไทยหลังอาน', sex: 'male', age: 2 * Y, estimated: true, kg: 18.2 },
  { owner: 'pimchanok', name: 'มะลิ', species: 'แมว', breed: 'สก็อตติช โฟลด์', sex: 'female', age: 3 * Y, neutered: true, chip: '764098100777003', kg: 3.6 },
  { owner: 'pimchanok', name: 'ทองหยอด', species: 'แมว', breed: 'บริติช ช็อตแฮร์', sex: 'male', age: 4 * Y + 200, neutered: true, kg: 6.3 },
  { owner: 'wichai', name: 'ร็อคกี้', species: 'สุนัข', breed: 'ร็อตไวเลอร์', sex: 'male', age: 7 * Y, neutered: false, chip: '764098100777004', kg: 44.0, behaviour: 'ดุกับสุนัขตัวอื่น ต้องใส่ตะกร้อปากระหว่างรอ' },
  { owner: 'kanokwan', name: 'ปุยฝ้าย', species: 'สุนัข', breed: 'ปอมเมอเรเนียน', sex: 'female', age: 2 * Y + 30, neutered: true, kg: 2.9 },
  { owner: 'kanokwan', name: 'โกโก้', species: 'สุนัข', breed: 'ชิวาวา', sex: 'male', age: 9 * Y, neutered: true, kg: 2.4, allergies: [['วัคซีนเลปโตสไปโรซิส', 'moderate', 'บวมบริเวณที่ฉีด มีไข้']] },
  { owner: 'arthit', name: 'แม็กซ์', species: 'สุนัข', breed: 'ไซบีเรียน ฮัสกี้', sex: 'male', age: 4 * Y, neutered: true, chip: '764098100777005', kg: 24.8 },
  { owner: 'siriwan', name: 'น้ำแข็ง', species: 'แมว', breed: 'แมวไทย (ขาวมณี)', sex: 'female', age: 1 * Y, neutered: false, kg: 3.1 },
  { owner: 'prasert', name: 'ซาโตชิ', species: 'สุนัข', breed: 'ชิสุ', sex: 'male', age: 11 * Y, neutered: true, kg: 7.2, behaviour: 'ตามองไม่ค่อยเห็น ค่อย ๆ เข้าหา', diet: 'อาหารสูตรโรคหัวใจ' },
  { owner: 'prasert', name: 'เหมียว', species: 'แมว', breed: 'แมวบ้าน', sex: 'female', age: 15 * Y, estimated: true, neutered: true, kg: 3.0, deceasedDaysAgo: 64 },
  { owner: 'jiraporn', name: 'บัตเตอร์', species: 'สุนัข', breed: 'คอร์กี้', sex: 'female', age: 3 * Y + 200, neutered: true, chip: '764098100777006', kg: 12.1, carers: [['nattapong', false]] },
  { owner: 'jiraporn', name: 'ชีส', species: 'แมว', breed: 'อเมริกัน ช็อตแฮร์', sex: 'male', age: 2 * Y, neutered: true, kg: 5.0 },
  { owner: 'nattapong', name: 'ทอฟฟี่', species: 'สุนัข', breed: 'ลาบราดอร์ รีทรีฟเวอร์', sex: 'male', age: 1 * Y + 150, neutered: false, kg: 27.3 },
  { owner: 'malee', name: 'ดำ', species: 'แมว', breed: 'แมวบ้าน', sex: 'male', age: 6 * Y, estimated: true, neutered: true, kg: 4.8 },
  { owner: 'teerawat', name: 'ซิมบ้า', species: 'แมว', breed: 'เมนคูน', sex: 'male', age: 3 * Y, neutered: true, chip: '764098100777007', kg: 8.4 },
  { owner: 'teerawat', name: 'ปีโป้', species: 'นก', breed: 'นกแก้วค็อกคาเทล', sex: 'unknown', age: 2 * Y, kg: 0.09 },
  { owner: 'sasithorn', name: 'ลูน่า', species: 'สุนัข', breed: 'บีเกิ้ล', sex: 'female', age: 5 * Y, neutered: true, chip: '764098100777008', kg: 11.7, diet: 'ควบคุมน้ำหนัก' },
  { owner: 'sasithorn', name: 'หมูแดง', species: 'สุนัข', breed: 'เฟรนช์ บูลด็อก', sex: 'male', age: 2 * Y + 90, neutered: false, kg: 11.9, behaviour: 'หายใจลำบากเมื่ออากาศร้อน ระวังการวางยาสลบ' },
  { owner: 'kittisak', name: 'ไทเกอร์', species: 'สุนัข', breed: 'บางแก้ว', sex: 'male', age: 4 * Y, neutered: true, kg: 21.0 },
  { owner: 'pornthip', name: 'มูมู่', species: 'แมว', breed: 'แร็กดอลล์', sex: 'female', age: 140, neutered: false, kg: 1.6 },
  { owner: 'anucha', name: 'บิ๊กบอส', species: 'สุนัข', breed: 'บูลเทอร์เรีย', sex: 'male', age: 6 * Y, neutered: true, kg: 26.4 },
  { owner: 'wanida', name: 'โบว์', species: 'สุนัข', breed: 'ยอร์คเชียร์ เทอร์เรีย', sex: 'female', age: 7 * Y, neutered: true, kg: 3.2, allergies: [['ยาซัลฟา (Sulfonamides)', 'moderate', 'ผื่นแดงทั่วตัว']] },
  { owner: 'wanida', name: 'โอริโอ้', species: 'กระต่าย', breed: 'เนเธอร์แลนด์ ดวาร์ฟ', sex: 'male', age: 2 * Y, neutered: true, kg: 1.1 },
  { owner: 'chaiwat', name: 'จูเนียร์', species: 'สุนัข', breed: 'ไทยหลังอาน', sex: 'male', age: 3 * Y, neutered: true, chip: '764098100777009', kg: 19.6 },
  { owner: 'rattana', name: 'ส้มโอ', species: 'แมว', breed: 'แมวส้ม', sex: 'male', age: 5 * Y, neutered: true, kg: 6.7, diet: 'อาหารเม็ดสูตรป้องกันนิ่ว' },
  { owner: 'rattana', name: 'เต้าหู้', species: 'แมว', breed: 'แมวบ้าน', sex: 'female', age: 3 * Y, neutered: true, kg: 3.9 },
  { owner: 'rattana', name: 'ถั่วเขียว', species: 'เต่า', breed: 'เต่าซูคาต้า', sex: 'unknown', age: 4 * Y, estimated: true, kg: 6.2 },
  { owner: 'supachai', name: 'ชิโร่', species: 'สุนัข', breed: 'ซามอยด์', sex: 'male', age: 2 * Y + 50, neutered: false, kg: 23.1 },
  { owner: 'orapin', name: 'เจ้าขาว', species: 'สุนัข', breed: 'สปิตซ์', sex: 'female', age: 12 * Y, neutered: true, kg: 8.0 },
  { owner: 'panupong', name: 'แพนด้า', species: 'หนูแฮมสเตอร์', breed: 'ไซเรียน', sex: 'male', age: 300, kg: 0.12 },
  { owner: 'panupong', name: 'คุกกี้', species: 'สุนัข', breed: 'ดัชชุน', sex: 'female', age: 3 * Y, neutered: true, kg: 6.8, behaviour: 'มีประวัติหมอนรองกระดูกเสื่อม ห้ามอุ้มแบบห้อยตัว' },
  { owner: 'duangjai', name: 'นมสด', species: 'แมว', breed: 'เปอร์เซีย', sex: 'female', age: 6 * Y, neutered: true, chip: '764098100777010', kg: 4.4 },
  { owner: 'duangjai', name: 'ช็อกโกแลต', species: 'สุนัข', breed: 'พุดเดิ้ล ทอย', sex: 'male', age: 4 * Y, neutered: true, kg: 3.8 },
  { owner: 'somsak', name: 'โจ้', species: 'สุนัข', breed: 'เยอรมัน เชพเพิร์ด', sex: 'male', age: 8 * Y, neutered: false, chip: '764098100777011', kg: 36.2, behaviour: 'ข้อสะโพกเสื่อม เดินช้า' },
  { owner: 'yupa', name: 'น้องเปา', species: 'แมว', breed: 'สฟิงซ์', sex: 'male', age: 2 * Y, neutered: true, kg: 3.7 },
  { owner: 'yupa', name: 'ข้าวหอม', species: 'สุนัข', breed: 'มอลทีส', sex: 'female', age: 1 * Y + 200, neutered: true, kg: 3.3 },
  { owner: 'krit', name: 'แบล็คกี้', species: 'แมว', breed: 'บอมเบย์', sex: 'male', age: 1 * Y, neutered: false, kg: 4.1 },
  { owner: 'nantana', name: 'ซูชิ', species: 'สุนัข', breed: 'ชิบะ อินุ', sex: 'female', age: 4 * Y, neutered: true, kg: 8.9 },
  { owner: 'thitima', name: 'มินนี่', species: 'สุนัข', breed: 'ชิวาวา', sex: 'female', age: 5 * Y, neutered: true, kg: 2.1, allergies: [['เซฟาเลกซิน (Cephalexin)', 'mild', 'ถ่ายเหลว']] },
  { owner: 'thitima', name: 'มิกกี้', species: 'สุนัข', breed: 'ชิวาวา', sex: 'male', age: 5 * Y, neutered: true, kg: 2.6, carers: [['krit', true]] },
  { owner: 'boonmee', name: 'เจ้าตูบ', species: 'สุนัข', breed: 'พันธุ์ผสม', sex: 'male', age: 9 * Y, estimated: true, neutered: true, kg: 15.5 },
  { owner: 'chanida', name: 'ฟักทอง', species: 'แมว', breed: 'แมวส้ม', sex: 'female', age: 90, kg: 0.9 },
  { owner: 'worawut', name: 'ไอรอน', species: 'สุนัข', breed: 'โดเบอร์แมน', sex: 'male', age: 5 * Y, neutered: true, chip: '764098100777012', kg: 38.0 },
  { owner: 'achara', name: 'เจลลี่', species: 'แมว', breed: 'สก็อตติช โฟลด์', sex: 'female', age: 2 * Y, neutered: true, kg: 3.4 },
  { owner: 'achara', name: 'บลูเบอร์รี่', species: 'นก', breed: 'นกหงส์หยก', sex: 'unknown', age: 1 * Y, kg: 0.04 },
  { owner: 'sompong', name: 'หลาน', species: 'สุนัข', breed: 'ไทยบางแก้ว', sex: 'female', age: 6 * Y, neutered: true, kg: 17.3 },
  { owner: 'lalita', name: 'พุดดิ้ง', species: 'สุนัข', breed: 'ปอมเมอเรเนียน', sex: 'male', age: 1 * Y + 20, neutered: false, kg: 2.7 },
  { owner: 'lalita', name: 'วาฟเฟิล', species: 'แมว', breed: 'เบงกอล', sex: 'male', age: 2 * Y, neutered: true, chip: '764098100777013', kg: 5.6 },
  { owner: 'pongsak', name: 'ต้นกล้า', species: 'สุนัข', breed: 'บอร์เดอร์ คอลลี่', sex: 'male', age: 3 * Y, neutered: true, kg: 19.0 },
  { owner: 'nuchjaree', name: 'มะม่วง', species: 'แมว', breed: 'แมวบ้าน', sex: 'female', age: 4 * Y, estimated: true, neutered: true, kg: 4.0 },
  { owner: 'nuchjaree', name: 'ถุงทอง', species: 'สุนัข', breed: 'ชิสุ', sex: 'female', age: 7 * Y, neutered: true, kg: 6.4 },
  { owner: 'ekkachai', name: 'เสือ', species: 'แมว', breed: 'แมวลายสลิด', sex: 'male', age: 1 * Y + 100, neutered: false, kg: 4.6 },
  { owner: 'jintana', name: 'ปลาทู', species: 'แมว', breed: 'แมวบ้าน', sex: 'female', age: 200, estimated: true, neutered: false, kg: 2.2 },
  { owner: 'piyawat', name: 'บอนบอน', species: 'สุนัข', breed: 'มินิเจอร์ ชเนาเซอร์', sex: 'male', age: 120, neutered: false, kg: 3.4 },
];

/** Vaccines by species, with how long each protects. */
export const VACCINES: Record<string, Array<{ name: string; manufacturer: string; days: number }>> = {
  สุนัข: [
    { name: 'วัคซีนรวม DHPPi+L', manufacturer: 'Boehringer Ingelheim', days: 365 },
    { name: 'วัคซีนพิษสุนัขบ้า (Rabies)', manufacturer: 'Virbac', days: 365 },
  ],
  แมว: [
    { name: 'วัคซีนรวมแมว FVRCP', manufacturer: 'Zoetis', days: 365 },
    { name: 'วัคซีนพิษสุนัขบ้า (Rabies)', manufacturer: 'Virbac', days: 365 },
  ],
  กระต่าย: [{ name: 'วัคซีน RHDV2', manufacturer: 'Filavie', days: 365 }],
};
