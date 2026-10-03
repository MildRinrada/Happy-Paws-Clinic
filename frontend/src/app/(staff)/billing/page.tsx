import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { buttonClass } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { InvoiceTable } from '@/features/billing/components/invoice-table';
import { loadInvoiceList, type InvoiceListView } from '@/features/billing/lib/load-billing-view';
import type { InvoiceStatus } from '@/features/billing/types/billing.types';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: 'เก็บเงิน · Happy Paws Clinic' };

/** The tabs a cashier works from. "ค้างชำระ" is first because that is who is standing at the desk. */
const tabs: { key: string; label: string; status?: InvoiceStatus }[] = [
  { key: 'open', label: 'ค้างชำระ', status: 'open' },
  { key: 'partially_paid', label: 'ชำระบางส่วน', status: 'partially_paid' },
  { key: 'paid', label: 'ชำระแล้ว', status: 'paid' },
  { key: 'all', label: 'ทั้งหมด' },
];

/**
 * The cash desk's list of bills.
 *
 * This screen exists because of two endpoints that are not in the API reference. The contract's
 * billing section is six POSTs — a bill raised by a vet closing a case upstairs had no way to be
 * found by the cashier downstairs. See docs/contract-extensions.md § 7.
 */
export default async function BillingPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; cursor?: string }>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.branch) {
    return <EmptyState title="ยังไม่มีสาขาที่บัญชีนี้เข้าถึงได้">ติดต่อผู้ดูแลระบบเพื่อกำหนดสาขา</EmptyState>;
  }

  const { branch } = context;

  if (!context.can(Scope.InvoiceWrite)) {
    return <Alert tone="info">บัญชีนี้ไม่มีสิทธิ์ดูบิล (invoice:write)</Alert>;
  }

  const active = tabs.find((tab) => tab.key === params.status) ?? tabs[0]!;

  let list: InvoiceListView;

  try {
    list = await authed(() =>
      loadInvoiceList({ branchId: branch.id, status: active.status, cursor: params.cursor }),
    );
  } catch (error) {
    if (error instanceof ApiError && (error.status === 403 || error.status === 422)) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  return (
    <div className="space-y-6">
      <header>
        <h1 className="font-display text-2xl font-semibold text-ink-900">เก็บเงิน</h1>
        <p className="text-sm text-ink-600">{branch.name}</p>
      </header>

      <nav aria-label="กรองตามสถานะ" className="flex flex-wrap gap-2">
        {tabs.map((tab) => (
          <Link
            key={tab.key}
            href={`/billing?status=${tab.key}`}
            aria-current={tab.key === active.key ? 'page' : undefined}
            className={buttonClass(tab.key === active.key ? 'primary' : 'secondary', 'sm')}
          >
            {tab.label}
          </Link>
        ))}
      </nav>

      <InvoiceTable rows={list.rows} />

      {list.nextCursor && (
        <Link
          href={`/billing?status=${active.key}&cursor=${encodeURIComponent(list.nextCursor)}`}
          className={buttonClass('secondary', 'sm')}
        >
          หน้าถัดไป
        </Link>
      )}
    </div>
  );
}
