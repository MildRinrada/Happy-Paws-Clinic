import { formatLongDate } from '@/lib/format/datetime';
import { portalRecords } from '../../content/portal-records';
import type { OwnerAttachment } from '../../types/records.types';
import { EmptyLine } from '../shared/empty-line';
import { SectionCard } from '../shared/section-card';

/**
 * Files the clinic chose to show, and the files the owner sent. Each link is signed and lasts ten
 * minutes, which is why the page is read fresh every time and the links are not cached.
 */
export function AttachmentList({ items }: { items: OwnerAttachment[] }) {
  const text = portalRecords;

  return (
    <SectionCard title={text.sections.attachments}>
      {items.length === 0 ? (
        <EmptyLine>{text.noneInVisit}</EmptyLine>
      ) : (
        <ul className="divide-y divide-ink-100">
          {items.map((item) => (
            <li key={item.document_id} className="flex items-center justify-between gap-3 py-2.5">
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-ink-900">{item.title}</p>
                <p className="text-xs text-site-muted">
                  {formatLongDate(item.added_at)}
                  {item.source === 'owner' && ` (${text.attachment.fromOwner})`}
                </p>
              </div>
              {item.download_url && (
                <a
                  href={item.download_url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="shrink-0 text-sm font-semibold text-site-teal hover:underline"
                >
                  {text.attachment.open}
                </a>
              )}
            </li>
          ))}
        </ul>
      )}
    </SectionCard>
  );
}
