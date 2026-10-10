import type { Metadata } from 'next';
import { SiteContainer } from '@/components/layout/site-container';
import { pageMetadata } from '@/features/seo/lib/page-metadata';
import { PrintStyles } from '@/features/vaccine-schedule/components/print-styles';
import { SchedulePlanner } from '@/features/vaccine-schedule/components/schedule-planner';
import { VaccineScheduleHero } from '@/features/vaccine-schedule/components/vaccine-schedule-hero';
import { VaccineScheduleIntro } from '@/features/vaccine-schedule/components/vaccine-schedule-intro';
import { vaccinePage } from '@/features/vaccine-schedule/content/vaccine-schedule-page';
import { loadScheduleActions } from '@/features/vaccine-schedule/lib/load-schedule-actions';

export const metadata: Metadata = pageMetadata(vaccinePage.meta);

/**
 * "ตารางวัคซีนน้อง": a visitor picks dog or cat, types a birth date, and gets a vaccine timeline,
 * computed in the browser and sent nowhere. The schedule is `features/vaccine-schedule/content/
 * vaccine-plans.ts`; only the LINE account and the vaccination service to book come from the API.
 */
export default async function VaccineSchedulePage() {
  const actions = await loadScheduleActions();

  return (
    <main data-print-root>
      <PrintStyles />
      <VaccineScheduleHero />

      <div className="py-10 lg:py-12">
        <SiteContainer>
          <SchedulePlanner actions={actions} />
        </SiteContainer>
      </div>

      <VaccineScheduleIntro />
    </main>
  );
}
