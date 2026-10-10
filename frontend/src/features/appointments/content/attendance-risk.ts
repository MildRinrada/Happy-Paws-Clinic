import type { BadgeTone } from '@/components/ui/badge';
import type { AttendanceRiskLevel } from '../types/attendance-risk.types';

/** Who may not turn up today (docs/contract-extensions.md § 33). */
export const attendanceRiskText = {
  heading: (count: number) => `นัดที่เสี่ยงไม่มา ${count} นัด`,
  none: 'วันนี้ไม่มีนัดที่เสี่ยงไม่มา',
  how: 'คิดจากประวัติไม่มาตามนัดและยกเลิกกระชั้นของลูกค้าเอง การจองล่วงหน้านาน และการยืนยันนัด เป็นแค่ข้อสังเกต ไม่ได้เปลี่ยนนัดหรือส่งข้อความให้ใคร',
  reasonsLabel: 'เหตุผล',
  marker: '!',
  open: 'ดูนัด',
};

export const riskLabels: Record<AttendanceRiskLevel, { label: string; tone: BadgeTone }> = {
  high: { label: 'เสี่ยงไม่มาสูง', tone: 'red' },
  medium: { label: 'เสี่ยงไม่มา', tone: 'amber' },
  low: { label: 'ปกติ', tone: 'neutral' },
};
