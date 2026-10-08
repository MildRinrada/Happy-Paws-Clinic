import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { dashboardPage } from '../content/dashboard-page';
import { reportHref, type ReportRange, type ReportTab } from '../lib/report-range';

/** The report tabs the session may open. A tab whose scope the session lacks is not drawn. */
export function ReportTabs({ range, tabs }: { range: ReportRange; tabs: ReportTab[] }) {
  if (tabs.length < 2) {
    return null;
  }

  return (
    <nav aria-label={dashboardPage.tabs.label} className="flex flex-wrap gap-2">
      {tabs.map((tab) => (
        <Link
          key={tab}
          href={reportHref(range, { tab })}
          aria-current={tab === range.tab ? 'page' : undefined}
          className={buttonClass(tab === range.tab ? 'primary' : 'secondary', 'sm')}
        >
          {dashboardPage.tabs[tab]}
        </Link>
      ))}
    </nav>
  );
}
