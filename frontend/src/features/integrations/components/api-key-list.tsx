import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { apiKeyListText } from '../content/api-key-list';
import type { ApiKeySummary } from '../types/integration.types';
import { ApiKeyRow } from './api-key-row';

interface ApiKeyListProps {
  apiKeys: ApiKeySummary[];
  branchNames: Record<string, string>;
}

/** The keys this session could have made, newest first. Never the key itself. */
export function ApiKeyList({ apiKeys, branchNames }: ApiKeyListProps) {
  const text = apiKeyListText;
  const columns = text.columns;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-3">
        <p className="text-sm text-ink-600">{text.hint}</p>
        {apiKeys.length === 0 ? (
          <EmptyState title={text.empty} />
        ) : (
          <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
            <table className="w-full min-w-[820px] text-sm">
              <thead className="border-b border-ink-100 text-left text-ink-500">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.name}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.scopes}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.branches}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.status}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.dates}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.action}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-ink-100">
                {apiKeys.map((apiKey) => (
                  <ApiKeyRow key={apiKey.id} apiKey={apiKey} branchNames={branchNames} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardBody>
    </Card>
  );
}
