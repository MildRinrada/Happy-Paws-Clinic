import { queueDisplay } from '../../content/queue-display';
import type { DisplayView } from '../../types/queue.types';

/** Earlier calls still waiting for their owner, shown smaller under the newest one. */
const EARLIER_SHOWN = 3;

function destination(call: DisplayView['calls'][number]): string {
  const where = call.room ? queueDisplay.goTo(call.room) : queueDisplay.goToCounter;

  return call.vetName ? `${where} · ${call.vetName}` : where;
}

/**
 * "กำลังเรียก": the newest call as large as the screen allows, the few before it beneath.
 * `aria-live` so a screen reader on the same machine announces each new call.
 */
export function DisplayCalls({ calls }: { calls: DisplayView['calls'] }) {
  const [latest, ...earlier] = calls;

  return (
    <section aria-labelledby="display-calling" className="flex min-h-0 flex-col gap-[2vh]">
      <h2 id="display-calling" className="font-display text-[clamp(1.5rem,4.5vh,4rem)] font-semibold text-brand-300">
        {queueDisplay.calling}
      </h2>

      <div aria-live="polite" className="flex flex-1 flex-col justify-center rounded-[2vh] bg-ink-800 p-[3vh]">
        {latest ? (
          <>
            <p className="font-display text-[clamp(4rem,24vh,22rem)] font-semibold leading-none tracking-tight text-white">
              {latest.number}
            </p>
            <p className="mt-[2vh] text-[clamp(1.5rem,5.5vh,5rem)] font-medium text-brand-200">{destination(latest)}</p>
          </>
        ) : (
          <p className="text-[clamp(1.5rem,5vh,4.5rem)] text-ink-400">{queueDisplay.callingEmpty}</p>
        )}
      </div>

      {earlier.length > 0 && (
        <ul className="grid grid-cols-3 gap-[2vh]">
          {earlier.slice(0, EARLIER_SHOWN).map((call) => (
            <li key={call.number} className="min-w-0 rounded-[1.5vh] bg-ink-800 px-[2vh] py-[1.5vh]">
              <p className="font-display text-[clamp(2rem,8vh,7rem)] font-semibold leading-none">{call.number}</p>
              <p className="truncate text-[clamp(1rem,2.8vh,2.5rem)] text-ink-300">{destination(call)}</p>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
