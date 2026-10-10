/**
 * `GET /queue/wait-estimate?branch=`: about how long a walk-in waits at one branch now. Counts
 * only (docs/contract-extensions.md § 37). The range is absent while the branch is closed.
 */
export interface WaitEstimate {
  branch_id: string;
  open_now: boolean;
  /** Tickets still waiting to be called today. */
  waiting: number;
  vets_on_duty: number;
  estimate_minutes_low?: number;
  estimate_minutes_high?: number;
  /** When a closed branch opens next, within two weeks; absent when open or unknown. */
  opens_at?: string;
  updated_at: string;
}

/** One line on the page: a branch's estimate, with its name when the clinic has several. */
export interface BranchWait {
  branchId: string;
  branchName?: string;
  estimate: WaitEstimate;
}

/** What the block shows, and when it was read, so a stale read can be hidden. */
export interface WaitEstimates {
  branches: BranchWait[];
  readAt: number;
}
