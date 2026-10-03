import type { Stat, StatTone } from '../../types/home.types';

const tones: Record<StatTone, string> = {
  green: 'bg-stat-green',
  orange: 'bg-stat-orange',
  purple: 'bg-stat-purple',
  gray: 'bg-stat-gray',
};

/** One figure and what it counts, on a block of its own colour. */
export function StatCard({ stat }: { stat: Stat }) {
  return (
    // Less padding on the right than the left: the longest label runs almost to the card's edge.
    <li className={`rounded-[20px] py-4 pl-7 pr-3 text-white ${tones[stat.tone]}`}>
      <p className="text-3xl font-semibold leading-tight">{stat.value}</p>
      <p className="mt-0.5 text-[15px]">{stat.label}</p>
    </li>
  );
}
