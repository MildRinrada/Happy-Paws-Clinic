import type { BadgeTone } from '@/components/ui/badge';
import type { LoyaltyTier, PackageStatus, PointsEntryKind } from '../types/crm.types';

/** Membership levels, as staff read them. */
export const tierLabels: Record<LoyaltyTier, string> = {
  member: 'สมาชิก',
  silver: 'ซิลเวอร์',
  gold: 'โกลด์',
};

/** The badge colour each level is drawn in. */
export const tierTones: Record<LoyaltyTier, BadgeTone> = {
  member: 'neutral',
  silver: 'blue',
  gold: 'amber',
};

/** A package's state, with the badge colour it is drawn in. */
export const packageStatusLabels: Record<PackageStatus, { text: string; tone: BadgeTone }> = {
  pending_payment: { text: 'รอชำระเงิน', tone: 'amber' },
  active: { text: 'ใช้ได้', tone: 'green' },
  used_up: { text: 'ใช้ครบแล้ว', tone: 'neutral' },
  expired: { text: 'หมดอายุ', tone: 'red' },
  cancelled: { text: 'ยกเลิกแล้ว', tone: 'neutral' },
};

/** What moved a customer's points. */
export const pointsKindLabels: Record<PointsEntryKind, string> = {
  earn: 'ได้แต้มจากบิล',
  reverse: 'หักแต้มคืนเพราะคืนเงิน',
  redeem: 'แลกแต้มเป็นส่วนลด',
  restore: 'คืนแต้มที่แลกไว้',
  adjust: 'ปรับแต้มโดยพนักงาน',
};

export const packageText = {
  uses: (left: number, total: number) => `เหลือ ${left} จาก ${total} ครั้ง`,
  expires: (date: string) => `ใช้ได้ถึง ${date}`,
  noExpiry: 'ไม่มีวันหมดอายุ',
};
