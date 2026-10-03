/** One year on the clinic's timeline. The dot sits on the line drawn by the list. */
export function MilestoneItem({ year, text }: { year: string; text: string }) {
  return (
    <li className="relative pl-7">
      <span aria-hidden="true" className="absolute left-0 top-1.5 size-3 rounded-full border-2 border-site-teal bg-white" />
      <p className="font-semibold text-site-teal">{year}</p>
      <p className="text-[15px] text-site-muted">{text}</p>
    </li>
  );
}
