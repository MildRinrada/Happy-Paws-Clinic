import { petDetail } from '../../../content/pet-detail';
import type { PastVisitView } from '../../../lib/load-pet-visits';
import { recordsHref } from '../../../lib/portal-links';
import type { MyPet } from '../../../types/portal.types';
import { EmptyLine } from '../../shared/empty-line';
import { MoreLink } from '../../shared/more-link';
import { SectionCard } from '../../shared/section-card';
import { PastVisitRow } from './past-visit-row';
import { VisitCardActions } from './visit-card-actions';

/**
 * "มาตรวจล่าสุด": the pet's last few visits, newest first. The way to the full history sits at the
 * title's right from 1024px and as a button on a phone, and only for an owner who may see it.
 */
export function RecentVisitsCard({ pet, visits }: { pet: MyPet; visits: PastVisitView[] }) {
  const text = petDetail.visits;

  return (
    <SectionCard
      title={text.title}
      ruled
      aside={
        pet.may_view_history ? (
          <span className="hidden lg:inline">
            <MoreLink label={text.history} href={recordsHref(pet.id)} />
          </span>
        ) : undefined
      }
    >
      {visits.length === 0 ? (
        <div className="py-4">
          <EmptyLine>{text.empty}</EmptyLine>
        </div>
      ) : (
        <ul className="divide-y divide-ink-100">
          {visits.map((visit) => (
            <PastVisitRow key={visit.id} visit={visit} />
          ))}
        </ul>
      )}
      <VisitCardActions pet={pet} />
    </SectionCard>
  );
}
