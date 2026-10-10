/** The two kinds of pet the helper plans for. */
export type Species = 'dog' | 'cat';

/** One vaccine, named by what it protects against. Never a brand or a dose. */
export interface VaccineInfo {
  id: string;
  name: string;
  /** What it protects against, in plain words. */
  protects: string;
}

/**
 * When a visit falls.
 * - `age-weeks` / `age-months`: counted from the birth date.
 * - `after-previous-months`: counted from the visit before it (the yearly booster).
 */
export type VisitTiming =
  | { kind: 'age-weeks'; weeks: number }
  | { kind: 'age-months'; months: number }
  | { kind: 'after-previous-months'; months: number };

/** One trip to the clinic in the general schedule, and what is given on it. */
export interface VisitTemplate {
  id: string;
  title: string;
  /** The timing in words, for the general schedule table: "อายุ 8 สัปดาห์". */
  when: string;
  timing: VisitTiming;
  vaccineIds: string[];
  note?: string;
}

/** A vaccine the general schedule leaves to the vet, by lifestyle and risk. */
export interface OptionalVaccine {
  name: string;
  detail: string;
}

/** Everything the helper knows about one species. */
export interface SpeciesPlan {
  species: Species;
  /** "น้องหมา", "น้องแมว". */
  label: string;
  vaccines: VaccineInfo[];
  /** The puppy or kitten series and the first booster, in order. */
  visits: VisitTemplate[];
  /** The visit repeated after the last one in `visits`, for as long as the plan runs. */
  repeat: VisitTemplate;
  optional: OptionalVaccine[];
}

/** The spacing rules the timeline is computed with. */
export interface ScheduleRules {
  /** The least time between two visits of the series. */
  minGapDays: number;
  /** A visit this many days away or fewer is "due soon". */
  dueSoonDays: number;
  /** How far ahead of today the timeline runs. */
  horizonMonths: number;
}

export type VisitStatus = 'done' | 'overdue' | 'due' | 'upcoming';

/** One visit on a pet's own timeline. */
export interface PlannedVisit {
  key: string;
  title: string;
  /** `YYYY-MM-DD`, a calendar day in Bangkok. */
  date: string;
  status: VisitStatus;
  vaccines: VaccineInfo[];
  note?: string;
  /** For a catch-up visit standing in for several missed ones: the earliest missed date. */
  missedSince?: string;
  /** For a catch-up visit: how many scheduled visits it stands in for. */
  missedCount?: number;
}

/** A pet's timeline: what is taken as done, and what lies ahead (anything overdue first). */
export interface PetSchedule {
  done: PlannedVisit[];
  ahead: PlannedVisit[];
}

/** What the visitor typed, as calendar days. */
export interface ScheduleInput {
  species: Species;
  birthDate: string;
  lastVaccineDate?: string;
}

/** The clinic's LINE account, as the page shows it. */
export interface LineLink {
  href: string;
  /** The account as people type it, such as `@happypaws`. */
  handle: string;
  /** The add-friend link drawn as a QR code, for a visitor on a computer. */
  qrDataUrl?: string;
}

export interface ScheduleActions {
  /** Absent when the clinic has no LINE entry with a web link: the LINE card is then not drawn. */
  line?: LineLink;
  book: Record<Species, string>;
}
