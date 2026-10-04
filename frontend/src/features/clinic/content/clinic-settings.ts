/** The settings page for the clinic's profile and its branches. */
export const clinicSettings = {
  metaTitle: 'คลินิกและสาขา · Happy Paws Clinic',
  title: 'คลินิกและสาขา',
  subtitle: 'ชื่อ โลโก้ สี และข้อมูลใบอนุญาตที่แสดงบนเว็บ กับสาขาที่เปิดรับนัด',

  profile: {
    heading: 'ข้อมูลคลินิก',
    name: 'ชื่อคลินิก',
    logoUrl: 'ลิงก์รูปโลโก้ (ไม่บังคับ)',
    logoUrlHint: 'ลิงก์รูปที่ขึ้นต้นด้วย https:// เว้นว่างไว้ถ้าไม่มี',
    brandColor: 'สีหลักของคลินิก',
    brandColorHint: 'ใช้รูปแบบ #RRGGBB เช่น #217A71',
    licenseNo: 'เลขที่ใบอนุญาตสถานพยาบาลสัตว์',
    operatorName: 'ผู้ดำเนินการสถานพยาบาล',
    operatorNameHint: 'ชื่อสัตวแพทย์ผู้ดำเนินการตามใบอนุญาต',
  },

  branches: {
    heading: 'สาขา',
    empty: 'ยังไม่มีสาขา เพิ่มสาขาแรกได้ด้านล่าง',
    rooms: 'ห้องตรวจ',
    noRooms: 'ยังไม่ระบุห้องตรวจ',
    coordinates: 'พิกัด',
    editNote: 'ตอนนี้ระบบเปิดสาขาใหม่ได้อย่างเดียว ยังแก้ชื่อ ที่อยู่ หรือห้องตรวจของสาขาเดิมไม่ได้',
    add: '+ เปิดสาขาใหม่',
    code: 'รหัสสาขา',
    codeHint: 'a-z 0-9 _ หรือ - ยาว 2 ถึง 32 ตัว เช่น sukhumvit39 ใช้เป็นรหัสถาวร แก้ภายหลังไม่ได้',
    name: 'ชื่อสาขา',
    address: 'ที่อยู่ (ไม่บังคับ)',
    latitude: 'ละติจูด (ไม่บังคับ)',
    longitude: 'ลองจิจูด (ไม่บังคับ)',
    coordinatesHint: 'ใส่ทั้งคู่หรือเว้นทั้งคู่ คัดลอกได้จาก Google Maps',
    examRooms: 'ห้องตรวจ',
    examRoomsHint: 'คั่นแต่ละห้องด้วยเครื่องหมายจุลภาค เช่น ห้อง 1, ห้อง 2',
  },
};
