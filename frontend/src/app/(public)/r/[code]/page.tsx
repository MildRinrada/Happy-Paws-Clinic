import type { Metadata } from 'next';
import { redirect } from 'next/navigation';
import { referralPaths } from '@/features/referrals/lib/referral-paths';
import { validReferralCode } from '@/features/referrals/lib/valid-referral-code';

export const metadata: Metadata = { robots: { index: false, follow: false } };

// Every visit asks the backend: the code's owner, or the programme, may have changed since.
export const dynamic = 'force-dynamic';

/**
 * A friend's invite link (docs/contract-extensions.md § 39). Never a page of its own: a code that
 * can be used goes on to `/book?ref=CODE`, anything else to `/book` without it, so a stale or mistyped
 * link still ends at the booking form and never at an error. Nothing about whose code it was is
 * shown or sent along.
 */
export default async function ReferralLinkPage({ params }: { params: Promise<{ code: string }> }) {
  const { code } = await params;

  redirect(referralPaths.book(await validReferralCode(code)));
}
