/**
 * Where the pharmacy counter lives. `forPet` is the link other screens (the pet's page, a closed
 * case) can point at to open one patient's medication history straight away.
 */
export const prescriptionPaths = {
  list: '/prescriptions',
  forPet: (petId: string) => `/prescriptions?pet_id=${encodeURIComponent(petId)}`,
  search: (q: string) => `/prescriptions?q=${encodeURIComponent(q)}`,
};
