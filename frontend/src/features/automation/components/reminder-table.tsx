import { formatDateTime } from '@/lib/format/datetime';
import { subjectLabels } from '../content/automation-labels';
import { remindersPage } from '../content/reminders-page';
import type { Reminder } from '../types/automation.types';
import { ReminderStatusBadge } from './reminder-status-badge';

/**
 * Reminders as a table: when, to whom, about what, the words, and what the run will do. Used by
 * the preview and by the upcoming list, which come from the same evaluator on the backend.
 */
export function ReminderTable({ reminders, showRule = false }: { reminders: Reminder[]; showRule?: boolean }) {
  const text = remindersPage;

  return (
    <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
      <table className="w-full text-left text-sm">
        <thead className="bg-ink-50 text-xs text-ink-600">
          <tr>
            <th className="px-3 py-2 font-medium">{text.columns.sendAt}</th>
            <th className="px-3 py-2 font-medium">{text.columns.customer}</th>
            <th className="px-3 py-2 font-medium">{text.columns.subject}</th>
            <th className="px-3 py-2 font-medium">{text.columns.message}</th>
            <th className="px-3 py-2 font-medium">{text.columns.status}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {reminders.map((reminder) => (
            <tr key={`${reminder.automation_id}:${reminder.subject.id}:${reminder.send_at}`} className="align-top">
              <td className="whitespace-nowrap px-3 py-2">
                {formatDateTime(reminder.send_at)}
                {reminder.due_now && <span className="block text-xs text-amber-800">{text.dueNow}</span>}
              </td>
              <td className="px-3 py-2">
                {reminder.customer_name}
                <span className="block text-xs text-ink-500">{reminder.pet_name}</span>
              </td>
              <td className="px-3 py-2">
                {subjectLabels[reminder.subject.type] ?? reminder.subject.type}
                {reminder.subject.label && <span className="block text-xs text-ink-500">{reminder.subject.label}</span>}
                {showRule && <span className="block text-xs text-ink-500">{text.rule(reminder.automation_name)}</span>}
              </td>
              <td className="max-w-xs px-3 py-2 text-ink-700">{reminder.message.body}</td>
              <td className="px-3 py-2">
                <ReminderStatusBadge status={reminder.status} />
                {reminder.skip_reason && (
                  <span className="block text-xs text-ink-500">{text.skipReasons[reminder.skip_reason] ?? reminder.skip_reason}</span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
