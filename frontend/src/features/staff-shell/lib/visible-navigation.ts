import type { StaffContext } from '@/features/account/lib/staff-context';
import { settingsTabs } from '@/features/settings/content/settings-tabs';
import { staffNavigation } from '../content/staff-navigation';
import type { VisibleNavGroup } from '../types/staff-shell.types';

/**
 * Where a menu entry goes. Settings opens straight on the first tab this session can use, rather
 * than on `/settings`, which would only redirect there: one round trip instead of two.
 */
function entryOf(href: string, context: StaffContext): string {
  return href === '/settings' ? (settingsTabs.find((tab) => context.can(tab.scope))?.href ?? href) : href;
}

/**
 * The menu this session may see: only the screens its scopes can use, and only the groups left
 * with an entry in them. A cashier sees no queue, a vet assistant no cash desk.
 *
 * That is `GET /me` used for what the contract says it is for ("บอกว่า UI ต้องเปิดเมนูใดให้บัญชีนี้")
 * and it is a convenience only: typing the address of a hidden screen reaches a page whose data the
 * backend then refuses.
 */
export function visibleNavigation(context: StaffContext, aiEnabled: boolean): VisibleNavGroup[] {
  return staffNavigation.flatMap((group) => {
    const items = group.items
      .filter((item) => item.scopes.some(context.can) && (!item.ai || aiEnabled))
      .map((item) => ({
        href: entryOf(item.href, context),
        section: item.href,
        label: item.label,
        icon: item.icon,
        badge: item.badge,
      }));

    return items.length > 0 ? [{ key: group.key, label: group.label, items }] : [];
  });
}
