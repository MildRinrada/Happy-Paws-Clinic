import type { AppointmentStatus } from '@/features/appointments/types/appointment.types';

/** One line of "ต้องจัดการตอนนี้": what is waiting, how many, and where the work is done. */
export interface AttentionItem {
  key: 'long_wait' | 'messages' | 'reviews' | 'stock' | 'attendance';
  count: number;
  /** True when the list read hit its page size, so `count` is a floor ("100+"). */
  more: boolean;
  href: string;
  /** A short second line, such as the longest wait or the stock problems by kind. */
  detail?: string;
}

/** What the to-do box knows: the lines with work in them, and how many reads came back. */
export interface AttentionView {
  items: AttentionItem[];
  /** Reads that answered. Zero items over at least one read is "เรียบร้อยดี". */
  checked: number;
  /** Reads the session may make that failed or were refused; their lines are left out. */
  failed: number;
}

/** An appointment starting soon, with the names a person reads. Either name may be unknown. */
export interface UpcomingVisit {
  id: string;
  startsAt: string;
  status: AppointmentStatus;
  petName?: string;
  customerName?: string;
}

/** The same weekday last week, read from the range reports. Absent when the read failed or is not allowed. */
export interface LastWeek {
  day: string;
  appointments?: number;
  revenueSatang?: number;
}

/** One customer to call, as the overview previews them. */
export interface DueBackPreviewRow {
  customerId: string;
  customerName: string;
  phone?: string;
  petNames: string[];
  daysOverdue: number;
}
