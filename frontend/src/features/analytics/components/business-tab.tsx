import { Alert } from '@/components/ui/alert';
import { businessReport } from '../content/business-report';
import { reportsPage } from '../content/reports-page';
import type { BusinessReports } from '../lib/load-reports';
import { RetentionReport } from './retention-report';
import { RevenueReport } from './revenue-report';
import { ServiceReport } from './service-report';
import { StaffPerformanceReport } from './staff-performance-report';

/**
 * "ธุรกิจ": revenue, services and retention on `analytics:read`, staff performance on
 * `analytics:hr`. A session with only one of the two sees only its half.
 */
export function BusinessTab({ reports, canRead }: { reports: BusinessReports; canRead: boolean }) {
  return (
    <div className="space-y-4">
      {canRead && (
        <>
          {reports.revenue ? <RevenueReport revenue={reports.revenue} /> : <Alert>{reportsPage.refused}</Alert>}
          {reports.services ? <ServiceReport report={reports.services} /> : <Alert>{reportsPage.refused}</Alert>}
          {reports.retention ? <RetentionReport retention={reports.retention} /> : <Alert>{reportsPage.refused}</Alert>}
        </>
      )}
      {reports.staff === undefined ? (
        <Alert tone="info">{businessReport.staff.noAccess}</Alert>
      ) : reports.staff ? (
        <StaffPerformanceReport performance={reports.staff} />
      ) : (
        <Alert>{reportsPage.refused}</Alert>
      )}
    </div>
  );
}
