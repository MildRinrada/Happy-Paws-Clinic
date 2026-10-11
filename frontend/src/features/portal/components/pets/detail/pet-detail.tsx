import type { PetDetailView } from '../../../lib/load-pet-detail';
import { PetsHeading } from '../pets-heading';
import { PetProfilePanel } from '../profile/pet-profile-panel';
import { PetStatTiles } from '../stats/pet-stat-tiles';
import { PetSwitcher } from '../switcher/pet-switcher';
import { PetVaccinesCard } from '../vaccines/pet-vaccines-card';
import { RecentVisitsCard } from '../visits/recent-visits-card';
import { LatestWeightCard } from '../weight/latest-weight-card';
import { WeightHistoryCard } from '../weight/weight-history-card';
import { PetBackBar } from './pet-back-bar';

/**
 * One pet's page. On a phone, one column as the phone design draws it: the profile, the latest
 * weight, the vaccines and the visits, under a bar with the way back. From 1024px, as the desktop
 * design: the title and the pet chips across the top, the profile on the left, and on the right the
 * three tiles, the weight chart, and the vaccines beside the visits.
 */
export function PetDetail({ view }: { view: PetDetailView }) {
  const { pet } = view;

  return (
    <div>
      <PetBackBar href={view.backHref} />

      <div className="mb-5 hidden flex-wrap items-center justify-between gap-x-6 gap-y-3 lg:flex">
        <PetsHeading />
        {view.switcher && <PetSwitcher view={view.switcher} />}
      </div>

      <div className="grid items-start gap-4 lg:grid-cols-[264px_minmax(0,1fr)] lg:gap-[18px]">
        <PetProfilePanel pet={pet} tone={view.tone} description={view.description} />

        <div className="min-w-0 space-y-4">
          <PetStatTiles pet={pet} vaccines={view.vaccines} nextVisit={view.nextVisit} />
          <LatestWeightCard weight={pet.latest_weight} points={view.weights} />
          <WeightHistoryCard points={view.weights} />

          <div className="grid items-start gap-4 lg:grid-cols-2">
            <PetVaccinesCard pet={pet} vaccines={view.vaccines} complete={view.vaccinesComplete} />
            <RecentVisitsCard pet={pet} visits={view.recentVisits} />
          </div>
        </div>
      </div>
    </div>
  );
}
