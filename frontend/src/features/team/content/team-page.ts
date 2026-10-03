import type { DepartmentCode } from '../types/team.types';

/** Everything the team page says that does not come from the API. Edit the wording here. */
export const teamPage = {
  title: 'ทีมของเรา',
  intro: 'ทุกคนที่ดูแลน้องของคุณ ตั้งแต่เคาน์เตอร์ต้อนรับ ห้องตรวจ จนถึงวันที่น้องได้กลับบ้าน',
  navLabel: 'ไปที่แผนก',
  photoLabel: 'รูปบุคลากร',
  unavailable: 'ยังโหลดรายชื่อทีมไม่ได้ ลองใหม่อีกครั้งภายหลัง',
  empty: 'ยังไม่มีรายชื่อทีมบนเว็บไซต์',
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
