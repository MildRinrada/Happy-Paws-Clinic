import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { buttonClass } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listContacts } from '@/features/clinic/api/contacts';
import { AddContactPanel } from '@/features/clinic/components/settings/add-contact-panel';
import { ContactRow } from '@/features/clinic/components/settings/contact-row';
import { contactSettings } from '@/features/clinic/content/contact-settings';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: contactSettings.metaTitle };

/**
 * The clinic's contact entries: add, change, remove. What is saved here is what the contact page,
 * the footer and the home page show, on the next request.
 */
export default async function ContactSettingsPage() {
  const context = await loadStaffContext();
  const text = contactSettings;

  if (!context?.can(Scope.SettingsWrite)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  const contacts = await listContacts();
  const branches = context.branches.map(({ id, name }) => ({ id, name }));
  const nextOrder = (contacts.at(-1)?.sort_order ?? 0) + 10;

  return (
    <div className="max-w-4xl space-y-6">
      <PageHeader
        title={text.title}
        subtitle={text.subtitle}
        actions={
          <Link href="/contact" target="_blank" className={buttonClass('secondary', 'sm')}>
            {text.viewPublic}
          </Link>
        }
      />

      <Card>
        {contacts.length === 0 ? (
          <p className="px-5 py-6 text-sm text-ink-500">{text.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {contacts.map((contact) => (
              <ContactRow key={`${contact.id}:${contact.version}`} contact={contact} branches={branches} />
            ))}
          </ul>
        )}
      </Card>

      <AddContactPanel branches={branches} nextOrder={nextOrder} />
    </div>
  );
}
