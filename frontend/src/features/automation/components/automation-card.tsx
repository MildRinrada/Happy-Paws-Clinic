import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { automationChannelLabels, triggerLabels } from '../content/automation-labels';
import { automationsPage } from '../content/automations-page';
import { describeConditions, describeTiming, isStarterRule } from '../lib/describe-rule';
import type { Automation } from '../types/automation.types';
import { AutomationEditor } from './automation-editor';
import { AutomationStatsLine } from './automation-stats-line';
import { AutomationStatusBadge } from './automation-status-badge';
import { AutomationToggle } from './automation-toggle';
import { SimulatePanel } from './simulate-panel';

interface AutomationCardProps {
  rule: Automation;
  /** The branch's name, or undefined for a clinic-wide rule. */
  branchName?: string;
  canSimulate: boolean;
  /** May switch, edit and delete this rule: `automation:manage`, and a clinic-wide rule needs an unrestricted session. */
  canChange: boolean;
  /** Holds `automation:manage` but this clinic-wide rule is out of a branch-bound session's reach. */
  locked: boolean;
}

/** One rule: trigger, condition and action in words, its history, the switch, edit, delete and the preview. */
export function AutomationCard({ rule, branchName, canSimulate, canChange, locked }: AutomationCardProps) {
  const conditions = describeConditions(rule);

  return (
    <Card>
      <CardHeader
        title={rule.name}
        aside={
          <span className="flex flex-wrap items-center gap-2">
            {isStarterRule(rule) && <Badge tone="blue">{automationsPage.starter}</Badge>}
            <AutomationStatusBadge enabled={rule.enabled} />
            {canChange && <AutomationToggle rule={rule} />}
          </span>
        }
      />
      <CardBody className="space-y-3">
        <p className="text-sm text-ink-800">
          {triggerLabels[rule.trigger.type]} {describeTiming(rule)}
          <span className="block text-xs text-ink-500">
            {[branchName ?? automationsPage.clinicWide, ...conditions, automationChannelLabels[rule.action.channel]].join(' / ')}
          </span>
        </p>
        <AutomationStatsLine stats={rule.stats} />
        {canChange && <AutomationEditor key={rule.version} rule={rule} />}
        {locked && <p className="text-xs text-ink-500">{automationsPage.manage.clinicWideLocked}</p>}
        {canSimulate && <SimulatePanel automationId={rule.id} />}
      </CardBody>
    </Card>
  );
}
