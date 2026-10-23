import { onCustomerMerge } from '../customers/store';
import { couponRedemptions, inbox, packages, pointsEntries, threadMessages, threads } from './store';
import './billing-bridge';
import './crm-routes';
import './messaging-routes';
import './notification-routes';
import './integration-routes';

/**
 * The CRM module: crm (segments, coupons, packages, loyalty, due-back, referrals console),
 * messaging, notifications, automation, integrations and LINE. Helpers other modules may call are
 * in ./api.ts.
 */

/** A merge moves the duplicate's CRM rows to the surviving record; two threads become one. */
onCustomerMerge((survivingId, duplicateId) => {
  const move = (rows: ReturnType<typeof inbox>) => {
    let count = 0;
    for (const row of rows.where((r) => r.customer_id === duplicateId)) {
      rows.update(row.id, { customer_id: survivingId });
      count++;
    }
    return count;
  };
  const kept = threads().find((t) => t.customer_id === survivingId);
  const duplicate = threads().find((t) => t.customer_id === duplicateId);
  let movedThreads = 0;
  if (kept && duplicate) {
    for (const m of threadMessages().where((x) => x.thread_id === duplicate.id)) threadMessages().update(m.id, { thread_id: kept.id });
    threads().remove(duplicate.id);
    movedThreads = 1;
  } else {
    movedThreads = move(threads());
  }
  return {
    loyalty_entries: move(pointsEntries()),
    packages: move(packages()),
    coupon_redemptions: move(couponRedemptions()),
    notifications: move(inbox()),
    threads: movedThreads,
  };
});
