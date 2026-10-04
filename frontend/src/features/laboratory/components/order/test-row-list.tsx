'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { labOrder } from '../../content/lab-order';
import { emptyTest, type TestDraft } from '../../lib/test-drafts';

const label = 'block text-xs font-medium text-ink-600';

/** The tests being asked for, one row each: a name, and the lab's code when it has one. Up to 30. */
export function TestRowList({ drafts, onChange }: { drafts: TestDraft[]; onChange: (drafts: TestDraft[]) => void }) {
  const text = labOrder;
  const update = (key: string, change: Partial<TestDraft>) =>
    onChange(drafts.map((row) => (row.key === key ? { ...row, ...change } : row)));

  return (
    <div className="space-y-2">
      {drafts.map((draft, index) => (
        <div key={draft.key} className="grid items-end gap-2 sm:grid-cols-[minmax(0,1fr)_8rem_auto]">
          <div className="space-y-1">
            <label htmlFor={`lab-test-${draft.key}`} className={label}>
              {text.test(index + 1)}
            </label>
            <Input
              id={`lab-test-${draft.key}`}
              value={draft.name}
              maxLength={200}
              placeholder={text.testPlaceholder}
              onChange={(event) => update(draft.key, { name: event.target.value })}
            />
          </div>
          <div className="space-y-1">
            <label htmlFor={`lab-code-${draft.key}`} className={label}>
              {text.testCode}
            </label>
            <Input id={`lab-code-${draft.key}`} value={draft.code} maxLength={50} onChange={(event) => update(draft.key, { code: event.target.value })} />
          </div>
          {drafts.length > 1 ? (
            <Button variant="ghost" size="sm" className="mb-1" onClick={() => onChange(drafts.filter((row) => row.key !== draft.key))}>
              {text.removeTest}
            </Button>
          ) : (
            <span />
          )}
        </div>
      ))}
      {drafts.length < 30 && (
        <Button variant="secondary" size="sm" onClick={() => onChange([...drafts, emptyTest()])}>
          {text.addTest}
        </Button>
      )}
    </div>
  );
}
