/** One branch's free start times for the featured service, today and tomorrow, earliest first. */
export interface BranchFreeTimes {
  branchId: string;
  /** Shown only when the clinic has more than one branch. */
  branchName?: string;
  /** Instants as the API sends them (`2026-10-10T14:30:00+07:00`). */
  times: string[];
}

/**
 * What the home page's "ว่างเร็วสุด" block draws from: read on the server, kept for about a minute,
 * and filtered against the clock each time it is drawn.
 */
export interface NextFreeTimes {
  serviceId: string;
  serviceName: string;
  branches: BranchFreeTimes[];
  /** When the API was read (epoch milliseconds), so a read kept too long is not shown. */
  readAt: number;
}

/** The earliest time still ahead at one branch. */
export interface NextFreeSlot {
  branchId: string;
  branchName?: string;
  startsAt: string;
}
