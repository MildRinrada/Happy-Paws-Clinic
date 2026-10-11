import Link from 'next/link';
import { SiteCard } from '@/components/site/site-card';
import type { Branch } from '@/features/clinic/types/clinic.types';
import { aboutVisit } from '../content/about-page';

/** One branch: its name, where it is, and how many exam rooms it has. */
export function BranchCard({ branch }: { branch: Branch }) {
  return (
    <SiteCard as="li" tone="whiteSoft">
      <h4 className="text-lg font-bold text-ink-900">{branch.name}</h4>
      {branch.address ? (
        <address className="mt-1 text-[15px] not-italic text-site-muted">{branch.address}</address>
      ) : (
        <p className="mt-1 text-[15px] text-site-muted">{aboutVisit.noAddress}</p>
      )}
      {branch.exam_rooms.length > 0 && (
        <p className="mt-2 text-sm text-ink-700">{aboutVisit.roomsLabel(branch.exam_rooms.length)}</p>
      )}
      <Link href={aboutVisit.mapLink.href} className="mt-3 inline-block text-sm font-semibold text-site-teal hover:underline">
        {aboutVisit.mapLink.label}
      </Link>
    </SiteCard>
  );
}
