import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { Alert } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { DiscountForm } from '@/features/billing/components/discount/discount-form';
import { InvoiceDocument } from '@/features/billing/components/invoice-document';
import { PaymentForm } from '@/features/billing/components/payment-form';
import { ReceiptPanel } from '@/features/billing/components/receipt-panel';
import { RefundForm } from '@/features/billing/components/refund/refund-form';
import { VoidForm } from '@/features/billing/components/void-form';
import { discountForm } from '@/features/billing/content/discount-form';
import { refundForm } from '@/features/billing/content/refund-form';
import { invoiceStatusLabel, isCollectible, isRefundable } from '@/features/billing/lib/invoice-labels';
import { loadInvoiceDetail, type InvoiceDetailView } from '@/features/billing/lib/load-billing-view';
import { loadStaffOptions } from '@/features/billing/lib/load-staff-options';
import { loadPointsBalance } from '@/features/crm/lib/load-points-balance';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { formatDateTime } from '@/lib/format/datetime';

export const metadata: Metadata = { title: 'บิล · Happy Paws Clinic' };

/**
 * One bill: what is owed, and the three things a cash desk does about it.
 *
 * The forms are shown by state and by scope, and neither of those is what enforces anything. A
 * paid bill shows no payment form because there is nothing left to take; a session without
 * `payment:collect` shows none because the backend would refuse it. Both rules live on the
 * backend — this page only avoids offering a button that would be answered with an error.
 */
export default async function InvoicePage({ params }: { params: Promise<{ id: string }> }) {
  const [context, { id }] = await Promise.all([loadStaffContext(), params]);

  if (!context) {
    notFound();
  }

  let view: InvoiceDetailView;

  try {
    view = await authed(() => loadInvoiceDetail(id));
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    if (error instanceof ApiError && error.status === 403) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  const { invoice, customer } = view;
  const status = invoiceStatusLabel[invoice.status];

  // Each form is offered by state and by the contract's own scope for it: `invoice:discount` to
  // reduce what is owed, `payment:refund` to give money back. The backend checks both again.
  const canDiscount = isCollectible(invoice.status) && context.can(Scope.InvoiceDiscount);
  const canRefund = isRefundable(invoice.status) && invoice.paid_satang > 0 && context.can(Scope.PaymentRefund);

  // Staff names, for the approver picker and for naming approvers on the bill. Read only when used.
  const staff =
    canDiscount || invoice.discounts.length > 0
      ? await authed(() => loadStaffOptions(invoice.branch_id, context.can(Scope.StaffRead)))
      : null;
  const staffNames = new Map(staff?.map((member) => [member.id, member.name]));
  // The points hint beside "แลกแต้ม", for a session that may read them. A failed read leaves it out.
  const pointsBalance = canDiscount && context.can(Scope.CrmRead) ? await loadPointsBalance(invoice.customer_id) : undefined;

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <Link href="/billing" className="no-print text-sm text-brand-700 hover:underline">
          ← กลับไปรายการบิล
        </Link>

        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-display text-2xl font-semibold text-ink-900">{invoice.number}</h1>
          <Badge tone={status.tone}>{status.text}</Badge>
        </div>

        <p className="text-sm text-ink-600">
          {customer?.customer.display_name ?? invoice.customer_id}
          {customer && ` · ${customer.customer.phone}`} · ออกเมื่อ {formatDateTime(invoice.issued_at)}
        </p>

        {invoice.status === 'void' && invoice.void_reason && (
          <Alert tone="info">บิลนี้ถูกยกเลิก · {invoice.void_reason}</Alert>
        )}
      </header>

      <div className="grid gap-6 lg:grid-cols-[1fr_22rem]">
        <Card>
          <CardHeader title="รายการ" />
          <CardBody>
            <InvoiceDocument invoice={invoice} staffNames={staffNames} />
          </CardBody>
        </Card>

        <div className="space-y-6">
          {isCollectible(invoice.status) && context.can(Scope.PaymentCollect) && (
            <Card className="no-print">
              <CardHeader title="รับชำระ" />
              <CardBody>
                {/* Keyed by version: once a tender lands, the form starts again from the new balance. */}
                <PaymentForm
                  key={invoice.version}
                  invoiceId={invoice.id}
                  version={invoice.version}
                  balanceSatang={invoice.balance_satang}
                />
              </CardBody>
            </Card>
          )}

          {canDiscount && (
            <Card className="no-print">
              <CardHeader title={discountForm.heading} />
              <CardBody>
                {/* Not keyed by version: after a conflict the form keeps what was typed and the message. */}
                <DiscountForm
                  invoice={invoice}
                  self={{ id: context.staffId, name: context.me.display_name }}
                  staff={staff}
                  pointsBalance={pointsBalance}
                />
              </CardBody>
            </Card>
          )}

          {context.can(Scope.ReceiptIssue) && invoice.status !== 'void' && (
            <Card>
              <CardHeader title="ใบเสร็จ" />
              <CardBody>
                <ReceiptPanel
                  invoiceId={invoice.id}
                  receipt={invoice.receipt}
                  hasPayments={invoice.payments.length > 0}
                  refundedSatang={invoice.refunded_satang}
                />
              </CardBody>
            </Card>
          )}

          {canRefund && (
            <Card className="no-print">
              <CardHeader title={refundForm.heading} />
              <CardBody>
                <RefundForm invoice={invoice} />
              </CardBody>
            </Card>
          )}

          {invoice.status === 'open' && context.can(Scope.InvoiceWrite) && (
            <div className="no-print">
              <VoidForm invoiceId={invoice.id} version={invoice.version} />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
