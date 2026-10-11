import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import type { VisibleNavGroup } from '@/features/staff-shell/types/staff-shell.types';
import { NavGroup } from './nav-group';

/** The grouped menu, the same in the sidebar and in the phone drawer. */
export function NavMenu({ groups, onNavigate }: { groups: VisibleNavGroup[]; onNavigate?: () => void }) {
  return (
    <nav aria-label={staffShellText.mainMenu} className="space-y-4">
      {groups.map((group) => (
        <NavGroup key={group.key} group={group} onNavigate={onNavigate} />
      ))}
    </nav>
  );
}
