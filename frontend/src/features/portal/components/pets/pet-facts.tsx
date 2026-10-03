export interface PetFact {
  label: string;
  value?: string | null;
}

/** Label and value pairs. A fact with no value is left out rather than shown as a dash. */
export function PetFacts({ facts }: { facts: PetFact[] }) {
  const shown = facts.filter((fact) => fact.value);

  return (
    <dl className="grid gap-x-6 gap-y-2 sm:grid-cols-2">
      {shown.map((fact) => (
        <div key={fact.label}>
          <dt className="text-xs text-site-muted">{fact.label}</dt>
          <dd className="text-sm text-ink-900">{fact.value}</dd>
        </div>
      ))}
    </dl>
  );
}
