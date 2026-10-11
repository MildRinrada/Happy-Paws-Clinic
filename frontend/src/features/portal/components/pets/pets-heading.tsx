import { portalPets } from '../../content/portal-pets';

/** "สัตว์เลี้ยงของฉัน" and the line under it, at the top of the pet pages, as the home page sets its greeting. */
export function PetsHeading() {
  return (
    <div className="min-w-0">
      <h1 className="font-display text-2xl font-semibold leading-[30px] text-ink-900">{portalPets.title}</h1>
      <p className="mt-0.5 text-sm leading-[22px] text-ink-600">{portalPets.intro}</p>
    </div>
  );
}
