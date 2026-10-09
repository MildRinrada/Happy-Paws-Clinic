/** Where the "เชื่อมต่อระบบ" pages live. */
export const integrationPaths = {
  webhooks: '/settings/integrations/webhooks',
  webhookLog: (id: string) => `/settings/integrations/webhooks/${encodeURIComponent(id)}`,
  webhookLogPage: (id: string, cursor: string) => `/settings/integrations/webhooks/${encodeURIComponent(id)}?cursor=${encodeURIComponent(cursor)}`,
};

/** A webhook id as the backend makes them: `whk_` and the Crockford base32 body. */
export const isWebhookId = (value: string) => /^whk_[0-9A-Z]{10,30}$/.test(value.trim());
