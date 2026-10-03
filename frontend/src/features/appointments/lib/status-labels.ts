import type { BadgeTone } from '@/components/ui/badge';
import type { AppointmentStatus } from '../types/appointment.types';

/** Thai for each state, and the colour it is drawn in on the board. */
export const appointmentStatusLabel: Record<AppointmentStatus, { text: string; tone: BadgeTone }> = {
  draft: { text: 'ร่าง', tone: 'neutral' },
  booked: { text: 'จองแล้ว', tone: 'blue' },
  confirmed: { text: 'ยืนยันแล้ว', tone: 'brand' },
  checked_in: { text: 'เช็กอินแล้ว', tone: 'amber' },
  in_consultation: { text: 'กำลังตรวจ', tone: 'amber' },
  completed: { text: 'เสร็จแล้ว', tone: 'green' },
  cancelled: { text: 'ยกเลิก', tone: 'neutral' },
  no_show: { text: 'ไม่มาตามนัด', tone: 'red' },
};

/** The block's own colours on the grid, matching the badge. Cancelled and no-show fade back. */
export const appointmentBlockTone: Record<AppointmentStatus, string> = {
  draft: 'border-ink-300 bg-ink-50',
  booked: 'border-sky-300 bg-sky-50',
  confirmed: 'border-brand-300 bg-brand-50',
  checked_in: 'border-amber-300 bg-amber-50',
  in_consultation: 'border-amber-400 bg-amber-100',
  completed: 'border-emerald-300 bg-emerald-50',
  cancelled: 'border-ink-200 bg-white opacity-60 line-through',
  no_show: 'border-red-200 bg-red-50 opacity-70',
};

/** Still to happen: the states the counter can still act on. */
export function isOpen(status: AppointmentStatus): boolean {
  return status === 'booked' || status === 'confirmed';
}
