import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/components/site/section-heading';
import { inWeekOrder } from '@/features/clinic/lib/weekdays';
import { aboutVisit } from '../content/about-page';
import { clinicToday, type VisitInfo } from '../lib/load-visit-info';
import { BranchCard } from './branch-card';
import { HolidayList } from './holiday-list';
import { OpeningHoursTable } from './opening-hours-table';

/**
 * Where and when to come, read from the API: the branches on the left, the week's hours and any
 * coming closures on the right. If the API is away, one line says so in their place.
 */
export function VisitSection({ info }: { info: VisitInfo | null }) {
  return (
    <section id="visit" aria-labelledby="visit-heading" className="scroll-mt-6 bg-site-soft py-12">
      <SiteContainer>
        <SectionHeading id="visit-heading">{aboutVisit.heading}</SectionHeading>

        {info === null ? (
          <p className="mt-4 text-[15px] text-site-muted">{aboutVisit.unavailable}</p>
        ) : (
          <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_380px]">
            <div>
              <h3 className="text-sm font-semibold text-site-muted">{aboutVisit.branchesLabel}</h3>
              <ul className="mt-3 grid gap-4 sm:grid-cols-2 lg:grid-cols-1">
                {info.branches.map((branch) => (
                  <BranchCard key={branch.id} branch={branch} />
                ))}
              </ul>
            </div>

            <div className="space-y-4">
              <h3 className="text-sm font-semibold text-site-muted">{aboutVisit.hoursLabel}</h3>
              {info.hours.configured ? (
                <OpeningHoursTable days={inWeekOrder(info.hours.days)} today={clinicToday()} />
              ) : (
                <p className="text-[15px] text-site-muted">{aboutVisit.notConfigured}</p>
              )}
              <HolidayList holidays={info.hours.holidays} branches={info.branches} />
            </div>
          </div>
        )}
      </SiteContainer>
    </section>
  );
}
