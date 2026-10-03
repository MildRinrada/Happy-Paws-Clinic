import type { CarePromise } from '../../types/home.types';

/** One numbered promise: the number, what is promised, and what that means for the owner. */
export function CarePromiseCard({ number, promise }: { number: number; promise: CarePromise }) {
  return (
    <li className="grid grid-cols-[30px_1fr] rounded-[14px] bg-site-forest-raised px-6 py-3">
      <span aria-hidden="true" className="text-lg font-bold text-site-orange">
        {number}.
      </span>
      <h3 className="text-lg font-bold">{promise.title}</h3>
      <p className="col-start-2 text-[15px] font-light text-white/95">{promise.detail}</p>
    </li>
  );
}
