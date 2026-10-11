import type { ReactNode } from 'react';
import { bookingFrame } from '../../content/booking-frame';
import { summaryDay, summaryWhen } from '../../lib/summary-when';
import { BookingSummaryCard } from './booking-summary-card';
import { SummaryCardRow } from './summary-card-row';
import { SummaryChangeButton } from './summary-change-button';

export interface BookingSummaryValues {
  /** Only on `/portal/book`, which asks which pet first. */
  pet?: string;
  service?: string;
  /** The kind of visit, on `/portal/book` only. */
  type?: string;
  /** The time picked or held. */
  startsAt?: string;
  /** The day chosen, while no time is picked yet. */
  day?: string;
  /** The vet's name. Left out for whoever is free. */
  vet?: string;
  branch?: string;
  reason?: string;
}

interface BookingSummaryProps {
  values: BookingSummaryValues;
  /** Whether the pet row shows at all: `/portal/book` asks for a pet, `/book` does not until the end. */
  askPet: boolean;
  /** Back to the details step, for "เปลี่ยน" beside the service. Left out once that can no longer change. */
  onChangeService?: () => void;
  /** The step's next button again, for a wide screen. */
  action?: ReactNode;
}

/** "สรุปการจอง" with the rows both booking pages share, filled in as the steps go. */
export function BookingSummary({ values, askPet, onChangeService, action }: BookingSummaryProps) {
  const text = bookingFrame.summary;
  const when = values.startsAt ? summaryWhen(values.startsAt) : values.day ? `${summaryDay(values.day)} ${text.noTime}` : undefined;

  return (
    <BookingSummaryCard action={action}>
      {askPet && <SummaryCardRow label={text.pet} value={values.pet} />}
      <SummaryCardRow
        label={text.service}
        value={values.service}
        action={onChangeService && <SummaryChangeButton onClick={onChangeService} />}
      />
      {values.type && <SummaryCardRow label={text.type} value={values.type} />}
      <SummaryCardRow label={text.when} value={when} highlight={Boolean(values.startsAt)} />
      <SummaryCardRow label={text.vet} value={values.vet ?? text.anyVet} />
      <SummaryCardRow label={text.branch} value={values.branch} />
      {values.reason && <SummaryCardRow label={text.reason} value={values.reason} />}
    </BookingSummaryCard>
  );
}
