/** Everything the edit-pet page says. Edit the wording here. */
export const petEdit = {
  title: (name: string) => `แก้ไขข้อมูลของ${name}`,
  intro: 'ข้อมูลส่วนนี้ช่วยให้ทีมดูแลน้องได้ถูกใจ ส่วนข้อมูลทางการแพทย์แก้ได้ที่คลินิกเท่านั้น',

  photo: 'รูปน้อง',
  photoPick: 'เลือกรูปใหม่',
  photoHint: 'JPG, PNG, WEBP หรือ HEIC ไม่เกิน 10 MB',
  photoWrongType: 'ใช้ได้เฉพาะไฟล์ JPG, PNG, WEBP หรือ HEIC',
  photoTooBig: 'ไฟล์ใหญ่เกิน 10 MB',
  photoUploadFailed: 'อัปโหลดรูปไม่สำเร็จ กรุณาลองใหม่อีกครั้ง',

  nickname: 'ชื่อเล่น',
  nicknameHint: 'ชื่อที่น้องคุ้นเคย ให้ทีมเรียกได้ถูก',
  diet: 'อาหารที่กิน',
  dietHint: 'ยี่ห้อ สูตร หรืออาหารที่ห้ามให้',
  behaviour: 'นิสัยและพฤติกรรม',
  behaviourHint: 'เช่น กลัวเสียงดัง ไม่ชอบให้จับหู ชอบกัดเมื่อตกใจ',
  clearHint: 'ลบข้อความออกทั้งหมดเพื่อล้างข้อมูลช่องนั้น',

  save: 'บันทึก',
  saving: 'กำลังบันทึก',
  uploading: 'กำลังอัปโหลดรูป',
  cancel: 'ยกเลิก',

  conflict: 'ข้อมูลของน้องถูกแก้จากที่อื่นระหว่างนี้ โหลดหน้าใหม่แล้วแก้อีกครั้ง',
  unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  deceased: 'แก้ไขข้อมูลของน้องที่จากไปแล้วไม่ได้',
};
