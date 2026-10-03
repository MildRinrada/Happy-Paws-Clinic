import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { RegisterCustomerForm } from '@/features/customers/components/register-customer-form';
import { registerCustomer } from '@/features/customers/content/register-customer';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: registerCustomer.metaTitle };

/** A new customer at the counter. Their pets are added on the page this leads to. */
export default async function NewCustomerPage() {
  const context = await loadStaffContext();
  const text = registerCustomer;

  if (!context?.can(Scope.CustomerWrite)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  return (
    <div className="max-w-xl space-y-6">
      <div className="space-y-2">
        <BackLink href={customerPaths.list}>{text.back}</BackLink>
        <PageHeader title={text.title} subtitle={text.subtitle} />
      </div>

      <Card>
        <CardBody className="py-5">
          <RegisterCustomerForm />
        </CardBody>
      </Card>
    </div>
  );
}
