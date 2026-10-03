/** The vet's title in a small green tag, e.g. "สัตวแพทย์ประจำ". Long titles are cut with an ellipsis. */
export function JobTitleBadge({ title }: { title: string }) {
  return (
    <span className="flex h-7 min-w-0 items-center rounded-md bg-[#d6f7e4] px-2.5 text-[13px] text-[#25ad6b]">
      <span className="truncate">{title}</span>
    </span>
  );
}
