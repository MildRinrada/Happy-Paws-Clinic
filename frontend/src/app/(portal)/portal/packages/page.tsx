import type { Metadata } from 'next';
import { PackageGroup } from '@/features/portal/components/packages/package-group';
import { PackagesEmpty } from '@/features/portal/components/packages/packages-empty';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { portalPackages } from '@/features/portal/content/portal-packages';
import { loadPackages } from '@/features/portal/lib/load-packages';

export const metadata: Metadata = { title: `${portalPackages.title} · Happy Paws Clinic` };

/** The owner's packages: what can still be used, then what is used up or expired. */
export default async function PortalPackagesPage() {
  const packages = await loadPackages();
  const text = portalPackages;

  return (
    <div className="space-y-6">
      <PageHeading title={text.title} intro={text.intro} />

      {packages.length === 0 ? (
        <PackagesEmpty />
      ) : (
        <>
          <PackageGroup title={text.usable} items={packages.filter((item) => item.status === 'active')} />
          <PackageGroup title={text.finished} items={packages.filter((item) => item.status !== 'active')} />
        </>
      )}
    </div>
  );
}
