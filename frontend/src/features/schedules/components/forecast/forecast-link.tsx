import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { demandForecastPage } from '../../content/forecast-page';

/** From the roster to the hour-by-hour forecast. Drawn only for a session holding `stats:read`. */
export function ForecastLink() {
  return (
    <Link href="/schedules/forecast" className={buttonClass('secondary', 'sm')}>
      {demandForecastPage.link}
    </Link>
  );
}
