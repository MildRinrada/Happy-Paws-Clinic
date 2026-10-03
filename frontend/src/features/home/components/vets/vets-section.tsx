import Link from 'next/link';
import { loadDepartmentMembers } from '@/features/team/lib/load-team';
import { allTeamLink, homeVetCount, vetsHeading, vetsUnavailable } from '../../content/vets';
import { SectionContainer } from '../shared/section-container';
import { SectionHeading } from '../shared/section-heading';
import { VetCard } from './vet-card';

/**
 * The clinic's vets, one card each, read from the API. The section is left out when the clinic
 * has not listed any vet on the website yet.
 */
export async function VetsSection() {
  const vets = await loadDepartmentMembers('veterinary', homeVetCount);

  if (vets !== null && vets.length === 0) {
    return null;
  }

  return (
    <section id="vets" aria-labelledby="vets-heading" className="scroll-mt-6 pt-12">
      <SectionContainer>
        <div className="flex items-end justify-between gap-4">
          <SectionHeading id="vets-heading">{vetsHeading}</SectionHeading>
          <Link href={allTeamLink.href} className="whitespace-nowrap text-sm font-bold text-site-teal hover:underline">
            {allTeamLink.label}
          </Link>
        </div>

        {vets === null ? (
          <p className="mt-7 text-[15px] text-site-muted">{vetsUnavailable}</p>
        ) : (
          <ul className="mt-7 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {vets.map((vet) => (
              <VetCard key={vet.id} vet={vet} />
            ))}
          </ul>
        )}
      </SectionContainer>
    </section>
  );
}
