/** Where a patient's page lives in the console. */
export const petPaths = {
  detail: (id: string) => `/pets/${encodeURIComponent(id)}`,
};
