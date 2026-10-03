import { formatLongDate } from '@/lib/format/datetime';
import { portalRecords } from '../../content/portal-records';
import type { OwnerVisit } from '../../types/records.types';
import { RecordSection } from './record-section';

/** A line list, or "ไม่มี" when the section is open but this visit had nothing in it. */
function Lines({ items }: { items: string[] }) {
  if (items.length === 0) return <p className="text-site-muted">{portalRecords.noneInVisit}</p>;

  return (
    <ul className="space-y-0.5">
      {items.map((item, index) => (
        <li key={index}>{item}</li>
      ))}
    </ul>
  );
}

/**
 * One visit, with only the sections the clinic opened. A section the API left out is not drawn at
 * all; one it sent empty says "ไม่มี".
 */
export function VisitRecord({ visit }: { visit: OwnerVisit }) {
  const text = portalRecords;
  const v = text.vitals;

  return (
    <article className="space-y-3 rounded-2xl bg-white p-5 shadow-sm">
      <h2 className="text-base font-semibold text-ink-900">{text.visitedOn(formatLongDate(visit.visited_at))}</h2>

      {visit.vitals && (
        <RecordSection title={text.sections.vitals}>
          <Lines
            items={visit.vitals.map((row) =>
              [
                row.weight_kg != null && v.weight(row.weight_kg),
                row.temperature_c != null && v.temperature(row.temperature_c),
                row.heart_rate_bpm != null && v.heartRate(row.heart_rate_bpm),
                row.respiratory_rate_bpm != null && v.respiratoryRate(row.respiratory_rate_bpm),
              ]
                .filter(Boolean)
                .join('  '),
            )}
          />
        </RecordSection>
      )}

      {visit.examination && (
        <RecordSection title={text.sections.examination}>
          <Lines
            items={visit.examination.map(
              (finding) =>
                `${finding.body_system}: ${finding.is_normal ? text.normal : text.abnormal}${finding.note ? ` (${finding.note})` : ''}`,
            )}
          />
        </RecordSection>
      )}

      {visit.diagnoses && (
        <RecordSection title={text.sections.diagnosis}>
          <Lines items={visit.diagnoses} />
        </RecordSection>
      )}

      {visit.treatments && (
        <RecordSection title={text.sections.treatment}>
          <Lines items={visit.treatments.map((item) => `${item.name} ${text.quantity(item.quantity)}`)} />
        </RecordSection>
      )}

      {visit.prescriptions && (
        <RecordSection title={text.sections.prescription}>
          <Lines
            items={visit.prescriptions.map(
              (item) =>
                `${item.drug_name} ${text.prescription(item.dose, item.frequency, item.duration_days)}${
                  item.instructions ? ` (${item.instructions})` : ''
                }`,
            )}
          />
        </RecordSection>
      )}
    </article>
  );
}
