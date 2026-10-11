import { SectionHeading } from '@/components/site/section-heading';
import type { Branch, OpeningHours } from '@/features/clinic/types/clinic.types';
import { emergencyPage } from '../content/emergency-page';
import { clinicClock } from '../lib/clinic-clock';
import { branchStatuses } from '../lib/open-status';
import { OpenStatusCard } from './open-status-card';

interface OpenStatusSectionProps {
  /** `null` when the clinic has not set its hours, or they could not be read. */
  hours: OpeningHours | null;
  branches: Branch[];
  /** The moment the page was drawn: the page is rendered per request, so this is the visit. */
  now: Date;
}

/** "ตอนนี้เปิดอยู่ไหม": each branch's state at the moment the page was asked for. */
export function OpenStatusSection({ hours, branches, now }: OpenStatusSectionProps) {
  const text = emergencyPage.status;
  const statuses = hours ? branchStatuses(hours, branches, now) : [];
  const anyClosed = statuses.some((status) => status.state.kind !== 'open');

  return (
    <section aria-labelledby="open-status-heading" className="space-y-4">
      <SectionHeading id="open-status-heading">{text.heading}</SectionHeading>

      {statuses.length === 0 ? (
        <p className="text-[15px] text-site-muted">{text.unknown}</p>
      ) : (
        <>
          <ul className={`grid gap-4 ${statuses.length > 1 ? 'sm:grid-cols-2' : 'max-w-[560px]'}`}>
            {statuses.map((status) => (
              <OpenStatusCard key={status.key} status={status} />
            ))}
          </ul>
          {anyClosed && <p className="max-w-[640px] text-[15px] font-semibold text-site-alert">{text.afterHours}</p>}
          <p className="text-sm text-site-muted">{text.checkedAt(clinicClock(now).time)}</p>
        </>
      )}
    </section>
  );
}
