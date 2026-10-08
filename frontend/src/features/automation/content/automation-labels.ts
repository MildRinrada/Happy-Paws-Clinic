import type { BadgeTone } from '@/components/ui/badge';
import type { AutomationChannel, AutomationTriggerType, ReminderStatus } from '../types/automation.types';

/** How the automation vocabulary reads on screen. Keys are the API's own values. */

export const triggerLabels: Record<AutomationTriggerType, string> = {
  vaccine_due: 'ครบกำหนดวัคซีน',
  appointment_upcoming: 'ก่อนถึงนัด',
  treatment_plan_session: 'ก่อนถึงรอบแผนการรักษา',
};

export const triggerChoices: { value: AutomationTriggerType; label: string }[] = [
  { value: 'vaccine_due', label: triggerLabels.vaccine_due },
  { value: 'appointment_upcoming', label: triggerLabels.appointment_upcoming },
  { value: 'treatment_plan_session', label: triggerLabels.treatment_plan_session },
];

export const automationChannelLabels: Record<AutomationChannel, string> = {
  auto: 'อัตโนมัติ (LINE แล้ว SMS แล้วอีเมล)',
  line: 'LINE เท่านั้น',
  sms: 'SMS เท่านั้น',
  email: 'อีเมลเท่านั้น',
};

export const reminderStatusLabels: Record<ReminderStatus, string> = {
  would_send: 'จะส่ง',
  would_skip: 'จะถูกข้าม',
  already_sent: 'ส่งไปแล้ว',
};

export const reminderStatusTones: Record<ReminderStatus, BadgeTone> = {
  would_send: 'green',
  would_skip: 'amber',
  already_sent: 'neutral',
};

export const subjectLabels: Record<string, string> = {
  vaccination: 'วัคซีน',
  appointment: 'นัดหมาย',
  treatment_plan_session: 'รอบแผนการรักษา',
};
