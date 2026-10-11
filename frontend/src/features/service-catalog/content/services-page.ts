import type { DotColor } from '@/components/site/dot-chip';

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

/**
 * What the banner's empty picture slot says when the banner has no picture. The banner's words,
 * buttons and picture are in `features/site-banners/content/page-banners.ts`, under `services`.
 */
export const servicesBannerSlotLabel = 'รูป banner (สัตวแพทย์ตรวจสุนัข)';

export const servicesPage = {
  navLabel: 'ไปที่หมวด',
  /** The API could not be reached. */
  unavailable: 'ยังโหลดรายการบริการไม่ได้ ลองใหม่อีกครั้งภายหลัง หรือโทรสอบถามคลินิก',
  /** The clinic has not opened any service for online booking. */
  empty: 'ยังไม่มีบริการที่เปิดให้จองออนไลน์ โทรสอบถามหรือนัดผ่านคลินิกได้',
  contactLink: { label: 'ดูช่องทางติดต่อ', href: '/contact' },
  /** At the right of a category's heading. */
  serviceCount: (count: number) => `${count} บริการ`,
  /** At the right of a category with nothing to book online. */
  askByPhone: 'โทรสอบถาม',
  /** Read out after "โทรสอบถาม", so each category's link says what it is about. */
  askAbout: (category: string) => `เรื่อง${category}`,
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

/** The dot on each category's chip in the bar under the banner. A category missing here gets `brand`. */
export const categoryDots: Partial<Record<string, DotColor>> = {
  checkup: 'teal',
  vaccine: 'orange',
  dental: 'violet',
  surgery: 'alert',
  lab: 'brand',
  grooming: 'sun',
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

/** One line under the last category: what the prices mean, and what is not listed. */
export const servicesNote =
  'ราคาที่แสดงเป็นราคาตั้งต้น ค่าใช้จ่ายจริงขึ้นกับการตรวจ ยา และการรักษาที่น้องได้รับ หน้านี้แสดงเฉพาะบริการที่จองออนไลน์ได้';

/** The orange box that closes the page. */
export const servicesCta = {
  heading: 'ไม่แน่ใจว่าน้องต้องใช้บริการไหน',
  lead: 'ทักแชทหรือโทรถามคลินิกได้ เราช่วยเลือกบริการและเวลาที่เหมาะให้',
  line: 'ทักแชท LINE',
  call: (number: string) => `โทร ${number}`,
  /** When the clinic has set neither a phone nor a LINE account. */
  fallback: { label: 'ดูช่องทางติดต่อ', href: '/contact' },
};
