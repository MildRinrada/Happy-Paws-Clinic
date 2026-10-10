/**
 * The staff AI assistant's wire shapes (docs/contract-extensions.md § 34). Everything here is a
 * draft or a summary for a person to check; nothing the API returns has been saved or sent.
 */

/** `GET /staff/ai/usage`. When `enabled` is false every AI control is hidden. */
export interface AiUsage {
  enabled: boolean;
  /** Clinic month, `2026-10`. */
  month: string;
  limit: number;
  used: number;
  by_feature: Record<AiFeature, number>;
}

export type AiFeature = 'pet_summary' | 'soap_draft' | 'owner_message' | 'ask' | 'daily_brief';

/** What a citation in a summary points at. */
export type CitationKind = 'allergy' | 'visit' | 'medicine' | 'weight' | 'lab' | 'vaccination';

/** `[R3]` in the summary text, and the record behind it. */
export interface PetSummaryCitation {
  ref: string;
  kind: CitationKind;
  id: string;
  date: string;
}

/** `POST /staff/ai/pets/{id}/summary`. `cached` means it was served without a new call. */
export interface PetSummary {
  pet_id: string;
  text: string;
  citations: PetSummaryCitation[];
  cached: boolean;
  generated_at: string;
}

export interface DraftFinding {
  body_system: string;
  is_normal: boolean;
  note?: string;
}

/** The consultation form's own fields, drafted. */
export interface SoapDraftFields {
  chief_complaint: string;
  subjective: string;
  objective: string;
  assessment: string;
  plan: string;
  examination: DraftFinding[];
}

export type DraftWarningReason = 'dose_not_in_notes' | 'number_not_in_notes' | 'term_not_in_notes';

export interface DraftWarning {
  field: string;
  text: string;
  reason: DraftWarningReason;
}

/** `POST /staff/ai/consultations/{id}/soap-draft`. */
export interface SoapDraft {
  consultation_id: string;
  draft: SoapDraftFields;
  warnings: DraftWarning[];
}

/** `POST /staff/ai/drafts/owner-message`. */
export interface OwnerMessageDraft {
  text: string;
}

/** One line of an ask-the-data conversation, as the page keeps it and the API takes it. */
export interface AskMessage {
  role: 'user' | 'assistant';
  text: string;
}

/** A button under an answer: always a path in the console, built by the API. */
export interface AskAction {
  label: string;
  href: string;
}

export interface AskReply {
  reply: string;
  actions: AskAction[];
}

export interface AskEntry extends AskMessage {
  actions?: AskAction[];
}

export interface BriefAppointments {
  total: number;
  upcoming: number;
  arrived: number;
  cancelled: number;
  walk_in: number;
  booked_online: number;
}

export interface BriefQueue {
  tickets: number;
  outstanding: number;
  served: number;
}

export interface BriefInventory {
  expired: number;
  out_of_stock: number;
  near_expiry: number;
  low_stock: number;
  top: { kind: string; item_name: string }[];
}

export interface BriefRevenue {
  date: string;
  collected_satang: number;
  refunded_satang: number;
  net_satang: number;
  payments: number;
}

export interface BriefNumbers {
  appointments: BriefAppointments;
  queue: BriefQueue;
  inventory: BriefInventory;
  /** Only for a session that may read money. */
  yesterday_revenue?: BriefRevenue;
}

export type BriefStatus = 'ready' | 'failed' | 'pending' | 'disabled';

/** `GET /staff/ai/daily-brief?branch_id=`. */
export interface DailyBrief {
  status: BriefStatus;
  branch_id: string;
  date: string;
  /** `07:00`: when the day's brief is written. */
  generates_at: string;
  text?: string;
  numbers?: BriefNumbers;
  generated_at?: string;
  reason?: 'quota_exceeded' | 'model_unavailable' | 'numbers_failed';
}
