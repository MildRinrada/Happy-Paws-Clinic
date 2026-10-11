import { portalHome } from '../../content/portal-home';
import { QuickActionTile } from './quick-action-tile';

/** The three shortcuts under the visit card: booking, bills and packages. */
export function QuickActions() {
  const { label, items } = portalHome.quickActions;

  return (
    <nav aria-label={label}>
      <ul className="grid grid-cols-3 gap-2.5">
        {items.map((item) => (
          <li key={item.href}>
            <QuickActionTile label={item.label} href={item.href} icon={item.icon} />
          </li>
        ))}
      </ul>
    </nav>
  );
}
