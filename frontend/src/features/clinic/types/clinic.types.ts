/** `GET /clinic` — "ชื่อ โลโก้ สี ใบอนุญาต ผู้ควบคุมกิจการ". Absent fields were never set. */
export interface ClinicProfile {
  version: number;
  name: string;
  /**
   * The logo: an `https://` address, or an uploaded logo as `storage:clinic-brand/logo-…`
   * (docs/contract-extensions.md § 50). Read it through `logoSrcOf`, never as an image address.
   */
  logo_url?: string;
  brand_color?: string;
  license_no?: string;
  operator_name?: string;
  /** The optional square browser-tab icon, uploaded only (§ 50). */
  tab_icon_url?: string;
  /** Whether the name is written beside the logo (§ 50). Absent from an older API: treat as on. */
  show_name_with_logo?: boolean;
}

export type Weekday = 'monday' | 'tuesday' | 'wednesday' | 'thursday' | 'friday' | 'saturday' | 'sunday';

/** One weekday's hours. `opens` and `closes` are "HH:mm", absent on a closed day. */
export interface DayHours {
  day: Weekday;
  opens?: string;
  closes?: string;
  closed: boolean;
}

/** A special closure. Without `branch_id` the whole clinic is closed. */
export interface Holiday {
  id: string;
  date: string;
  name: string;
  branch_id?: string;
}

/** `GET /clinic/hours` — the week, and the special closures still to come. */
export interface OpeningHours {
  version: number;
  /** False until the clinic has set its hours; `days` is then a placeholder. */
  configured: boolean;
  days: DayHours[];
  holidays: Holiday[];
}

/** One site — `GET /clinic/branches`, "สาขา ที่อยู่ พิกัด ห้องตรวจ". */
export interface Branch {
  id: string;
  version: number;
  code: string;
  name: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  exam_rooms: string[];
}
