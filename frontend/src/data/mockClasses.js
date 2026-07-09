// Mock class list, shared across Manage Classes / Subjects / Users screens.
// `studentsCount` is a display-only mock number; `description` is optional.
// TODO: replace with GET /api/classes once the backend endpoint exists.
export const mockClasses = [
  { id: 1, name: 'Grade 1A', description: 'First grade, section A', studentsCount: 24 },
  { id: 2, name: 'Grade 2B', description: 'Second grade, section B', studentsCount: 21 },
  { id: 3, name: 'Grade 3A', description: 'Third grade, section A', studentsCount: 27 },
  { id: 4, name: 'Grade 4B', description: 'Fourth grade, section B', studentsCount: 19 },
  { id: 5, name: 'Grade 5A', description: 'Fifth grade, section A', studentsCount: 23 }
]

export default mockClasses
