import { automationsPage } from '../content/automations-page';
import type { Automation } from '../types/automation.types';

/** "14 วันก่อน เวลา 09:00 น." or "24 ชั่วโมงก่อนถึงเวลา": when a rule sends, in words. */
export function describeTiming(rule: Pick<Automation, 'trigger'>): string {
  const text = automationsPage.timing;
  const { offset_days: days, offset_hours: hours, send_time: time = '09:00' } = rule.trigger;

  if (hours !== undefined) return text.hoursBefore(hours);
  if (days === undefined || days === 0) return text.sameDay(time);
  if (rule.trigger.type === 'customer_lapsed') return text.daysAfterDueBack(Math.abs(days), time);

  return days > 0 ? text.daysBefore(days, time) : text.daysAfter(-days, time);
}

/** The narrowing conditions of a rule, each as a short phrase. Empty when it covers everything. */
export function describeConditions(rule: Pick<Automation, 'condition'>): string[] {
  const text = automationsPage.conditions;
  const { species, vaccine_name: vaccine, unconfirmed_only: unconfirmed } = rule.condition;

  return [
    ...(species ? [text.species(species)] : []),
    ...(vaccine ? [text.vaccine(vaccine)] : []),
    ...(unconfirmed ? [text.unconfirmed] : []),
  ];
}

/** The rules the clinic was given at installation carry fixed ids. */
export const isStarterRule = (rule: Pick<Automation, 'id'>) => rule.id.startsWith('atm_starter_');
