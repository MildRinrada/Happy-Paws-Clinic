import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listAutomations } from '@/features/automation/api/listAutomations';
import { AutomationCard } from '@/features/automation/components/automation-card';
import { CreateAutomationForm } from '@/features/automation/components/create-automation-form';
import { automationsPage } from '@/features/automation/content/automations-page';
import { authed } from '@/lib/auth/session';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: automationsPage.metaTitle };

/**
 * The reminder rules with their history (`automation:read`); creating one, previewing who it would
 * reach, and switching, editing or deleting one need `automation:manage` (the last three are
 * docs/contract-extensions.md § 19). A rule made here belongs to the branch in view; a clinic-wide
 * rule is changed only by a session no branch limits, as the backend requires.
 */
export default async function AutomationsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.AutomationRead)) {
    return <Alert tone="info">{automationsPage.noAccess}</Alert>;
  }

  const canManage = context.can(Scope.AutomationManage);
  const rules = await authed(listAutomations);
  const branchNames = new Map(context.branches.map((branch) => [branch.id, branch.name]));

  return (
    <div className="space-y-5">
      <PageHeader title={automationsPage.title} subtitle={automationsPage.subtitle} />
      <Alert tone="info">{automationsPage.startHint}</Alert>

      {canManage && <CreateAutomationForm branch={context.branch ? { id: context.branch.id, name: context.branch.name } : undefined} />}

      {rules.length === 0 ? (
        <EmptyState title={automationsPage.empty} />
      ) : (
        <div className="space-y-4">
          {rules.map((rule) => (
            <AutomationCard
              key={rule.id}
              rule={rule}
              branchName={rule.condition.branch_id ? (branchNames.get(rule.condition.branch_id) ?? rule.condition.branch_id) : undefined}
              canSimulate={canManage}
              canChange={canManage && (Boolean(rule.condition.branch_id) || !context.branchBound)}
              locked={canManage && !rule.condition.branch_id && context.branchBound}
            />
          ))}
        </div>
      )}
    </div>
  );
}
