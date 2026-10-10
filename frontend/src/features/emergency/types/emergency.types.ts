/** One sign that needs a vet right away. */
export interface WarningSign {
  id: string;
  title: string;
  detail: string;
}

/** One thing to do, or not to do, on the way to the clinic. */
export interface CareStep {
  id: string;
  title: string;
  detail: string;
}

/** A number to ring from the top of the page. */
export interface CallNumber {
  id: string;
  label: string;
  value: string;
  /** A `tel:` link. */
  href: string;
  /** Set when the clinic has several branches and the number is one branch's. */
  branchName?: string;
  /** True when the clinic has no emergency entry and this is its main phone instead. */
  fallback: boolean;
}

/** Whether a branch is open at this moment, by its opening hours and closures. */
export type OpenState =
  | { kind: 'open'; closes: string }
  /** Closed now. `day` is the clinic day it next opens, `offset` how many days from today. */
  | { kind: 'opens-later'; day: string; offset: number; opens: string }
  /** No opening in the days looked at. */
  | { kind: 'closed' };

export interface BranchOpenStatus {
  key: string;
  /** Set when the clinic has several branches. */
  branchName?: string;
  state: OpenState;
  /** The name of today's closure, when today is one. */
  holiday?: string;
}
