import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import { StaffIcon } from './staff-icon';

/** The button at the foot of the sidebar that folds it down to icons and back. */
export function CollapseToggle({ collapsed, onToggle }: { collapsed: boolean; onToggle: () => void }) {
  const label = collapsed ? staffShellText.expand : staffShellText.collapse;

  return (
    <button
      type="button"
      onClick={onToggle}
      aria-expanded={!collapsed}
      aria-label={label}
      title={label}
      className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-ink-600 hover:bg-ink-100 hover:text-ink-900 group-data-[collapsed=true]/sidebar:justify-center group-data-[collapsed=true]/sidebar:px-0"
    >
      <StaffIcon name={collapsed ? 'expand' : 'collapse'} className="size-5 shrink-0" />
      <span aria-hidden className="group-data-[collapsed=true]/sidebar:hidden">
        {label}
      </span>
    </button>
  );
}
