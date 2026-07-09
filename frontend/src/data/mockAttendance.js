// Mock daily attendance records. status is 'Present' | 'Absent' | 'Late'.
// studentId → mockStudents, classId → mockClasses, date is ISO 'yyyy-mm-dd'.
// TODO: replace with GET /api/attendance once the backend endpoint exists.
export const mockAttendance = [
  { id: 1, studentId: 1, classId: 1, date: '2026-06-29', status: 'Present' },
  { id: 2, studentId: 2, classId: 2, date: '2026-06-29', status: 'Absent' },
  { id: 3, studentId: 3, classId: 3, date: '2026-06-29', status: 'Late' },
  { id: 4, studentId: 4, classId: 4, date: '2026-06-29', status: 'Present' },
  { id: 5, studentId: 5, classId: 5, date: '2026-06-29', status: 'Present' },
  { id: 6, studentId: 1, classId: 1, date: '2026-06-28', status: 'Present' },
  { id: 7, studentId: 2, classId: 2, date: '2026-06-28', status: 'Present' },
  { id: 8, studentId: 3, classId: 3, date: '2026-06-28', status: 'Absent' }
]

export default mockAttendance
