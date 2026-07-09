// Mock announcements/events published by the admin (all roles will see these later).
// `type` is 'Announcement' | 'Event'; `date` is an ISO yyyy-mm-dd string.
// TODO: replace with GET /api/events once the backend endpoint exists.
export const mockEvents = [
  {
    id: 1,
    title: 'Parent-Teacher Meeting',
    date: '2026-07-15',
    description: 'Termly parent-teacher meetings will be held in the main hall. Please check your assigned time slot.',
    type: 'Event'
  },
  {
    id: 2,
    title: 'New Semester Registration Open',
    date: '2026-07-05',
    description: 'Registration for the new semester is now open. Kindly complete the forms before the deadline.',
    type: 'Announcement'
  },
  {
    id: 3,
    title: 'Annual Science Fair',
    date: '2026-08-02',
    description: 'Students from all grades are invited to showcase their projects at the annual science fair.',
    type: 'Event'
  }
]

export default mockEvents
