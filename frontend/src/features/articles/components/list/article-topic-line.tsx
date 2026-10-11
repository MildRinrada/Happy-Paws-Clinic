/** The small green line of topics over a card's title, such as "แมว วัคซีน". */
export function ArticleTopicLine({ tags, className = '' }: { tags: string[]; className?: string }) {
  return <p className={`text-[12.5px] font-semibold text-brand-700 ${className}`}>{tags.join(' ')}</p>;
}
