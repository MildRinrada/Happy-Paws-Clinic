import type { HeroAction } from '@/features/home/types/home.types';

/**
 * Everything the public "บริการและราคา" page says that does not come from the API. The services,
 * their categories, durations and prices are the clinic's own, edited in the staff console under
 * ตั้งค่า > บริการและราคา.
 */

export const servicesMeta = {
  title: 'บริการและราคา | Happy Paws Clinic',
  description:
    'ราคาและระยะเวลาของบริการตรวจสุขภาพ วัคซีน ทันตกรรม ผ่าตัด แล็บ และกรูมมิ่ง ดูราคาก่อนมา แล้วเลือกจองบริการที่ต้องการได้ทันที',
};

export const servicesHero = {
  eyebrow: 'บริการและราคา',
  heading: 'รู้ราคาก่อนมา เลือกบริการแล้วจองได้เลย',
  lead: 'ทุกบริการที่จองออนไลน์ได้ พร้อมราคาและเวลาที่ใช้ กดจองที่บริการไหนก็ได้ ระบบจะเลือกบริการนั้นไว้ให้',
};

export const servicesPage = {
  navLabel: 'ไปที่หมวด',
  /** The API could not be reached. */
  unavailable: 'ยังโหลดรายการบริการไม่ได้ ลองใหม่อีกครั้งภายหลัง หรือโทรสอบถามคลินิก',
  /** The clinic has not opened any service for online booking. */
  empty: 'ยังไม่มีบริการที่เปิดให้จองออนไลน์ โทรสอบถามหรือนัดผ่านคลินิกได้',
  contactLink: { label: 'ดูช่องทางติดต่อ', href: '/contact' },
};

/** One line under a category's name. A category missing here simply shows no line. */
export const categoryBlurbs: Partial<Record<string, string>> = {
  checkup: 'ตรวจร่างกาย ซักประวัติ และคุยแผนดูแลกับสัตวแพทย์',
  vaccine: 'วัคซีนและการป้องกันโรคตามช่วงวัยของน้อง',
  dental: 'ดูแลช่องปากและฟัน',
  surgery: 'การผ่าตัดและการทำหมัน',
  lab: 'ตรวจเลือด ตรวจปัสสาวะ และแล็บอื่น ๆ',
  grooming: 'อาบน้ำ ตัดขน และดูแลความสะอาด',
};

export const serviceCard = {
  book: 'จองบริการนี้',
  /** Where "จองบริการนี้" goes: the booking page with this service already chosen. */
  bookHref: (serviceId: string) => `/book?service=${encodeURIComponent(serviceId)}`,
  startingAt: 'เริ่มต้น',
  /** A price of zero: the clinic has not put a price on the menu. */
  askPrice: 'สอบถามราคา',
  tiersHeading: 'ราคาตามชนิดและน้ำหนัก',
  otherwise: 'นอกเหนือจากนี้',
  branchesOnly: 'มีที่',
};

/** Under the menu: what the prices mean, and what to do about a service not listed. */
export const servicesNote = {
  heading: 'เรื่องราคา',
  lines: [
    'ราคาที่แสดงเป็นราคาตั้งของบริการ ค่าใช้จ่ายจริงขึ้นกับการตรวจ ยา และการรักษาที่น้องได้รับ',
    'หน้านี้แสดงเฉพาะบริการที่จองออนไลน์ได้ บริการอื่นสอบถามหรือนัดผ่านคลินิกได้',
  ],
};

/** The orange band that closes the page. */
export const servicesCta = {
  heading: 'ไม่แน่ใจว่าน้องต้องใช้บริการไหน?',
  lead: 'ติดต่อคลินิกให้ช่วยเลือก หรือจองนัดไว้ก่อนแล้วคุยกับสัตวแพทย์ที่คลินิก',
  primaryAction: { label: 'จองนัดหมาย', href: '/book' } satisfies HeroAction,
  secondaryAction: { label: 'ติดต่อคลินิก', href: '/contact' } satisfies HeroAction,
};
