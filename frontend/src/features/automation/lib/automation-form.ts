import type { ConditionValue } from '../components/condition-fields';
import type { TimingValue } from '../components/timing-fields';
import type { AutomationChannel, AutomationTrigger, AutomationTriggerType } from '../types/automation.types';

/** What the create form starts with: the contract's own example, a vaccine 14 days ahead at 09:00. */
export const initialTiming: TimingValue = { mode: 'days', days: '14', hours: '24', time: '09:00' };

export const initialCondition: ConditionValue = { species: '', vaccineName: '', unconfirmedOnly: false };

/** The form's text fields as the API's trigger. A number field left empty is sent as 0 and refused there. */
export function toTrigger(type: AutomationTriggerType, timing: TimingValue): AutomationTrigger {
  const mode = type === 'vaccine_due' ? 'days' : timing.mode;

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
