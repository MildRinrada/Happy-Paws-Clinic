import { schedulePage } from '../content/schedule-page';
import type { DayCover } from '../lib/roster-summary';

interface RosterSummaryRowProps {
  days: string[];
  today: string;
  cover: Record<string, DayCover>;
}

/** The last row: per day, how many vets and how many front-desk staff are on, red on a day with no vet. */
export function RosterSummaryRow({ days, today, cover }: RosterSummaryRowProps) {
  const text = schedulePage.cover;

  return (
    <tfoot className="border-t-2 border-ink-200 bg-ink-50/60 text-xs">
      <tr>
        <th scope="row" className="px-3 py-2 text-left font-semibold text-ink-700">
          {text.heading}
        </th>
        {days.map((day) => {
          const { vets, frontDesk } = cover[day]!;

          return (
            <td
              key={day}
              className={`border-l border-ink-100 px-2 py-2 align-top ${
                vets === 0 ? 'bg-red-50 text-red-800' : day === today ? 'bg-brand-50/50 text-ink-700' : 'text-ink-700'
              }`}
            >
              <span className={`block ${vets === 0 ? 'font-semibold' : ''}`}>{vets === 0 ? text.noVet : text.vets(vets)}</span>
              <span className="block">{text.frontDesk(frontDesk)}</span>
            </td>
          );
        })}
        <td className="border-l border-ink-100" />
      </tr>
    </tfoot>
  );
}
