import { clinicDay, formatTime } from '@/lib/format/datetime';
import { bookingFrame } from '../content/booking-frame';
import { dayChipParts } from './booking-dates';

/** "อ. 13 ต.ค. เวลา 10:30" for the summary card, from the fixed day tables so it renders the same everywhere. */
export function summaryWhen(startsAt: string): string {
  const { weekday, date } = dayChipParts(clinicDay(startsAt));
  return bookingFrame.summary.at(`${weekday} ${date}`, formatTime(startsAt));
}

/** "อ. 13 ต.ค." for a day chosen before its time. */
export function summaryDay(day: string): string {
  const { weekday, date } = dayChipParts(day);
  return `${weekday} ${date}`;
}
