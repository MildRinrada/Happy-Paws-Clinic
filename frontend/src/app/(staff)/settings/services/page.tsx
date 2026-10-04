import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listManagedServices, listServiceCategories } from '@/features/service-catalog/api/manage-services';
import { AddServiceForm } from '@/features/service-catalog/components/settings/add-service-form';
import { ServiceCategoryCard } from '@/features/service-catalog/components/settings/service-category-card';
import { serviceSettings } from '@/features/service-catalog/content/service-settings';
import { listStaffMembers } from '@/features/staff/api/listStaffMembers';
import { optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: serviceSettings.metaTitle };

/** The whole catalogue by heading: what each service is, what it costs, and who can book it. */
export default async function ServiceSettingsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.ServiceManage)) {
    return <Alert tone="info">{serviceSettings.noAccess}</Alert>;
  }

  const [services, categories, members] = await Promise.all([
    listManagedServices(),
    listServiceCategories(),
    // Naming who performs a service needs the staff list, which is `staff:read`.
    context.can(Scope.StaffRead) ? optional(listStaffMembers()) : Promise.resolve(null),
  ]);

  const staff = members && Object.fromEntries(members.map((member) => [member.id, `${member.display_name} (${member.staff_type})`]));
  const branches = Object.fromEntries(context.branches.map((branch) => [branch.id, branch.name]));
  const shared = { categories, staff, branches, today: clinicDay(new Date()) };

  return (
    <div className="space-y-6">
      <PageHeader title={serviceSettings.title} subtitle={serviceSettings.subtitle} />
      <AddServiceForm categories={categories} />

      {categories.map((category) => (
        <ServiceCategoryCard
          key={category.code}
          category={category}
          services={services.filter((service) => service.category_code === category.code)}
          {...shared}
        />
      ))}
    </div>
  );
}
