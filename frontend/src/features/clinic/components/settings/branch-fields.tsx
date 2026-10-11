import { Field, Input } from '@/components/ui/field';
import { clinicSettings } from '../../content/clinic-settings';
import type { BranchDraft } from '../../lib/branch-draft';

interface BranchFieldsProps {
  /** Keeps the ids apart when two forms are on the page. */
  idPrefix: string;
  values: BranchDraft;
  errors: Partial<Record<string, string>>;
  onChange: (field: keyof BranchDraft, value: string) => void;
  coordinatesHint: string;
  roomsHint: string;
}

/** Name, address, pin and exam rooms: what opening a branch and editing one share. */
export function BranchFields({ idPrefix, values, errors, onChange, coordinatesHint, roomsHint }: BranchFieldsProps) {
  const text = clinicSettings.branches;
  const id = (field: string) => `${idPrefix}-${field}`;

  return (
    <>
      <Field label={text.name} htmlFor={id('name')} error={errors.name}>
        <Input id={id('name')} value={values.name} maxLength={200} required onChange={(event) => onChange('name', event.target.value)} />
      </Field>

      <Field label={text.address} htmlFor={id('address')} error={errors.address}>
        <Input id={id('address')} value={values.address} maxLength={500} onChange={(event) => onChange('address', event.target.value)} />
      </Field>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.latitude} htmlFor={id('lat')} hint={coordinatesHint} error={errors.latitude}>
          <Input id={id('lat')} inputMode="decimal" value={values.latitude} onChange={(event) => onChange('latitude', event.target.value)} />
        </Field>

        <Field label={text.longitude} htmlFor={id('lng')} error={errors.longitude}>
          <Input id={id('lng')} inputMode="decimal" value={values.longitude} onChange={(event) => onChange('longitude', event.target.value)} />
        </Field>
      </div>

      <Field label={text.examRooms} htmlFor={id('rooms')} hint={roomsHint} error={errors.exam_rooms}>
        <Input id={id('rooms')} value={values.rooms} onChange={(event) => onChange('rooms', event.target.value)} />
      </Field>
    </>
  );
}
