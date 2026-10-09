import { queueDisplay } from '../../content/queue-display';

/** As many as stay readable from the back of the room. */
const SHOWN = 6;

/** "คิวถัดไป": the next numbers in the order they will be called, and how many wait in all. */
export function DisplayNext({ next, waitingCount }: { next: string[]; waitingCount: number }) {
  return (
    <section aria-labelledby="display-next" className="flex min-h-0 flex-col gap-[2vh]">
      <h2 id="display-next" className="font-display text-[clamp(1.5rem,4.5vh,4rem)] font-semibold text-brand-300">
        {queueDisplay.next}
      </h2>

      <div className="flex flex-1 flex-col rounded-[2vh] bg-ink-800 p-[3vh]">
        {next.length === 0 ? (
          <p className="text-[clamp(1.5rem,5vh,4.5rem)] text-ink-400">{queueDisplay.nextEmpty}</p>
        ) : (
          <ol className="grid grid-cols-2 gap-x-[3vh] gap-y-[2vh]">
            {next.slice(0, SHOWN).map((number) => (
              <li key={number} className="font-display text-[clamp(2rem,9vh,8rem)] font-semibold leading-none">
                {number}
              </li>
            ))}
          </ol>
        )}

        <p className="mt-auto pt-[2vh] text-[clamp(1.25rem,4vh,3.5rem)] text-ink-300">
          {queueDisplay.waiting(waitingCount)}
        </p>
      </div>
    </section>
  );
}
