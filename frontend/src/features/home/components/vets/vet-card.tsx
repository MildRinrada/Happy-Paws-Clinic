import Link from 'next/link';
import { ImageSlot } from '@/components/shared/image-slot';
import { memberHref, memberSummary } from '@/features/team/lib/team-text';
import type { TeamMember } from '@/features/team/types/team.types';
import { vetProfileLabel } from '../../content/vets';
import { JobTitleBadge } from './job-title-badge';

/** One vet: photo, name, field and years, their title, and a link to them on the team page. */
export function VetCard({ vet }: { vet: TeamMember }) {
  return (
    <li className="overflow-hidden rounded-2xl border border-site-border bg-white">
      <ImageSlot
        src={vet.photo_path}
        alt={vet.display_name}
        label="รูปสัตวแพทย์"
        sizes="(min-width: 1024px) 22vw, (min-width: 640px) 50vw, 100vw"
        className="h-[196px] border-x-0 border-t-0"
      />

      <div className="px-5 pb-6 pt-5">
        <h3 className="font-bold">{vet.display_name}</h3>
        <p className="mt-2.5 text-[13px] text-site-muted">{memberSummary(vet)}</p>

        <div className="mt-4 flex items-center justify-between gap-3">
          {vet.job_title ? <JobTitleBadge title={vet.job_title} /> : <span />}
          <Link href={memberHref(vet.id)} className="whitespace-nowrap text-[13px] font-bold text-site-teal hover:underline">
            {vetProfileLabel}
          </Link>
        </div>
      </div>
    </li>
  );
}
