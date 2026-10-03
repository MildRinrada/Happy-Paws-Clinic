/** Where the customer screens live in the console. */
export const customerPaths = {
  list: '/customers',
  register: '/customers/new',
  detail: (id: string) => `/customers/${encodeURIComponent(id)}`,
  search: (q: string) => `/customers?q=${encodeURIComponent(q)}`,
};
