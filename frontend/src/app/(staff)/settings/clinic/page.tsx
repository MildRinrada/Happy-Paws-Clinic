import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getClinicProfile } from '@/features/clinic/api/getClinicProfile';
import { listBranches } from '@/features/clinic/api/listBranches';
import { BranchesCard } from '@/features/clinic/components/settings/branches-card';
import { ProfileForm } from '@/features/clinic/components/settings/profile-form';
import { clinicSettings } from '@/features/clinic/content/clinic-settings';
import { formMessages } from '@/features/settings/content/form-messages';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: clinicSettings.metaTitle };

/** The clinic's public profile and its branches. What is saved shows on the website on the next request. */
export default async function ClinicSettingsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.SettingsWrite)) {
    return <Alert tone="info">{formMessages.noAccess}</Alert>;
  }

  const [profile, branches] = await Promise.all([getClinicProfile(), listBranches()]);

  return (
    <div className="space-y-6">
      <PageHeader title={clinicSettings.title} subtitle={clinicSettings.subtitle} />
      <ProfileForm profile={profile} />
      <BranchesCard branches={branches} />
    </div>
  );
}
