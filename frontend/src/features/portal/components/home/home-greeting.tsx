import { portalHome } from '../../content/portal-home';

/** "สวัสดี คุณ..." at the top of the portal's home. */
export function HomeGreeting({ name }: { name: string }) {
  return (
    <div>
      <h1 className="text-2xl font-semibold text-ink-900">{portalHome.greeting(name)}</h1>
      <p className="mt-1 text-sm text-site-muted">{portalHome.intro}</p>
    </div>
  );
}
