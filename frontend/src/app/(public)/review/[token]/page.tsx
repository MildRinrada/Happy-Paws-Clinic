import type { Metadata } from 'next';
import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/components/site/section-heading';
import { InvitationBody } from '@/features/reviews/components/invitation/invitation-body';
import { invitationPage } from '@/features/reviews/content/invitation-page';
import { loadInvitation } from '@/features/reviews/lib/load-invitation';

// A one-time link: never prerendered or cached, and never listed by a search engine.
export const dynamic = 'force-dynamic';

export const metadata: Metadata = {
  title: invitationPage.metaTitle,
  robots: { index: false, follow: false },
  referrer: 'no-referrer',
};

interface Props {
  params: Promise<{ token: string }>;
}

/**
 * The page a review invitation links to (docs/contract-extensions.md § 36), for a customer with no
 * portal account. Public: the token in the address is the credential, for one review of one visit
 * within 30 days. It shows the visit's day, service and pet, and nothing else about anyone. No
 * consent is asked: writing a review is the customer's own act.
 */
export default async function ReviewInvitationPage({ params }: Props) {
  const { token } = await params;
  const load = await loadInvitation(token);

  return (
    <main className="py-12">
      <SiteContainer>
        <div className="max-w-[560px] space-y-6">
          <SectionHeading id="review-heading">{invitationPage.title}</SectionHeading>
          <InvitationBody load={load} token={token} />
        </div>
      </SiteContainer>
    </main>
  );
}
