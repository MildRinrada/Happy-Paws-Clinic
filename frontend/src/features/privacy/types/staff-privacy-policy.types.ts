import type { PolicySection } from './privacy-policy.types';

/** One version as the console sees it, drafts included (docs/contract-extensions.md § 35). */
export interface StaffPolicyVersion {
  id: string;
  status: 'draft' | 'published';
  /** The version the public page shows and consents are stamped with. */
  is_current: boolean;
  label?: string;
  effective_date?: string;
  published_at?: string;
  published_by_staff_id?: string;
  legal_review_done: boolean;
  /** `template`: made from the text that ships in the code. `version`: a copy of another version. */
  created_from: 'template' | 'version';
  template_version?: string;
  based_on_version_id?: string;
  created_by_staff_id?: string;
  created_at: string;
  updated_at?: string;
  /** Echo as `If-Match`. */
  version: number;
  sections: PolicySection[];
}

/** `GET /staff/privacy-policy/versions`. */
export interface StaffPolicyList {
  data: StaffPolicyVersion[];
  current_version_id?: string;
  template: {
    version: string;
    /** True when a release shipped a template newer than any the clinic made a draft from. */
    newer_available: boolean;
  };
}

/** Where a new draft's text comes from. */
export type DraftSource = { source: 'template' } | { source: 'current' } | { source: 'version'; version_id: string };

/** What publishing asks for. */
export interface PublishInput {
  label: string;
  effective_date: string;
  legal_review_done: boolean;
}
