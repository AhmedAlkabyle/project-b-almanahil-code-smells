// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about ATTENDANCE (who was present/absent):
//   GET /api/attendance/my-subjects        → a teacher's subjects
//   GET /api/attendance/my                 → the logged-in student's own attendance
//   GET /api/attendance/student/{id}       → one student's attendance (admin/parent view)
//   GET /api/attendance/subject/{id}?date  → the class register for a date
//   PUT /api/attendance/subject/{id}       → save the register for a date
//   GET /api/attendance/reports            → admin reports (with filters)
import api from './axios'
import { request } from './client'

// Attendance API. Every function returns { data, message }.
const BASE = '/api/attendance'

/** GET /api/attendance/my-subjects — the logged-in teacher's assigned subjects. */
export const getMySubjects = () => request(api.get(`${BASE}/my-subjects`))

/** GET /api/attendance/my — the logged-in student's OWN attendance records (student-scoped via JWT). */
export const getMyAttendance = () => request(api.get(`${BASE}/my`))

/** GET /api/attendance/student/{studentId} — a student's attendance (Admin, the student, or their parent). */
export const getStudentAttendance = (studentId) => request(api.get(`${BASE}/student/${studentId}`))

/** GET /api/attendance/subject/{subjectId}?date=YYYY-MM-DD — the attendance sheet (students + statuses). */
export const getAttendanceSheet = (subjectId, date) =>
  request(api.get(`${BASE}/subject/${subjectId}`, { params: { date } }))

/** PUT /api/attendance/subject/{subjectId} — { subjectId, date, entries: [{ studentId, status, note }] }. */
export const saveAttendance = (subjectId, payload) =>
  request(api.put(`${BASE}/subject/${subjectId}`, payload))

/** GET /api/attendance/reports (Admin) — optional filters { classId, subjectId, studentId, from, to }. */
export const getAttendanceReports = (params = {}) => request(api.get(`${BASE}/reports`, { params }))
