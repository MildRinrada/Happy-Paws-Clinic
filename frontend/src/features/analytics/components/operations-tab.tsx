import { Alert } from '@/components/ui/alert';
import { reportsPage } from '../content/reports-page';
import type { OperationsReports } from '../lib/load-reports';
import { AppointmentStatsReport } from './appointment-stats-report';
import { QueueStatsReport } from './queue-stats-report';
import { WorkloadReport } from './workload-report';

/** "สถิติการทำงาน": appointments, queue and workload over the chosen range. */
export function OperationsTab({ reports }: { reports: OperationsReports }) {
  return (
    <div className="space-y-4">
      {reports.appointments ? <AppointmentStatsReport stats={reports.appointments} /> : <Alert>{reportsPage.refused}</Alert>}
      {reports.queue ? <QueueStatsReport stats={reports.queue} /> : <Alert>{reportsPage.refused}</Alert>}
      {reports.workload ? <WorkloadReport stats={reports.workload} /> : <Alert>{reportsPage.refused}</Alert>}
    </div>
  );
}
