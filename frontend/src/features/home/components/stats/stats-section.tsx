import { stats } from '../../content/stats';
import { SectionContainer } from '../shared/section-container';
import { StatCard } from './stat-card';

/** The row of figures under the hero. */
export function StatsSection() {
  return (
    <section aria-label="คลินิกในตัวเลข" className="pt-7">
      <SectionContainer>
        <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4 lg:gap-[18px]">
          {stats.map((stat) => (
            <StatCard key={stat.value} stat={stat} />
          ))}
        </ul>
      </SectionContainer>
    </section>
  );
}
