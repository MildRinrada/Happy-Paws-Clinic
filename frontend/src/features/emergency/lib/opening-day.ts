import { dayChipParts } from '@/features/booking/lib/booking-dates';
import { WEEKDAY_LABELS } from '@/features/clinic/lib/weekdays';
import { emergencyPage } from '../content/emergency-page';
import { weekdayOf } from './clinic-clock';

/** "วันนี้", "พรุ่งนี้", or "วันจันทร์ที่ 13 ต.ค." for the day a closed branch opens again. */
export function openingDay(day: string, offset: number): string {
  const text = emergencyPage.status;

  if (offset === 0) return text.today;
  if (offset === 1) return text.tomorrow;

  return text.laterDay(WEEKDAY_LABELS[weekdayOf(day)], dayChipParts(day).date);
}
