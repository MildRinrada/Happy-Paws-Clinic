'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import { updateAutomationAction } from '../actions/automation-actions';
import { automationsPage } from '../content/automations-page';
import type { Automation } from '../types/automation.types';
import { SwitchControl } from './switch-control';

/**
 * Switches a rule on or off. Off is one tap; on asks once, because from the next run the rule
 * messages real customers by itself. The list re-reads itself afterwards.
 */
export function AutomationToggle({ rule }: { rule: Pick<Automation, 'id' | 'version' | 'name' | 'enabled'> }) {
  const text = automationsPage.manage;
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function save(enabled: boolean) {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await updateAutomationAction({ id: rule.id, version: rule.version, values: { enabled } });
        if (!result.ok) setError(result.error.code === 'version_conflict' ? formMessages.conflict : result.error.message);
      } catch {
        setError(formMessages.unreachable);
      }
      setConfirming(false);
    });
  }

  return (
    <span className="flex flex-col items-end gap-1">
      <span className="flex items-center gap-2">
        {confirming && (
          <>
            <Button variant="ghost" size="sm" disabled={pending} onClick={() => setConfirming(false)}>
              {text.cancel}
            </Button>
            <Button size="sm" disabled={pending} onClick={() => save(true)}>
              {pending ? text.switching : text.confirmOnButton}
            </Button>
          </>
        )}
        <SwitchControl
          checked={rule.enabled}
          label={text.switchLabel(rule.name)}
          disabled={pending || confirming}
          onToggle={() => (rule.enabled ? save(false) : setConfirming(true))}
        />
      </span>
      {confirming && <span className="text-xs text-ink-500">{text.confirmOn}</span>}
      {error && (
        <span role="alert" className="text-xs text-red-700">
          {error}
        </span>
      )}
    </span>
  );
}
