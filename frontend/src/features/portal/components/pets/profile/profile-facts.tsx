import { portalPets } from '../../../content/portal-pets';

export interface ProfileFact {
  label: string;
  value?: string | null;
}

/**
 * The clinic's and the owner's facts about a pet. Two to a row with the label above on a phone; one
 * row each, label left and value right, from 1024px. Every fact keeps its place: one not filled in
 * says so quietly, so the grid never shifts. Long notes stop at four lines; the edit page has them whole.
 */
export function ProfileFacts({ facts }: { facts: ProfileFact[] }) {
  return (
    <dl className="grid grid-cols-2 lg:grid-cols-1 lg:px-5">
      {facts.map((fact) => (
        <div
          key={fact.label}
          className="min-w-0 border-b border-ink-100 px-4 py-3.5 even:border-l lg:flex lg:items-baseline lg:justify-between lg:gap-4 lg:px-0 lg:py-2.5 lg:even:border-l-0"
        >
          <dt className="text-[12.5px] leading-5 text-ink-600 lg:shrink-0 lg:text-[13px]">{fact.label}</dt>
          <dd
            className={`mt-1 line-clamp-4 break-words text-[15px] leading-[22px] lg:mt-0 lg:text-right lg:text-[13.5px] lg:leading-5 ${
              fact.value ? 'font-medium text-ink-900' : 'text-ink-400'
            }`}
          >
            {fact.value || portalPets.facts.missing}
          </dd>
        </div>
      ))}
    </dl>
  );
}
