import type { BadgeTone } from '@/components/ui/badge';
import type { TicketPriority, TicketStatus } from '../types/queue.types';

/** The Thai a counter uses for each state, and the colour it is drawn in. */
export const statusLabel: Record<TicketStatus, { text: string; tone: BadgeTone }> = {
  waiting: { text: 'รอเรียก', tone: 'neutral' },
  skipped: { text: 'ข้ามแล้ว · รอเรียกซ้ำ', tone: 'amber' },
  called: { text: 'กำลังเรียก', tone: 'blue' },
  in_room: { text: 'อยู่ในห้องตรวจ', tone: 'brand' },
  served: { text: 'ตรวจเสร็จ', tone: 'green' },
  left: { text: 'ออกจากคิว', tone: 'neutral' },
};

export const priorityLabel: Record<TicketPriority, { text: string; tone: BadgeTone } | null> = {
  normal: null,
  urgent: { text: 'เร่งด่วน', tone: 'amber' },
  emergency: { text: 'ฉุกเฉิน', tone: 'red' },
};
