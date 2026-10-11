import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { quickActionText } from '../../content/quick-actions';
import { quickActions } from '../../lib/quick-actions';

/**
 * The counter's shortcuts beside the title, each to the page that already does the job. A shortcut
 * shows only when the session holds every scope that page needs, so none of them leads to a refusal.
 */
export function QuickActions({ can }: { can: (scope: string) => boolean }) {
  const actions = quickActions.filter((action) => action.scopes.every(can));

  if (actions.length === 0) {
    return null;
  }

  return (
    <nav aria-label={quickActionText.label} className="flex flex-wrap gap-2">
      {actions.map((action) => (
        <Link key={action.key} href={action.href} className={buttonClass('secondary', 'sm')}>
          {quickActionText[action.key]}
        </Link>
      ))}
    </nav>
  );
}
