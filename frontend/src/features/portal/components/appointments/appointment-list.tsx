import type { AppointmentRowView } from '../../lib/load-appointments';
import { EmptyLine } from '../shared/empty-line';
import { SectionCard } from '../shared/section-card';
import { AppointmentRow } from './appointment-row';

interface AppointmentListProps {
  title: string;
  empty: string;
  rows: AppointmentRowView[];
}

/** A titled list of appointments, or one line saying there are none. */
export function AppointmentList({ title, empty, rows }: AppointmentListProps) {
  return (
    <SectionCard title={title}>
      {rows.length === 0 ? (
        <EmptyLine>{empty}</EmptyLine>
      ) : (
        <ul className="divide-y divide-ink-100">
          {rows.map((row) => (
            <AppointmentRow key={row.id} row={row} />
          ))}
        </ul>
      )}
    </SectionCard>
  );
}
