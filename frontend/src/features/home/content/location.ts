/**
 * The clinic's address card on the home page. The address, phone, LINE, map and opening hours come
 * from the API (the contact entries and hours the clinic edits in the staff console); only what the
 * API does not hold is written here.
 */
export const location = {
  name: 'Happy Paws สุขุมวิท 39',

  labels: {
    hours: 'เวลาทำการ',
    phone: 'โทรศัพท์',
    line: 'LINE',
  },

  /** Rows the API has no field for, shown after the ones it does. */
  extraRows: [{ label: 'ที่จอดรถ', value: '6 คัน หน้าคลินิก' }],

  closed: 'ปิด',
  more: { label: 'ช่องทางติดต่อทั้งหมด', href: '/contact' },
};

/** The map beside the address when the clinic's map entry has no Google Maps embed: a picture, put in public/images/home/. */
export const locationMap: { imageSrc?: string; alt: string } = {
  alt: 'แผนที่ Happy Paws สุขุมวิท 39',
};
