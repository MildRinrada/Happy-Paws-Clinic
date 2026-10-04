/**
 * Thai names for the codes the staff API speaks. A code not listed here (a staff type or role the
 * clinic made up itself) is shown as written.
 */

/** The four positions the API suggests, and the two the bootstrap gives the owner and admin. */
const staffTypes: Record<string, string> = {
  veterinarian: 'สัตวแพทย์',
  vet_assistant: 'ผู้ช่วยสัตวแพทย์',
  receptionist: 'พนักงานต้อนรับ',
  cashier: 'แคชเชียร์',
  owner: 'เจ้าของคลินิก',
  clinic_admin: 'ผู้ดูแลคลินิก',
};

/** The seven starter roles. */
const roles: Record<string, string> = {
  clinic_owner: 'เจ้าของคลินิก',
  clinic_admin: 'ผู้ดูแลคลินิก',
  veterinarian: 'สัตวแพทย์',
  receptionist: 'พนักงานต้อนรับ',
  vet_assistant: 'ผู้ช่วยสัตวแพทย์',
  cashier: 'แคชเชียร์',
  customer: 'ลูกค้า (portal)',
};

export const staffTypeLabel = (code: string) => staffTypes[code] ?? code;
export const roleLabel = (name: string) => roles[name] ?? name;

export const statusLabels = {
  active: 'ใช้งานอยู่',
  invited: 'รอตอบรับคำเชิญ',
  deactivated: 'ปิดใช้งาน',
} as const;
