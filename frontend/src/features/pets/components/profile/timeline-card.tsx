import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { petProfile } from '../../content/pet-profile';
import { petPaths } from '../../lib/pet-paths';
import type { PetProfile } from '../../lib/load-pet-profile';
import { TimelineEntry } from './timeline-entry';

interface TimelineCardProps {
  petId: string;
  history: PetProfile['history'];
  /** The page is showing an older page of the timeline. */
  paged: boolean;
}

/** Visits, medicines, vaccines and lab work, newest first, one page at a time. */
export function TimelineCard({ petId, history, paged }: TimelineCardProps) {
  const text = petProfile.timeline;

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody className="space-y-4">
        {history === null ? (
          <p className="text-sm text-ink-500">{text.noAccess}</p>
        ) : history.entries.length === 0 ? (
          <p className="text-sm text-ink-500">{text.none}</p>
        ) : (
          <ol className="relative space-y-4 before:absolute before:bottom-1 before:left-[5px] before:top-1 before:w-0.5 before:bg-ink-100">
            {history.entries.map((entry) => (
              <TimelineEntry key={`${entry.kind}:${entry.reference_id}`} entry={entry} />
            ))}
          </ol>
        )}

        <div className="flex gap-2">
          {paged && (
            <Link href={petPaths.detail(petId)} className={buttonClass('ghost', 'sm')}>
              {text.newest}
            </Link>
          )}
          {history?.nextCursor && (
            <Link href={`${petPaths.detail(petId)}?cursor=${encodeURIComponent(history.nextCursor)}`} className={buttonClass('secondary', 'sm')}>
              {text.more}
            </Link>
          )}
        </div>
      </CardBody>
    </Card>
  );
}
