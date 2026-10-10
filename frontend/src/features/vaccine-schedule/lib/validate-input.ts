import { vaccinePage } from '../content/vaccine-schedule-page';
import { addMonths, isCalendarDay } from './calendar-date';

export interface InputErrors {
  birthDate?: string;
  lastVaccineDate?: string;
}

const text = vaccinePage.form.errors;

/** What is wrong with the two dates, if anything. An empty object means they can be used. */
export function validateInput(birthDate: string, lastVaccineDate: string, today: string): InputErrors {
  const errors: InputErrors = {};

  if (!isCalendarDay(birthDate)) {
    errors.birthDate = text.birthMissing;
  } else if (birthDate > today) {
    errors.birthDate = text.birthFuture;
  } else if (birthDate < addMonths(today, -12 * 30)) {
    errors.birthDate = text.birthTooOld;
  }

  if (lastVaccineDate) {
    if (!isCalendarDay(lastVaccineDate) || lastVaccineDate > today) {
      errors.lastVaccineDate = text.lastFuture;
    } else if (!errors.birthDate && lastVaccineDate < birthDate) {
      errors.lastVaccineDate = text.lastBeforeBirth;
    }
  }

  return errors;
}
