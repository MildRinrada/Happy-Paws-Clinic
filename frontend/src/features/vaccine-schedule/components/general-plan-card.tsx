import { vaccinePage } from '../content/vaccine-schedule-page';
import type { SpeciesPlan } from '../types/vaccine-schedule.types';

/** One species' general schedule as a short list: when, and what is given. */
export function GeneralPlanCard({ plan }: { plan: SpeciesPlan }) {
  const text = vaccinePage.intro;
  const visits = [...plan.visits, plan.repeat];
  const names = (ids: string[]) =>
    ids.map((id) => plan.vaccines.find((vaccine) => vaccine.id === id)?.name).filter(Boolean).join(' และ ');

  return (
    <section aria-labelledby={`general-${plan.species}`} className="rounded-2xl border border-site-border bg-white p-5 sm:p-6">
      <h3 id={`general-${plan.species}`} className="text-lg font-bold">
        {text.generalHeading.replace('{pet}', plan.label)}
      </h3>

      <dl className="mt-4 divide-y divide-ink-100 text-[15px]">
        {visits.map((visit) => (
          <div key={visit.id} className="grid gap-1 py-3 sm:grid-cols-[130px_1fr] sm:gap-4">
            <dt className="font-semibold text-site-teal">{visit.when}</dt>
            <dd>{names(visit.vaccineIds)}</dd>
          </div>
        ))}
      </dl>

      <h4 className="mt-5 text-sm font-bold">{text.optionalHeading}</h4>
      <ul className="mt-2 space-y-2 text-sm text-site-muted">
        {plan.optional.map((vaccine) => (
          <li key={vaccine.name}>
            <span className="font-semibold text-ink-900">{vaccine.name}</span> {vaccine.detail}
          </li>
        ))}
      </ul>
    </section>
  );
}
