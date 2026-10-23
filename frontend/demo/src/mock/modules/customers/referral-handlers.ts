import type { MyReferral } from '@/features/referrals/types/referral.types';
import { hasRoute, page, requireCustomer, requireStaff, route } from '../../core/http';
import { findReferralCode, referralCodeOf, referralLink, referrals, referralSettings } from './referrals';
import { customerName, customers } from './store';

/** Refer-a-friend (docs/contract-extensions.md § 39): the public code check, the portal card, the console list. */

route('GET', '/referrals/:code', (ctx) => {
  const owner = findReferralCode(ctx.params.code ?? '');
  const customer = owner ? customers().get(owner.customer_id) : undefined;
  return { valid: Boolean(referralSettings().enabled && customer && customer.is_active && !customer.merged_into_customer_id) };
});

route('GET', '/portal/referral', (ctx) => {
  const auth = requireCustomer(ctx);
  const settings = referralSettings();
  const code = referralCodeOf(auth.customerId);
  const mine = referrals().where((row) => row.referrer.customer_id === auth.customerId);
  const month = new Date().toISOString().slice(0, 7);
  const answer: MyReferral = {
    enabled: settings.enabled,
    code: code.code,
    link: referralLink(code.code),
    invited: mine.filter((row) => row.status !== 'rejected').length,
    rewarded: mine.filter((row) => row.status === 'rewarded').length,
    pending: mine.filter((row) => row.status === 'pending').length,
    points_earned: mine.reduce((sum, row) => sum + (row.status === 'rewarded' ? row.referrer_points : 0), 0),
    monthly_cap: settings.monthly_cap,
    rewarded_this_month: mine.filter((row) => row.status === 'rewarded' && (row.rewarded_at ?? '').startsWith(month)).length,
    referrer_points: settings.referrer_points,
    friend_points: settings.friend_points,
  };
  return answer;
});

// The console list sits on the CRM page; the crm module may answer it instead (its handlers load first).
if (!hasRoute('GET', '/staff/referrals')) {
  route('GET', '/staff/referrals', (ctx) => {
    requireStaff(ctx, 'crm:read');
    const customer = ctx.query.get('customer');
    const rows = referrals()
      .where((row) => !customer || row.referrer.customer_id === customer || row.friend.customer_id === customer)
      .sort((a, b) => b.created_at.localeCompare(a.created_at))
      .map((row) => ({
        ...row,
        referrer: { customer_id: row.referrer.customer_id, name: customerName(row.referrer.customer_id) ?? row.referrer.name },
        friend: { customer_id: row.friend.customer_id, name: customerName(row.friend.customer_id) ?? row.friend.name },
      }));
    const answer = page(rows, ctx.query, 25);
    return { data: answer.data, page: { next_cursor: answer.page.next_cursor ?? null } };
  });
}
