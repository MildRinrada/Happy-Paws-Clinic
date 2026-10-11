import { petDetail } from '../../../content/pet-detail';
import type { WeightPoint } from '../../../lib/load-pet-weights';
import { thaiDate } from '../../../lib/pet-dates';
import type { MyPet } from '../../../types/portal.types';
import { EmptyLine } from '../../shared/empty-line';
import { SectionCard } from '../../shared/section-card';
import { WeightBars } from './weight-bars';

interface LatestWeightCardProps {
  weight: MyPet['latest_weight'];
  points: WeightPoint[];
}

/**
 * "น้ำหนักล่าสุด" on a phone: the number large, the day it was weighed, and small bars of the
 * recent weighings when there are at least two. Below 1024px only; wider screens have the tile and the chart.
 */
export function LatestWeightCard({ weight, points }: LatestWeightCardProps) {
  const text = petDetail.weight;

  return (
    <div className="lg:hidden">
      <SectionCard snug title={text.latestTitle} aside={weight ? thaiDate(weight.recorded_at) : undefined}>
        {weight ? (
          <>
            <p className="-mt-2 flex items-baseline gap-2.5 text-brand-900">
              <span className="font-display text-[32px] font-semibold leading-10">{weight.weight_kg}</span>
              <span className="font-display text-lg">{text.unit}</span>
            </p>
            {points.length >= 2 && (
              <div className="mt-4">
                <WeightBars points={points} variant="spark" />
              </div>
            )}
          </>
        ) : (
          <EmptyLine>{text.noWeight}</EmptyLine>
        )}
      </SectionCard>
    </div>
  );
}
