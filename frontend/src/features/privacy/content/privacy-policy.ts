/**
 * The words around the privacy notice on the public page. The notice itself is not here: the
 * clinic writes and publishes it in the console (`/settings/privacy-policy`), and the page reads
 * the published version from the API (docs/contract-extensions.md § 35).
 *
 * The text that used to be hard-coded here was shown as version `2026-10`, and consents recorded
 * before § 35 carry that label. It is in git history at this path; see
 * `docs/features/privacy-and-consent.md` for how to show it if a customer asks.
 *
 * Who the clinic is (name, operator, licence, contacts) is read from the API (`GET /clinic`,
 * `GET /clinic/contacts`), so it follows the clinic's own settings.
 */

export const privacyMeta = {
  title: 'นโยบายความเป็นส่วนตัว | Happy Paws Clinic',
  description: 'คลินิกเก็บข้อมูลอะไรของคุณและสัตว์เลี้ยง ใช้ทำอะไร ส่งให้ใคร เก็บนานแค่ไหน และคุณใช้สิทธิ์ตาม PDPA ได้อย่างไร',
};

export const privacyHero = {
  eyebrow: 'ความเป็นส่วนตัว',
  heading: 'นโยบายความเป็นส่วนตัว',
  lead: 'คลินิกเก็บข้อมูลของคุณและน้อง ๆ เท่าที่ต้องใช้ดูแลรักษาและติดต่อคุณ หน้านี้บอกตรง ๆ ว่าเก็บอะไร ใช้ทำอะไร ส่งให้ใคร และคุณขอดู แก้ หรือลบได้อย่างไร',
  effective: (date: string, version: string) => `มีผลตั้งแต่ ${date} (ฉบับ ${version})`,
};

/** Shown while the published version is not marked as reviewed by a lawyer. */
export const reviewNotice = {
  title: 'ประกาศนี้ยังไม่ได้ผ่านการตรวจของนักกฎหมาย',
  body: 'คลินิกเผยแพร่ประกาศนี้ก่อนให้ทนายความหรือที่ปรึกษาด้านกฎหมายคุ้มครองข้อมูลส่วนบุคคลตรวจทาน ถ้อยคำอาจเปลี่ยนเมื่อตรวจแล้ว และคลินิกจะเผยแพร่เป็นฉบับใหม่',
};

/** The page when the clinic has not published a notice yet. Never the template. */
export const notPublished = {
  title: 'คลินิกยังไม่ได้เผยแพร่ประกาศความเป็นส่วนตัว',
  body: 'คลินิกกำลังจัดทำประกาศความเป็นส่วนตัวของตัวเอง ระหว่างนี้ถ้าอยากรู้ว่าคลินิกเก็บหรือใช้ข้อมูลของคุณอย่างไร หรืออยากขอดู แก้ไข หรือลบข้อมูล ติดต่อคลินิกได้ตามช่องทางด้านล่าง',
  account: { label: 'จัดการข้อมูลและความยินยอมของฉัน', href: '/portal/account' },
};

/** The list of published versions under the notice. */
export const historyText = {
  heading: 'ฉบับที่ผ่านมา',
  lead: 'ประกาศแต่ละฉบับเปิดอ่านได้ตลอด ความยินยอมที่คุณให้ถูกบันทึกพร้อมเลขฉบับที่มีผลอยู่ตอนนั้น',
  current: 'ฉบับปัจจุบัน',
  entry: (label: string, date: string) => `ฉบับ ${label} มีผลตั้งแต่ ${date}`,
};

/** The banner on an older version's page. */
export const oldVersionNotice = {
  title: (label: string) => `นี่คือประกาศฉบับ ${label} ซึ่งไม่ใช่ฉบับปัจจุบัน`,
  body: 'คลินิกเก็บฉบับนี้ไว้ให้อ่านย้อนหลัง ข้อมูลของคุณตอนนี้อยู่ภายใต้ฉบับปัจจุบัน',
  link: 'อ่านฉบับปัจจุบัน',
  metaTitle: (label: string) => `นโยบายความเป็นส่วนตัว ฉบับ ${label} | Happy Paws Clinic`,
};

export const tocLabel = 'หัวข้อในหน้านี้';

/** The first section. Its details come from the API; only the labels and fallbacks are here. */
export const controllerSection = {
  id: 'controller',
  heading: 'ใครดูแลข้อมูลของคุณ',
  lead: 'ผู้ควบคุมข้อมูลส่วนบุคคลตาม พ.ร.บ.คุ้มครองข้อมูลส่วนบุคคล พ.ศ. 2562 (PDPA) คือคลินิก ไม่ใช่ผู้พัฒนาระบบ คลินิกเป็นผู้ตัดสินว่าจะใช้ข้อมูลของคุณอย่างไร และเป็นผู้ตอบคำขอของคุณ',
  labels: {
    clinic: 'คลินิก',
    operator: 'ผู้ดำเนินการสถานพยาบาล',
    license: 'ใบอนุญาตประกอบกิจการสถานพยาบาลสัตว์',
    address: 'ที่อยู่',
  },
  contactLead: 'ติดต่อเรื่องข้อมูลส่วนบุคคลได้ทางช่องทางเหล่านี้ คลินิกยังไม่ได้แต่งตั้งเจ้าหน้าที่คุ้มครองข้อมูลส่วนบุคคล (DPO) แยก',
  unavailable: 'ตอนนี้ยังโหลดข้อมูลคลินิกไม่ได้ ดูช่องทางติดต่อได้ที่หน้าติดต่อเรา',
  contactPage: { label: 'ช่องทางติดต่อทั้งหมด', href: '/contact' },
};

/** When the notice itself cannot be read: the API is away. */
export const policyUnavailable = 'ตอนนี้ยังโหลดประกาศความเป็นส่วนตัวไม่ได้ ลองโหลดหน้านี้อีกครั้งภายหลัง หรือติดต่อคลินิกตามช่องทางด้านบน';
