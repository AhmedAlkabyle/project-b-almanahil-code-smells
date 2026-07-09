// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about GRADES (marks/scores):
//   GET /api/grades/my-subjects              → a teacher's subjects
//   GET /api/grades/subject/{id}?assessment  → the mark sheet for a subject
//   PUT /api/grades/subject/{id}             → save the marks for a subject
//   GET /api/grades/reports                  → admin reports (with filters)
//   GET /api/grades/student/{id}             → one student's grades (admin/parent view)
import api from './axios'
import { request } from './client'

// Grades API. Every function returns { data, message }. Mirrors api/attendance.js.
const BASE = '/api/grades'

/** GET /api/grades/my-subjects — the logged-in teacher's assigned subjects. */
export const getMySubjects = () => request(api.get(`${BASE}/my-subjects`))

/** GET /api/grades/subject/{subjectId}?assessmentType=Quiz — the grade sheet (students + marks). */
export const getGradeSheet = (subjectId, assessmentType) =>
  request(api.get(`${BASE}/subject/${subjectId}`, { params: { assessmentType } }))

/** PUT /api/grades/subject/{subjectId} — { subjectId, assessmentType, maxMark, entries: [{ studentId, mark, note }] }. */
export const saveGrades = (subjectId, payload) =>
  request(api.put(`${BASE}/subject/${subjectId}`, payload))

/** GET /api/grades/reports (Admin) — optional filters { classId, subjectId, studentId, assessmentType }. */
export const getGradeReports = (params = {}) => request(api.get(`${BASE}/reports`, { params }))

/** GET /api/grades/student/{studentId} — a student's grades (Admin, the student, or their parent). */
export const getStudentGrades = (studentId) => request(api.get(`${BASE}/student/${studentId}`))
