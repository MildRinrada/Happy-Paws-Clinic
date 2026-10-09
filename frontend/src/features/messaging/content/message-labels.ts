import type { BadgeTone } from '@/components/ui/badge';
import { channelLabels } from '@/features/notifications/content/delivery-labels';
import type { ThreadStatus } from '../types/thread.types';

/** How a thread's state reads in the list. */
export const threadStatusLabels: Record<ThreadStatus, string> = {
  awaiting_reply: 'รอคำตอบ',
  replied: 'ตอบแล้ว',
};

export const threadStatusTones: Record<ThreadStatus, BadgeTone> = {
  awaiting_reply: 'amber',
  replied: 'neutral',
};

/** What the customer sent when it was not words. Only text is kept; the rest stays in LINE. */
export const kindLabels: Record<string, string> = {
  image: 'ส่งรูปภาพ (ดูได้ในแอป LINE)',
  video: 'ส่งวิดีโอ (ดูได้ในแอป LINE)',
  audio: 'ส่งข้อความเสียง (ฟังได้ในแอป LINE)',
  file: 'ส่งไฟล์',
  location: 'ส่งตำแหน่ง',
  sticker: 'ส่งสติกเกอร์',
  other: 'ส่งข้อความชนิดที่ระบบยังไม่รองรับ',
};

/**
 * Where a customer wrote from: the delivery channels, plus `portal` for the portal's chat page
 * (contract extension § 24), which is not a way to send anything and so is not in `channelLabels`.
 */
export const inboundChannelLabels: Record<string, string> = {
  ...channelLabels,
  portal: 'เว็บไซต์',
};
