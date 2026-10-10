import { buttonClass } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { articleStatusLabels, staffArticlesText } from '../../content/staff-articles';
import type { ArticleStatus } from '../../types/staff-article.types';

/** Search and status for the article list, a plain GET form on the page's own address. */
export function ArticleFilters({ q, status }: { q?: string; status?: ArticleStatus }) {
  const text = staffArticlesText.filters;

  return (
    <form method="get" role="search" aria-label={text.label} className="flex flex-wrap items-end gap-3">
      <label className="block w-64 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.search}</span>
        <Input name="q" type="search" defaultValue={q} maxLength={120} />
      </label>
      <label className="block w-40 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.status}</span>
        <Select name="status" defaultValue={status ?? ''}>
          <option value="">{text.any}</option>
          {(Object.keys(articleStatusLabels) as ArticleStatus[]).map((value) => (
            <option key={value} value={value}>
              {articleStatusLabels[value].text}
            </option>
          ))}
        </Select>
      </label>
      <button type="submit" className={buttonClass('secondary', 'md')}>
        {text.apply}
      </button>
    </form>
  );
}
