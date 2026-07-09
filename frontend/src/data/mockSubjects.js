// Mock subjects, each belonging to one class (classId → mockClasses).
// Shared by the Manage Subjects and Teacher Assignments screens.
// TODO: replace with GET /api/subjects once the backend endpoint exists.
export const mockSubjects = [
  { id: 1, name: 'Mathematics', classId: 1 },
  { id: 2, name: 'Arabic', classId: 2 },
  { id: 3, name: 'Science', classId: 3 },
  { id: 4, name: 'English', classId: 1 },
  { id: 5, name: 'Islamic Studies', classId: 4 },
  { id: 6, name: 'Social Studies', classId: 5 }
]

export default mockSubjects
