/** Google Maps at a branch's pin, the way a phone opens it in the Maps app when it has one. */
export function mapLink(latitude: number, longitude: number): string {
  return `https://www.google.com/maps/search/?api=1&query=${latitude},${longitude}`;
}
