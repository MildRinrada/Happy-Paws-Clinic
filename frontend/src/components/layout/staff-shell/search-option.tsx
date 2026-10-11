import type { ReactNode } from 'react';

interface SearchOptionProps {
  id: string;
  active: boolean;
  onPick: () => void;
  onHover: () => void;
  children: ReactNode;
}

/**
 * One line of the search results. Focus stays in the box while the arrows move the highlight
 * (`aria-activedescendant`), so a press here must not take focus away before the click lands.
 */
export function SearchOption({ id, active, onPick, onHover, children }: SearchOptionProps) {
  return (
    <li
      id={id}
      role="option"
      aria-selected={active}
      onMouseDown={(event) => event.preventDefault()}
      onMouseMove={onHover}
      onClick={onPick}
      className={`flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2 text-sm ${active ? 'bg-brand-50 text-brand-900' : 'text-ink-800'}`}
    >
      {children}
    </li>
  );
}
