import Link from 'next/link';
import { Button, buttonClass } from '@/components/ui/button';
import { channelLabels, sourceLabels, statusLabels } from '../content/delivery-labels';
import { deliveryLogPage } from '../content/delivery-log';
import type { DeliveryLogQuery } from '../types/notification.types';
import { LogFilterSelect } from './log-filter-select';

const STATUSES = ['sent', 'failed', 'skipped'];
const CHANNELS = ['line', 'sms', 'email'];
const SOURCES = ['system', 'manual', 'broadcast', 'automation'];

/** A plain GET form: the filters live in the address, so a filtered view can be bookmarked or shared. */
export function DeliveryLogFilters({ query, basePath }: { query: DeliveryLogQuery; basePath: string }) {
  const text = deliveryLogPage.filters;
  const filtered = Boolean(query.status || query.channel || query.source || query.batch_id || query.customer_id);

  return (
    <form method="get" action={basePath} aria-label={text.label} className="flex flex-wrap items-end gap-3">
      <LogFilterSelect name="status" label={text.status} value={query.status} options={STATUSES} labels={statusLabels} />
      <LogFilterSelect name="channel" label={text.channel} value={query.channel} options={CHANNELS} labels={channelLabels} />
      <LogFilterSelect name="source" label={text.source} value={query.source} options={SOURCES} labels={sourceLabels} />
      {query.batch_id && <input type="hidden" name="batch_id" value={query.batch_id} />}
      {query.customer_id && <input type="hidden" name="customer_id" value={query.customer_id} />}

      <Button type="submit" variant="secondary">
        {text.apply}
      </Button>
      {filtered && (
        <Link href={basePath} className={buttonClass('ghost')}>
          {text.clear}
        </Link>
      )}
      {query.batch_id && <p className="w-full text-sm text-ink-600">{text.batch(query.batch_id)}</p>}
    </form>
  );
}
