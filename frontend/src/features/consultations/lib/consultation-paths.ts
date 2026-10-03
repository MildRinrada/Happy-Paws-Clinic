/** Where a case lives in the console. */
export const consultationPaths = {
  detail: (id: string) => `/consultations/${encodeURIComponent(id)}`,
};
