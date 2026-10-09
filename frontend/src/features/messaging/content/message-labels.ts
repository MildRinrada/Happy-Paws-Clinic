import type { BadgeTone } from '@/components/ui/badge';
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
