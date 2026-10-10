'use client';

import { formatBaht } from '@/lib/format/money';
import { bookingPage } from '../content/booking-page';
import type { BookingDetails, BookingOptions } from '../types/booking.types';
import { BookingFieldset } from './booking-fieldset';
import { ChoiceCard } from './choice-card';

interface DetailsStepProps {
  options: BookingOptions;
  value: BookingDetails;
  onChange: (value: BookingDetails) => void;
  onNext: () => void;
  /**
   * The public booking page (`/book`) leaves these out: a visitor's pets are only known once their
   * number is proved, and the types a clinic offers online are read with a customer's session.
   */
  askPet?: boolean;
  askType?: boolean;
  /** The button's words, when the next step is not simply choosing a time. */
  nextLabel?: string;
}

/** Step 1: which pet, where, what service and what kind of visit, and anything for the vet. */
export function DetailsStep({ options, value, onChange, onNext, askPet = true, askType = true, nextLabel }: DetailsStepProps) {
  const text = bookingPage.details;
  const services = options.servicesByBranch[value.branchId] ?? [];
  const set = (patch: Partial<BookingDetails>) => onChange({ ...value, ...patch });
  const ready = Boolean(
    (!askPet || value.petId) && value.branchId && value.serviceId && (!askType || options.types.some((type) => type.code === value.type)),
  );

  return (
    <div className="space-y-6">
      {askPet && (
        <BookingFieldset legend={text.pet}>
          <div className="grid gap-2 sm:grid-cols-2">
            {options.pets.map((pet) => (
              <ChoiceCard
                key={pet.id}
                name="pet"
                value={pet.id}
                checked={value.petId === pet.id}
                onChange={(petId) => set({ petId })}
                title={pet.name}
                detail={pet.species}
              />
            ))}
          </div>
        </BookingFieldset>
      )}

      {options.branches.length > 1 && (
        <BookingFieldset legend={text.branch}>
          <div className="grid gap-2 sm:grid-cols-2">
            {options.branches.map((branch) => (
              <ChoiceCard
                key={branch.id}
                name="branch"
                value={branch.id}
                checked={value.branchId === branch.id}
                onChange={(branchId) => set({ branchId, serviceId: '' })}
                title={branch.name}
              />
            ))}
          </div>
        </BookingFieldset>
      )}

      <BookingFieldset legend={text.service}>
        {services.length === 0 ? (
          <p className="text-sm text-site-muted">{text.noServices}</p>
        ) : (
          <div className="grid gap-2 sm:grid-cols-2">
            {services.map((service) => (
              <ChoiceCard
                key={service.id}
                name="service"
                value={service.id}
                checked={value.serviceId === service.id}
                onChange={(serviceId) => set({ serviceId })}
                title={service.name}
                detail={`${text.minutes(service.duration_minutes)}  ${text.priceFrom(formatBaht(service.price_satang))}`}
              />
            ))}
          </div>
        )}
      </BookingFieldset>

      {askType && (
        <BookingFieldset legend={text.type}>
          {options.types.length === 0 ? (
            <p className="text-sm text-site-muted">{text.noTypes}</p>
          ) : (
            <div className="grid gap-2 sm:grid-cols-2">
              {options.types.map((type) => (
                <ChoiceCard
                  key={type.code}
                  name="type"
                  value={type.code}
                  checked={value.type === type.code}
                  onChange={() => set({ type: type.code })}
                  title={type.name}
                  detail={text.typeHints[type.code] ?? (type.duration_minutes ? text.typeMinutes(type.duration_minutes) : undefined)}
                />
              ))}
            </div>
          )}
        </BookingFieldset>
      )}

      <div className="space-y-2">
        <label htmlFor="booking-reason" className="block text-sm font-semibold text-ink-900">
          {text.reason}
        </label>
        <textarea
          id="booking-reason"
          value={value.reason}
          onChange={(event) => set({ reason: event.target.value })}
          maxLength={1000}
          rows={3}
          placeholder={text.reasonPlaceholder}
          className="w-full rounded-xl border border-ink-200 bg-white px-3.5 py-2.5 text-sm text-ink-900 placeholder:text-ink-400 focus:border-site-teal"
        />
      </div>

      <button
        type="button"
        onClick={onNext}
        disabled={!ready}
        className="h-12 w-full rounded-xl bg-site-orange text-sm font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {nextLabel ?? text.next}
      </button>
    </div>
  );
}
