import type { BadgeTone } from '@/components/ui/badge';
import type { ReferralChannel, ReferralRejectReason, ReferralStatus } from '../types/referral.types';

/** `/crm/referrals`: who referred whom (docs/contract-extensions.md § 39). */
export const staffReferralsText = {
  title: 'ชวนเพื่อน',
  subtitle: 'ลูกค้าใหม่ที่จองผ่านลิงก์ของลูกค้าเดิม สถานะ และแต้มที่แต่ละฝั่งได้',
  filterHint: 'ค้นลูกค้าเพื่อดูเฉพาะการชวนที่ลูกค้าคนนั้นเป็นคนชวนหรือถูกชวน',
  showingFor: (customerId: string) => `แสดงเฉพาะการชวนของลูกค้า ${customerId}`,
  showAll: 'ดูทั้งหมด',
  empty: 'ยังไม่มีการชวนเพื่อน',
  referrer: 'คนชวน',
  friend: 'เพื่อนที่ถูกชวน',
  noName: 'ไม่ทราบชื่อ',
  joined: (date: string) => `จองผ่านลิงก์ ${date}`,
  rewardedOn: (date: string) => `ได้แต้ม ${date}`,
  rejectedOn: (date: string) => `ไม่ได้แต้ม ${date}`,
  points: (referrer: number, friend: number) =>
    `คนชวนได้ ${referrer.toLocaleString('th-TH')} แต้ม เพื่อนได้ ${friend.toLocaleString('th-TH')} แต้ม`,
  overCap: 'คนชวนครบเพดานของเดือนแล้ว จึงไม่ได้แต้ม',
  bill: 'บิลแรก',
  code: (code: string) => `รหัส ${code}`,
  next: 'หน้าถัดไป',
  first: 'กลับหน้าแรก',
  settingsLink: 'ตั้งแต้มและเพดานต่อเดือน',
};

export const referralStatusLabels: Record<ReferralStatus, { text: string; tone: BadgeTone }> = {
  pending: { text: 'รอบิลแรก', tone: 'amber' },
  rewarded: { text: 'ได้แต้มแล้ว', tone: 'green' },
  rejected: { text: 'ไม่ได้แต้ม', tone: 'neutral' },
};

export const referralRejectLabels: Record<ReferralRejectReason, string> = {
  self_referral: 'ใช้ลิงก์ของตัวเอง',
  not_new_customer: 'เป็นลูกค้าเดิมอยู่แล้ว',
  shared_contact: 'ใช้เบอร์โทรหรืออีเมลเดียวกับคนชวน',
  programme_off: 'ปิดการชวนเพื่อนอยู่ตอนบิลแรกชำระ',
};

export const referralChannelLabels: Record<ReferralChannel, string> = {
  guest_booking: 'จองผ่านหน้าเว็บ',
  register: 'สมัครบัญชี',
};
