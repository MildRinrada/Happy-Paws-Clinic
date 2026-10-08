import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listInventoryAlerts } from '@/features/inventory/api/listInventoryAlerts';
import { AlertList } from '@/features/inventory/components/alert-list';
import { AlertsWindowForm } from '@/features/inventory/components/alerts-window-form';
import { BranchNote } from '@/features/inventory/components/branch-note';
import { alertsPage } from '@/features/inventory/content/alerts-page';
import { inventoryArea } from '@/features/inventory/content/inventory-tabs';
import { readAlertWindow } from '@/features/inventory/lib/alert-windows';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: alertsPage.metaTitle };

interface AlertsPageProps {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/** "ใกล้หมด ใกล้หมดอายุ ล็อตที่ต้องเรียกคืน" for the branch in view, worst first. */
export default async function InventoryAlertsPage({ searchParams }: AlertsPageProps) {
  const context = await loadStaffContext();

  if (!context?.can(Scope.InventoryRead)) {
    return <Alert tone="info">{alertsPage.noAccess}</Alert>;
  }

  if (!context.branch) {
    return <Alert tone="info">{inventoryArea.noBranch}</Alert>;
  }

  const branch = context.branch;
  const withinDays = readAlertWindow((await searchParams).within_days);
  const alerts = await authed(() => listInventoryAlerts(branch.id, withinDays));

  return (
    <div className="space-y-5">
      <PageHeader title={alertsPage.title} subtitle={alertsPage.subtitle} actions={<BranchNote name={branch.name} />} />
      <AlertsWindowForm withinDays={withinDays} />
      {alerts.length === 0 ? <EmptyState title={alertsPage.empty} /> : <AlertList alerts={alerts} />}
      {alerts.some((alert) => alert.kind === 'expired') && <p className="text-sm text-ink-600">{alertsPage.recallNote}</p>}
    </div>
  );
}
