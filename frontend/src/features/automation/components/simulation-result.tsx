import { EmptyState } from '@/components/ui/empty-state';
import { automationsPage } from '../content/automations-page';
import type { Simulation } from '../types/automation.types';
import { ReminderTable } from './reminder-table';

/** The preview's answer: counts first, then each reminder with its words. */
export function SimulationResult({ simulation }: { simulation: Simulation }) {
  const text = automationsPage.simulate;

  return (
    <div className="space-y-3">
      <p className="text-sm text-ink-700">
        {text.summary(simulation.would_send, simulation.would_skip, simulation.already_sent)}
        {simulation.due_now > 0 && <span className="block text-amber-800">{text.dueNow(simulation.due_now)}</span>}
      </p>
      {simulation.recipients.length === 0 ? <EmptyState title={text.empty} /> : <ReminderTable reminders={simulation.recipients} />}
      {simulation.truncated && <p className="text-xs text-ink-500">{text.truncated}</p>}
    </div>
  );
}
