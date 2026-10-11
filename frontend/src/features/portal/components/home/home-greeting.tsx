import { portalHome } from '../../content/portal-home';
import { firstNameOf } from '../../lib/first-name';

/** "สวัสดี คุณสมใจ" at the top of the portal's home, by first name, and the line under it. */
export function HomeGreeting({ name }: { name: string }) {
  return (
    <div>
      <h1 className="font-display text-2xl font-semibold leading-[30px] text-ink-900">{portalHome.greeting(firstNameOf(name))}</h1>
      <p className="mt-0.5 text-sm leading-[22px] text-ink-600">{portalHome.intro}</p>
    </div>
  );
}
