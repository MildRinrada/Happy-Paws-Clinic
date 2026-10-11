import Link from 'next/link';
import { reportsPage } from '../content/reports-page';
import { reportHref, type ReportRange, type ReportTab } from '../lib/report-range';

/**
 * The report tabs the session may open, drawn as tabs on a rule rather than as buttons: they switch
 * what is shown, they do not do anything. A tab whose scope the session lacks is not drawn.
 */
export function ReportTabs({ range, tabs }: { range: ReportRange; tabs: ReportTab[] }) {
  if (tabs.length < 2) {
    return null;
  }

  return (
    <nav aria-label={reportsPage.tabs.label} className="flex gap-1 border-b border-ink-200">
      {tabs.map((tab) => {
        const current = tab === range.tab;

        return (
          <Link
            key={tab}
            href={reportHref(range, { tab })}
            aria-current={current ? 'page' : undefined}
            className={`-mb-px border-b-2 px-4 py-2 text-sm font-medium transition-colors ${
              current ? 'border-brand-600 text-brand-800' : 'border-transparent text-ink-600 hover:border-ink-300 hover:text-ink-900'
            }`}
          >
            {reportsPage.tabs[tab]}
          </Link>
        );
      })}
    </nav>
  );
}
