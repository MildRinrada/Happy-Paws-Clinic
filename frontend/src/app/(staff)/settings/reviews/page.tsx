import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { withCursor } from '@/features/crm/lib/read-list-query';
import { listStaffReviews } from '@/features/reviews/api/staffReviews';
import { ReviewStatusTabs } from '@/features/reviews/components/staff/review-status-tabs';
import { StaffReviewList } from '@/features/reviews/components/staff/staff-review-list';
import { staffReviewsText } from '@/features/reviews/content/staff-reviews';
import { readReviewQuery } from '@/features/reviews/lib/read-review-query';
import { reviewPaths } from '@/features/reviews/lib/review-paths';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: staffReviewsText.metaTitle };

/**
 * Customer reviews waiting for, or past, a decision (docs/contract-extensions.md § 36), on
 * `content:manage`. Staff publish, keep off for a listed reason, or answer; the customer's words
 * cannot be changed here, and every decision is audited with the staff id.
 */
export default async function ReviewsSettingsPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.ContentManage)) {
    return <Alert tone="info">{staffReviewsText.noAccess}</Alert>;
  }

  const query = readReviewQuery(params);
  const page = await authed(() => listStaffReviews(query));
  const filters = { status: query.status };

  return (
    <div className="space-y-6">
      <PageHeader title={staffReviewsText.title} subtitle={staffReviewsText.subtitle} />
      <StaffReviewList
        reviews={page.data}
        tabs={<ReviewStatusTabs current={query.status} />}
        nextHref={page.page.next_cursor ? withCursor(reviewPaths.staff, filters, page.page.next_cursor) : undefined}
        firstHref={query.cursor ? withCursor(reviewPaths.staff, filters) : undefined}
      />
    </div>
  );
}
