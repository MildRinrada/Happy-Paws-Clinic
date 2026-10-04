import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { RolesCard } from '@/features/staff/components/roles/roles-card';
import { InviteStaffForm } from '@/features/staff/components/settings/invite-staff-form';
import { StaffFilterForm } from '@/features/staff/components/settings/staff-filter-form';
import { StaffListCard } from '@/features/staff/components/settings/staff-list-card';
import { StaffStatusTabs } from '@/features/staff/components/settings/staff-status-tabs';
import { roleSettings } from '@/features/staff/content/role-settings';
import { staffSettings } from '@/features/staff/content/staff-settings';
import { loadStaffSettings, type StaffSettingsView } from '@/features/staff/lib/load-staff-settings';
import { parseStaffFilters } from '@/features/staff/lib/staff-filters';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: staffSettings.metaTitle };

type SearchParams = Promise<Record<string, string | string[] | undefined>>;

/**
 * Who works here and what each of them may do: the staff list with its invitations, and the roles
 * that bundle scopes. Reading needs `staff:read`; every button that changes something is drawn
 * only for a session holding the matching manage scope, and the backend checks again regardless.
 */
export default async function StaffSettingsPage({ searchParams }: { searchParams: SearchParams }) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.StaffRead)) {
    return <Alert tone="info">{staffSettings.noAccess}</Alert>;
  }

  const filters = parseStaffFilters(params);
  let view: StaffSettingsView;

  try {
    view = await loadStaffSettings(context, filters);
  } catch (error) {
    // A filter the backend refuses (a branch outside this session's reach) is said, not crashed on.
    if (error instanceof ApiError && (error.status === 403 || error.status === 422)) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  const { directory } = view;

  return (
    <div className="space-y-6">
      <PageHeader title={staffSettings.title} subtitle={staffSettings.subtitle} />

      {directory.canManage &&
        (directory.roles ? <InviteStaffForm directory={directory} /> : <Alert tone="info">{staffSettings.invite.rolesUnavailable}</Alert>)}

      <div className="space-y-4">
        <StaffStatusTabs filters={filters} />
        <StaffFilterForm filters={filters} branches={directory.branches} departments={directory.departments} roles={directory.roles} />
      </div>

      <StaffListCard
        members={view.members}
        filters={filters}
        nextCursor={view.nextCursor}
        invitedFirst={view.invitedFirst}
        directory={directory}
      />

      {directory.roles ? (
        <RolesCard
          roles={directory.roles}
          permissions={view.permissions}
          callerScopes={directory.callerScopes}
          canManage={context.can(Scope.RoleManage)}
        />
      ) : (
        <Alert tone="info">{roleSettings.noAccess}</Alert>
      )}
    </div>
  );
}
