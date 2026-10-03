import type { Metadata } from 'next';
import { InvitationFlow } from '@/features/staff/components/invitation/invitation-flow';
import { invitationPage } from '@/features/staff/content/invitation-page';

export const metadata: Metadata = {
  title: `${invitationPage.metaTitle} · Happy Paws Clinic`,
  // The address carries an invitation id. Nothing here is for a search engine.
  robots: { index: false, follow: false },
};

/**
 * Where an invitation link lands: `/invitations/{id}#token={token}`, the shape the backend's
 * `Staff:Invitations:AcceptUrlTemplate` builds. No session is needed or created here; the new
 * member signs in afterwards on `/login`.
 */
export default async function InvitationPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <main className="grid min-h-dvh place-items-center px-4 py-10">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center">
          <p className="font-display text-2xl font-semibold text-brand-800">{invitationPage.brand}</p>
          <h1 className="mt-1 text-sm text-ink-600">{invitationPage.heading}</h1>
        </div>

        <div className="rounded-2xl border border-ink-200 bg-white p-6 shadow-sm">
          <InvitationFlow invitationId={id} />
        </div>

        <p className="mt-4 text-center text-xs text-ink-500">{invitationPage.footnote}</p>
      </div>
    </main>
  );
}
