/** Department codes, in the order the API returns them. */
export type DepartmentCode = 'veterinary' | 'nursing' | 'front_desk' | 'finance' | 'management' | 'support';

/** One person as `GET /team` shows them to the public. */
export interface TeamMember {
  id: string;
  display_name: string;
  job_title?: string;
  specialty?: string;
  bio?: string;
  /** Path under the site's /public folder. */
  photo_path?: string;
  years_of_experience?: number;
  /** A signed link to an uploaded photo, good for an hour, minted for this page (§ 32). Shown before `photo_path`. */
  photo_url?: string;
}

/** One department of `GET /team`, with its listed members. */
export interface TeamDepartment {
  department: DepartmentCode;
  /** Thai name from the API, e.g. "ทีมสัตวแพทย์". */
  name: string;
  members: TeamMember[];
}
