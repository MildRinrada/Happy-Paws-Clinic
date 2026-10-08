import { businessReport } from '../content/business-report';
import type { RetentionAnalytics } from '../types/analytics.types';
import { CohortTable } from './cohort-table';
import { ReportCard } from './report-card';
import { RetentionMonthTable } from './retention-month-table';

/** New and returning customers by month, and how each month's newcomers came back. */
export function RetentionReport({ retention }: { retention: RetentionAnalytics }) {
  const text = businessReport.retention;

  return (
    <ReportCard title={text.title} note={text.note}>
      <RetentionMonthTable months={retention.months} />
      <CohortTable cohorts={retention.cohorts} />
    </ReportCard>
  );
}
