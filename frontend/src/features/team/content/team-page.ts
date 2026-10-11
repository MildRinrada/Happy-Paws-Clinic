import type { DepartmentCode } from '../types/team.types';

/**
 * Everything the team page says that does not come from the API. Edit the wording here. The
 * banner's words and button are in `features/site-banners/content/page-banners.ts`, under `team`.
 */
export const teamPage = {
  /** The tab's title and Google's description. */
  title: 'ทีมของเรา',
  intro: 'ทุกคนที่ดูแลน้องของคุณ ตั้งแต่เคาน์เตอร์ต้อนรับ ห้องตรวจ จนถึงวันที่น้องได้กลับบ้าน',
  /** What the banner's empty picture slot says while there is no picture of the team. */
  bannerSlotLabel: 'รูปทีมทั้งหมดหน้าคลินิก',
  navLabel: 'ไปที่แผนก',
  photoLabel: 'รูปบุคลากร',
  unavailable: 'ยังโหลดรายชื่อทีมไม่ได้ ลองใหม่อีกครั้งภายหลัง',
  empty: 'ยังไม่มีรายชื่อทีมบนเว็บไซต์',
  /** Beside the field on a card. A no-break space keeps "ปี" with its number. */
  experience: (years: number) => (years < 1 ? 'ประสบการณ์น้อยกว่า 1 ปี' : `ประสบการณ์ ${years} ปี`),
};

/** The link under each vet's card: the booking page with that vet already chosen. */
export const bookVetLink = {
  label: 'จองนัดกับหมอท่านนี้',
  href: (staffId: string) => `/book?vet=${encodeURIComponent(staffId)}`,
};

/** One line under each department's name. A department missing here simply shows no line. */
export const departmentBlurbs: Partial<Record<DepartmentCode, string>> = {
  veterinary: 'ตรวจ วินิจฉัย ผ่าตัด และวางแผนการรักษาให้น้องแต่ละตัว',
  nursing: 'ช่วยหมอในห้องตรวจและห้องผ่าตัด และดูแลน้องที่นอนโรงพยาบาล',
  front_desk: 'รับนัด เช็กอิน และโทรเตือนเมื่อถึงเวลาวัคซีน',
  finance: 'อธิบายค่าใช้จ่าย ออกใบเสร็จ และเตรียมเอกสารประกัน',
  management: 'ดูแลให้คลินิกทำงานราบรื่นทุกวัน',
  support: 'ความสะอาด การฆ่าเชื้อ และการดูแลน้องที่ฝากเลี้ยง',
};
