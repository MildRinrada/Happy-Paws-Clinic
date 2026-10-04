import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { formatDateTime } from '@/lib/format/datetime';
import { treatmentPlan } from '../../content/treatment-plan';
import type { TreatmentPlan } from '../../types/pet.types';

/**
 * The plan just made, session by session. There is no way to read a plan back later, so this is
 * the one place its dates are shown; the booked ones are also on the appointment calendar.
 */
export function TreatmentPlanResult({ plan, onAnother }: { plan: TreatmentPlan; onAnother: () => void }) {
  const text = treatmentPlan;

  return (
    <div role="status" className="space-y-3 rounded-lg border border-emerald-200 bg-emerald-50/60 p-4">
      <p className="text-sm font-semibold text-emerald-900">{text.created(plan.title, plan.sessions.length)}</p>
      <ul className="divide-y divide-emerald-100 text-sm">
        {plan.sessions.map((session) => (
          <li key={session.number} className="flex flex-wrap items-center justify-between gap-2 py-1.5">
            <span className="text-ink-900">
              {text.session(session.number)} <span className="text-ink-600">{formatDateTime(session.planned_at)}</span>
            </span>
            <Badge tone={session.appointment_id ? 'green' : 'neutral'}>{session.appointment_id ? text.booked : text.planned}</Badge>
          </li>
        ))}
      </ul>
      <Button variant="secondary" size="sm" onClick={onAnother}>
        {text.another}
      </Button>
    </div>
  );
}
