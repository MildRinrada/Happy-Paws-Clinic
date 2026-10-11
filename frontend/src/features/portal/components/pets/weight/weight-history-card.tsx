import { petDetail } from '../../../content/pet-detail';
import type { WeightPoint } from '../../../lib/load-pet-weights';
import { SectionCard } from '../../shared/section-card';
import { WeightBars } from './weight-bars';

/**
 * "น้ำหนักย้อนหลัง", the bars with their values and months, from 1024px. Drawn only with two
 * weighings or more; one weight is already on the tile above, and a chart of one bar says nothing.
 */
export function WeightHistoryCard({ points }: { points: WeightPoint[] }) {
  if (points.length < 2) return null;

  const text = petDetail.weight;

  return (
    <div className="hidden lg:block">
      <SectionCard title={text.historyTitle} aside={text.count(points.length)}>
        <WeightBars points={points} variant="full" />
      </SectionCard>
    </div>
  );
}
