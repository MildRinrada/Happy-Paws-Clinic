import { deliveryErrorLabels, deliveryLogPage } from '../content/webhook-settings';
import type { WebhookDelivery } from '../types/integration.types';

/**
 * The latest attempt: status code, what went wrong, and the start of what the receiver said. The
 * receiver's answer is shown as plain text, never as markup; it comes from outside the clinic.
 */
export function WebhookDeliveryResult({ delivery }: { delivery: WebhookDelivery }) {
  if (delivery.attempts === 0) {
    return <span className="text-ink-400">-</span>;
  }

  const error = delivery.last_error ? (deliveryErrorLabels[delivery.last_error] ?? delivery.last_error) : undefined;

  return (
    <div className="space-y-1">
      <p className="tabular-nums text-ink-800">
        {delivery.last_status_code ?? '-'}
        {delivery.last_duration_ms !== undefined && <span className="ml-2 text-xs text-ink-500">{delivery.last_duration_ms} ms</span>}
      </p>
      {error && <p className="text-xs text-red-700">{error}</p>}
      {delivery.last_response_body && (
        <details className="text-xs text-ink-600">
          <summary className="cursor-pointer">{deliveryLogPage.answer}</summary>
          <pre className="mt-1 max-h-40 max-w-md overflow-auto whitespace-pre-wrap break-all rounded bg-ink-50 p-2 font-mono">
            {delivery.last_response_body}
          </pre>
        </details>
      )}
    </div>
  );
}
