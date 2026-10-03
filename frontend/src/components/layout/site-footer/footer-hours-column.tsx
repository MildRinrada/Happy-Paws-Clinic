import { footerHours } from '@/config/site-footer';
import { hoursRows } from '@/features/clinic/lib/weekdays';
import type { OpeningHours } from '@/features/clinic/types/clinic.types';
import { FooterColumn, FooterColumnTitle } from './footer-column';

/**
 * The third column: the week's opening hours as a small table, days with the same hours folded
 * into one row and a closed day picked out. Read from the clinic's own settings.
 */
export function FooterHoursColumn({ hours }: { hours: OpeningHours | null }) {
  return (
    <FooterColumn heading={<FooterColumnTitle>{footerHours.title}</FooterColumnTitle>}>
      {hours ? (
        // The lines between rows are the footer showing through a 1px gap, not a border colour.
        <dl className="flex flex-col gap-px overflow-hidden rounded-[10px] text-sm whitespace-nowrap">
          {hoursRows(hours.days, footerHours.closed).map((row) => (
            <div key={row.days} className="flex min-h-[37px] items-center justify-between gap-3 bg-site-footer-raised pl-4 pr-3">
              <dt>{row.days}</dt>
              <dd className={row.closed ? 'text-[#ff7878]' : ''}>{row.hours}</dd>
            </div>
          ))}
        </dl>
      ) : (
        <p className="text-sm">{footerHours.unavailable}</p>
      )}
    </FooterColumn>
  );
}
