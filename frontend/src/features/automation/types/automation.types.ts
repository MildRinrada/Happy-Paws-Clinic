/** Shapes from `/staff/automations` and `/staff/reminders/upcoming`. Field names are the API's snake_case ones. */

/**
 * `customer_lapsed` is docs/contract-extensions.md § 33: a customer later back than their own usual gap.
 * `visit_completed` is § 36: a finished visit, for the review invitation.
 */
export type AutomationTriggerType =
  | 'vaccine_due'
  | 'appointment_upcoming'
  | 'treatment_plan_session'
  | 'customer_lapsed'
  | 'visit_completed';

export type AutomationChannel = 'auto' | 'line' | 'sms' | 'email';

export type ReminderStatus = 'would_send' | 'would_skip' | 'already_sent';

export interface AutomationTrigger {
  type: AutomationTriggerType;
  offset_days?: number;
  offset_hours?: number;
  /** `HH:mm`, clinic time, with `offset_days`. */
  send_time?: string;
}

export interface AutomationCondition {
  branch_id?: string;
  species?: string;
  vaccine_name?: string;
  service_id?: string;
  unconfirmed_only: boolean;
}

export interface AutomationStats {
  total: number;
  sent: number;
  skipped: number;
  failed: number;
  lost: number;
  last_30_days: number;
  last_fired_at?: string;
}

export interface Automation {
  id: string;
  /** For `If-Match` on an edit or a delete (docs/contract-extensions.md § 19). */
  version: number;
  name: string;
  enabled: boolean;
  trigger: AutomationTrigger;
  condition: AutomationCondition;
  action: { type: 'send_notification'; channel: AutomationChannel; template: string };
  stats: AutomationStats;
  /** Absent for a starter rule the clinic was given. */
  created_by_staff_id?: string;
  created_at: string;
}

export interface CreateAutomationInput {
  name: string;
  trigger: AutomationTrigger;
  condition: { branch_id?: string; species?: string; vaccine_name?: string; unconfirmed_only?: boolean };
  action: { type: 'send_notification'; channel: AutomationChannel };
  enabled: boolean;
}

/**
 * `PATCH /staff/automations/{id}` (docs/contract-extensions.md § 19). Every part optional; an
 * empty condition string clears it. What the rule watches (`trigger.type`) cannot change.
 */
export interface UpdateAutomationInput {
  name?: string;
  trigger?: AutomationTrigger;
  condition?: { species?: string; vaccine_name?: string; unconfirmed_only?: boolean };
  action?: { channel: AutomationChannel };
  enabled?: boolean;
}

export interface Reminder {
  automation_id: string;
  automation_name: string;
  send_at: string;
  due_now: boolean;
  status: ReminderStatus;
  skip_reason?: string;
  customer_id: string;
  customer_name: string;
  pet_id: string;
  pet_name: string;
  branch_id?: string;
  subject: {
    type: 'vaccination' | 'appointment' | 'treatment_plan_session' | 'customer';
    id: string;
    due_on: string;
    starts_at?: string;
    label?: string;
    appointment_id?: string;
  };
  template: string;
  channel: AutomationChannel;
  message: { title: string; body: string };
}

export interface Simulation {
  automation_id: string;
  enabled: boolean;
  evaluated_at: string;
  from: string;
  to: string;
  due_now: number;
  would_send: number;
  would_skip: number;
  already_sent: number;
  truncated: boolean;
  recipients: Reminder[];
}

export interface UpcomingReminders {
  from: string;
  to: string;
  total: number;
  truncated: boolean;
  data: Reminder[];
}
