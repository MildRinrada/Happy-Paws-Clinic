'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { automationsPage } from '../content/automations-page';
import type { Automation } from '../types/automation.types';
import { DeleteAutomationButton } from './delete-automation-button';
import { EditAutomationForm } from './edit-automation-form';

/** The edit and delete buttons under a rule, and the edit form in their place while it is open. */
export function AutomationEditor({ rule }: { rule: Automation }) {
  const [editing, setEditing] = useState(false);

  if (editing) {
    return <EditAutomationForm rule={rule} onDone={() => setEditing(false)} onCancel={() => setEditing(false)} />;
  }

  return (
    <div className="flex flex-wrap items-start gap-2">
      <Button variant="secondary" size="sm" onClick={() => setEditing(true)}>
        {automationsPage.manage.edit}
      </Button>
      <DeleteAutomationButton id={rule.id} version={rule.version} />
    </div>
  );
}
