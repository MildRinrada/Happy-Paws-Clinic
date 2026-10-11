import { useId, type ReactNode } from 'react';

/** The white card a booking step sits in, with the step's heading and an optional line under it. */
export function StepCard({ title, lead, children }: { title: string; lead?: string; children: ReactNode }) {
  const headingId = useId();

  return (
    <section aria-labelledby={headingId} className="rounded-[18px] bg-white p-5 sm:p-7">
      <h2 id={headingId} className="text-xl font-bold text-site-forest sm:text-[21px]">
        {title}
      </h2>
      {lead && <p className="mt-1.5 text-sm leading-relaxed text-site-muted">{lead}</p>}
      <div className="mt-6">{children}</div>
    </section>
  );
}
