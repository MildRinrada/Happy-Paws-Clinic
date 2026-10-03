/** What a contact entry is. Each kind is drawn its own way on the public site. */
export type ContactKind =
  | 'phone'
  | 'emergency'
  | 'email'
  | 'line'
  | 'facebook'
  | 'instagram'
  | 'website'
  | 'map'
  | 'other';

/** One way to reach the clinic — `GET /clinic/contacts`, a contract extension (§ 15). */
export interface ClinicContact {
  id: string;
  version: number;
  kind: ContactKind;
  label: string;
  value: string;
  url?: string;
  /** A Google Maps embed address, for a map only. */
  embed_url?: string;
  /** Absent for the whole clinic. */
  branch_id?: string;
  sort_order: number;
}

/** The body of `POST /clinic/contacts`, and of `PATCH` where an empty string clears an optional field. */
export interface ContactInput {
  kind: ContactKind;
  label: string;
  value: string;
  url: string;
  embed_url: string;
  branch_id: string;
  sort_order: number;
}
