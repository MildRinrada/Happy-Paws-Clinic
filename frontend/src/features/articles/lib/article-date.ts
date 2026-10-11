const publishedDay = new Intl.DateTimeFormat('th-TH', {
  timeZone: 'Asia/Bangkok',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
});

/** "29 กันยายน 2569": the clinic's calendar day an article went live. */
export function formatArticleDate(iso: string): string {
  return publishedDay.format(new Date(iso));
}
