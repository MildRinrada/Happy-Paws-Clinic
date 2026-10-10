import type { ConditionValue } from '../components/condition-fields';
import type { TimingValue } from '../components/timing-fields';
import type { Automation, AutomationChannel, AutomationTrigger, AutomationTriggerType } from '../types/automation.types';

/** What the create form starts with: the contract's own example, a vaccine 14 days ahead at 09:00. */
export const initialTiming: TimingValue = { mode: 'days', days: '14', hours: '24', time: '09:00' };

export const initialCondition: ConditionValue = { species: '', vaccineName: '', unconfirmedOnly: false };

/**
 * Triggers whose subject is a day with no time in it (a due date, the day a customer was due back,
 * the day of a visit), so they only count in days.
 */
export const countsInDaysOnly = (type: AutomationTriggerType) =>
  type === 'vaccine_due' || type === 'customer_lapsed' || type === 'visit_completed';

/**
 * Triggers that only send after their day, so the form's offset is negative: a lapsed customer
 * (docs/contract-extensions.md § 33) and a review invitation (§ 36). The range is the API's.
 */
export const afterOnlyRange: Partial<Record<AutomationTriggerType, { min: number; max: number }>> = {
  customer_lapsed: { min: -90, max: -1 },
  visit_completed: { min: -30, max: -1 },
};

/** The form's text fields as the API's trigger. A number field left empty is sent as 0 and refused there. */
export function toTrigger(type: AutomationTriggerType, timing: TimingValue): AutomationTrigger {
  const mode = countsInDaysOnly(type) ? 'days' : timing.mode;

  return mode === 'hours'
    ? { type, offset_hours: Number(timing.hours) || 0 }
    : { type, offset_days: Number(timing.days) || 0, send_time: timing.time || undefined };
}

export interface CreateFormValues {
  name: string;
  type: AutomationTriggerType;
  timing: TimingValue;
  condition: ConditionValue;
  channel: AutomationChannel;
  enabled: boolean;
  branchId?: string;
}

/** The server action's input, with conditions that do not apply to the trigger dropped. */
export function toCreateInput(values: CreateFormValues) {
  return {
    name: values.name,
    trigger: toTrigger(values.type, values.timing),
    condition: {
      branch_id: values.branchId,
      species: values.condition.species,
      vaccine_name: values.type === 'vaccine_due' ? values.condition.vaccineName : undefined,
      unconfirmed_only: values.type === 'appointment_upcoming' ? values.condition.unconfirmedOnly : undefined,
    },
    channel: values.channel,
    enabled: values.enabled,
  };
}

/** An existing rule's timing as the form's text fields, so an edit starts from what is saved. */
export function timingOf(rule: Pick<Automation, 'trigger'>): TimingValue {
  const { offset_days: days, offset_hours: hours, send_time: time } = rule.trigger;

  return {
    mode: hours !== undefined ? 'hours' : 'days',
    days: String(days ?? initialTiming.days),
    hours: String(hours ?? initialTiming.hours),
    time: time ?? initialTiming.time,
  };
}

export function conditionOf(rule: Pick<Automation, 'condition'>): ConditionValue {
  return {
    species: rule.condition.species ?? '',
    vaccineName: rule.condition.vaccine_name ?? '',
    unconfirmedOnly: rule.condition.unconfirmed_only,
  };
}

export interface EditFormValues {
  name: string;
  type: AutomationTriggerType;
  timing: TimingValue;
  condition: ConditionValue;
  channel: AutomationChannel;
}

/**
 * The edit's server action input. Conditions are always sent, blank ones as an empty string,
 * because on an edit a blank means "clear it" rather than "leave it".
 */
export function toUpdateValues(values: EditFormValues) {
  return {
    name: values.name,
    trigger: toTrigger(values.type, values.timing),
    condition: {
      species: values.condition.species.trim(),
      vaccine_name: values.type === 'vaccine_due' ? values.condition.vaccineName.trim() : undefined,
      unconfirmed_only: values.type === 'appointment_upcoming' ? values.condition.unconfirmedOnly : undefined,
    },
    channel: values.channel,
  };
}
