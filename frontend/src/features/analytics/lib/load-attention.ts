import type { AttendanceRisk } from '@/features/appointments/types/attendance-risk.types';
import { Scope } from '@/lib/auth/scopes';
import type { AttentionItem, AttentionView } from '../types/today.types';
import { awaitingReplies, longWaits, pendingReviews, riskyBookings, stockProblems } from './attention-probes';
import { tolerant } from './tolerant-read';

interface AttentionSources {
  can: (scope: string) => boolean;
  branchId: string;
  today: string;
  now: Date;
  /** The day's attendance risk, read once for the page: `null` when that read failed. */
  risk: Promise<AttendanceRisk | null>;
}

type Probe = () => Promise<AttentionItem | null>;

/**
 * Every line of "ต้องจัดการตอนนี้" this session may read, side by side. A line is asked for only
 * with the scope its own page needs; one that is refused or fails is counted in `failed` and left
 * out, and the rest still show. `null` when the session may read none of them, so the box is not
 * drawn at all rather than claiming there is nothing to do.
 */
export async function loadAttention({ can, branchId, today, now, risk }: AttentionSources): Promise<AttentionView | null> {
  const probes: Probe[] = [];

  if (can(Scope.QueueRead)) probes.push(() => longWaits(branchId, now));
  if (can(Scope.MessageRead)) probes.push(() => awaitingReplies());
  if (can(Scope.ContentManage)) probes.push(() => pendingReviews());
  if (can(Scope.InventoryRead)) probes.push(() => stockProblems(branchId));
  if (can(Scope.AppointmentRead)) {
    probes.push(async () => {
      const day = await risk;
      if (!day) throw new Error('attendance risk unavailable');
      return riskyBookings(day, today, now);
    });
  }

  if (probes.length === 0) {
    return null;
  }

  // Each answer is boxed so that "nothing waiting" (an item of null) stays apart from "could not
  // read" (null from `tolerant`).
  const answers = await Promise.all(probes.map((probe) => tolerant(async () => ({ item: await probe() }))));

  return {
    items: answers.flatMap((answer) => (answer?.item ? [answer.item] : [])),
    checked: answers.filter((answer) => answer !== null).length,
    failed: answers.filter((answer) => answer === null).length,
  };
}
