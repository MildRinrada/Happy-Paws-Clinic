/** Where the staff article screens live, under the settings area. */
export const articlePaths = {
  list: '/settings/articles',
  create: '/settings/articles/new',
  edit: (id: string) => `/settings/articles/${encodeURIComponent(id)}`,
};
