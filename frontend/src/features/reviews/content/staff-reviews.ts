import type { BadgeTone } from '@/components/ui/badge';
import type { RejectReason, ReviewStatus } from '../types/review.types';

/** The staff moderation page, `/settings/reviews` (docs/contract-extensions.md § 36). */
export const staffReviewsText = {
  metaTitle: 'รีวิวจากลูกค้า | Happy Paws Clinic',
  title: 'รีวิวจากลูกค้า',
  subtitle:
    'รีวิวทุกอันรอเจ้าหน้าที่อ่านก่อนขึ้นหน้าเว็บ เผยแพร่ได้ หรือไม่เผยแพร่ด้วยเหตุผลจากรายการเท่านั้น คะแนนต่ำไม่ใช่เหตุผล ค่าเฉลี่ยบนหน้าเว็บจึงตรงกับที่ลูกค้าให้จริง',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์จัดการเนื้อหาบนหน้าเว็บ',
  empty: 'ไม่มีรีวิวในแท็บนี้',

  tabsLabel: 'สถานะรีวิว',
  tabs: [
    { status: 'pending', label: 'รอตรวจ' },
    { status: 'published', label: 'เผยแพร่แล้ว' },
    { status: 'rejected', label: 'ไม่เผยแพร่' },
  ] satisfies { status: ReviewStatus; label: string }[],

  next: 'หน้าถัดไป',
  first: 'กลับไปหน้าแรก',

  from: (name: string) => `จาก ${name}`,
  shownAs: (name: string) => `แสดงบนเว็บว่า ${name}`,
  visit: (pet: string, day: string, branch: string) => `${pet} มาเมื่อ ${day} ${branch}`.trim(),
  source: { portal: 'เขียนในระบบออนไลน์', invitation: 'เขียนผ่านลิงก์รีวิว' },
  noText: 'ให้คะแนนอย่างเดียว ไม่ได้เขียนข้อความ',
  written: (at: string) => `เขียนเมื่อ ${at}`,
  moderated: (at: string, staffId: string) => `ตัดสินเมื่อ ${at} โดย ${staffId}`,
  rejectedFor: (reason: string) => `ไม่เผยแพร่เพราะ ${reason}`,

  publish: 'เผยแพร่',
  publishing: 'กำลังเผยแพร่…',

  reject: {
    open: 'ไม่เผยแพร่',
    takeDown: 'นำออกจากหน้าเว็บ',
    reason: 'เหตุผล',
    choose: 'เลือกเหตุผล',
    chooseFirst: 'ต้องเลือกเหตุผลจากรายการ',
    confirm: 'ยืนยันไม่เผยแพร่',
    confirming: 'กำลังบันทึก…',
    cancel: 'ยกเลิก',
    note: 'นำออกได้เฉพาะเหตุผลในรายการ ไม่มีช่องเหตุผลอื่น และแก้กลับไม่ได้',
  },

  reply: {
    heading: 'คำตอบของคลินิก',
    label: 'ตอบรีวิวนี้ (แสดงใต้รีวิวบนหน้าเว็บ)',
    edit: 'แก้คำตอบ',
    write: 'ตอบรีวิว',
    save: 'บันทึกคำตอบ',
    saving: 'กำลังบันทึก…',
    saved: 'บันทึกคำตอบแล้ว',
    cancel: 'ยกเลิก',
    repliedAt: (at: string) => `ตอบเมื่อ ${at}`,
  },

  conflict: 'มีคนตัดสินรีวิวนี้ไปก่อนแล้ว โหลดหน้าใหม่เพื่อดูสถานะล่าสุด',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
};

export const reviewStatusLabels: Record<ReviewStatus, string> = {
  pending: 'รอตรวจ',
  published: 'เผยแพร่แล้ว',
  rejected: 'ไม่เผยแพร่',
};

export const reviewStatusTones: Record<ReviewStatus, BadgeTone> = {
  pending: 'amber',
  published: 'green',
  rejected: 'neutral',
};

/** The only reasons a review may be kept off the site. Keys are the API's own values. */
export const rejectReasonLabels: Record<RejectReason, string> = {
  personal_data: 'มีข้อมูลส่วนบุคคล',
  offensive: 'ภาษาไม่สุภาพ',
  off_topic: 'ไม่เกี่ยวกับบริการของคลินิก',
  spam: 'สแปมหรือซ้ำ',
  customer_request: 'ลูกค้าขอให้นำออก',
};
