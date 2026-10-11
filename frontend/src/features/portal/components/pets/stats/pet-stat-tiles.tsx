import { formatTime } from '@/lib/format/datetime';
import { portalPaths } from '../../../content/portal-navigation';
import { petDetail } from '../../../content/pet-detail';
import type { PetVaccine } from '../../../lib/load-pet-vaccines';
import type { NextVisitView } from '../../../lib/load-pet-visits';
import { thaiDate } from '../../../lib/pet-dates';
import type { MyPet } from '../../../types/portal.types';
import { StatTile } from './stat-tile';

interface PetStatTilesProps {
  pet: MyPet;
  vaccines: PetVaccine[];
  nextVisit?: NextVisitView;
}

/**
 * The three tiles across the top of a pet's page, from 1024px: the latest weight, the most urgent
 * vaccine (red when overdue, yellow when due soon), and the next appointment. Booking links point at
 * the booking form with this pet chosen.
 */
export function PetStatTiles({ pet, vaccines, nextVisit }: PetStatTilesProps) {
  const text = petDetail.stats;
  const book = portalPaths.bookFor(pet.id);
  const [urgent, ...more] = vaccines.filter((item) => item.status !== 'given');
  const weight = pet.latest_weight;

  return (
    <div className="hidden grid-cols-3 gap-4 lg:grid">
      <StatTile
        label={text.weight}
        value={weight ? text.weightValue(weight.weight_kg) : text.noWeight}
        valueClassName={weight ? 'text-brand-900' : undefined}
        note={weight ? thaiDate(weight.recorded_at) : undefined}
      />

      {/* A pet that has died has nothing due and nothing to book: its weight stands alone. */}
      {pet.is_deceased ? null : urgent ? (
        <StatTile
          tone={urgent.status === 'overdue' ? 'alert' : 'warn'}
          label={urgent.status === 'overdue' ? text.vaccineOverdue : text.vaccineDueSoon}
          value={more.length > 0 ? text.moreVaccines(urgent.name, more.length) : urgent.name}
          link={{ label: text.bookVaccine, href: book }}
        />
      ) : (
        <StatTile label={text.vaccineClear} value={text.vaccineNoneDue} />
      )}

      {pet.is_deceased ? null : nextVisit ? (
        <StatTile
          label={text.nextVisit}
          value={thaiDate(nextVisit.startsAt)}
          note={text.nextVisitNote(formatTime(nextVisit.startsAt), nextVisit.serviceName)}
          link={{ label: text.seeAppointments, href: portalPaths.appointments }}
        />
      ) : (
        <StatTile
          label={text.nextVisit}
          value={text.noNextVisit}
          link={{ label: text.book, href: book }}
        />
      )}
    </div>
  );
}
