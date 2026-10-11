/** The bar along the bottom of a settings page while a form there has changes not yet saved. */
export const saveBarText = {
  region: 'การแก้ไขที่ยังไม่บันทึก',
  unsaved: 'มีการแก้ไขที่ยังไม่บันทึก',
  /** Which part of the page: one name, or how many and which. */
  sections: (names: string[]) => (names.length === 1 ? names[0] : `${names.length} ส่วน: ${names.join(', ')}`),
  failed: (names: string[]) => `บันทึกไม่สำเร็จ ดูข้อความที่ส่วน ${names.join(', ')}`,
  saved: 'บันทึกแล้ว',
  /** Asked before a link, the page list or the browser takes the person away from unsaved changes. */
  leave: 'มีการแก้ไขที่ยังไม่บันทึก ออกจากหน้านี้เลยไหม การแก้ไขจะหายไป',
};
