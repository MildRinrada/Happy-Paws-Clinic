import { portalRecords } from '../../content/portal-records';
import type { OwnerRecords } from '../../types/records.types';
import { EmptyLine } from '../shared/empty-line';
import { AttachmentList } from './attachment-list';
import { LabResults } from './lab-results';
import { VisitRecord } from './visit-record';

/** Everything the clinic shows of one pet's history: the visits, then lab results, then files. */
export function PetRecords({ records }: { records: OwnerRecords }) {
  const text = portalRecords;

  return (
    <div className="space-y-6">
      {/* The clinic's default is to open nothing, which leaves an owner with dates only. Say so. */}
      {records.visible_sections.length === 0 && (
        <p className="rounded-xl bg-white px-4 py-3 text-sm text-ink-700 shadow-sm">{text.onlyDates}</p>
      )}

      {records.visits.length === 0 ? (
        <EmptyLine>{text.noVisits}</EmptyLine>
      ) : (
        records.visits.map((visit) => <VisitRecord key={visit.record_id} visit={visit} />)
      )}

      {records.lab_results && <LabResults results={records.lab_results} />}
      {records.attachments && <AttachmentList items={records.attachments} />}
    </div>
  );
}
