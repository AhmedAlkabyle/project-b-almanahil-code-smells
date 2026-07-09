// Mock teacher→subject assignments (who teaches what).
// teacherId → mockTeachers, subjectId → mockSubjects (whose classId gives the class).
// TODO: replace with GET /api/assignments once the backend endpoint exists.
export const mockAssignments = [
  { id: 1, teacherId: 1, subjectId: 1 },
  { id: 2, teacherId: 2, subjectId: 2 },
  { id: 3, teacherId: 3, subjectId: 3 }
]

export default mockAssignments
