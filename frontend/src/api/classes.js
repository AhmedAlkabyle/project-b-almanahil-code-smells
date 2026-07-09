// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about school CLASSES (and who is in them):
//   GET    /api/classes                              → list classes
//   GET    /api/classes/{id}                         → get one class
//   POST   /api/classes                              → create a class
//   PUT    /api/classes/{id}                         → edit a class
//   DELETE /api/classes/{id}                         → delete a class
//   POST   /api/classes/{id}/students                → add students to a class
//   GET    /api/classes/{id}/students                → list a class's students
//   DELETE /api/classes/{id}/students/{studentId}    → remove a student from a class
import api from './axios'
import { request } from './client'

// Classes API (Admin only). Every function returns { data, message }.
const BASE = '/api/classes'

/** GET /api/classes */
export const listClasses = () => request(api.get(BASE))

/** GET /api/classes/{id} */
export const getClass = (id) => request(api.get(`${BASE}/${id}`))

/** POST /api/classes — { academicYear, level, grade, section, description? }. Name is auto-composed server-side. */
export const createClass = (payload) => request(api.post(BASE, payload))

/** PUT /api/classes/{id} — { academicYear, level, grade, section, description? }. */
export const updateClass = (id, payload) => request(api.put(`${BASE}/${id}`, payload))

/** DELETE /api/classes/{id} */
export const removeClass = (id) => request(api.delete(`${BASE}/${id}`))

/** POST /api/classes/{classId}/students — enroll students into a class. Body: { studentIds: [ ... ] }. */
export const enrollStudents = (classId, studentIds) =>
  request(api.post(`${BASE}/${classId}/students`, { studentIds }))

/** GET /api/classes/{classId}/students — students enrolled in a class ({ id, fullName, email }). */
export const listClassStudents = (classId) => request(api.get(`${BASE}/${classId}/students`))

/** DELETE /api/classes/{classId}/students/{studentId} — unenroll a student (their ClassId -> null). */
export const removeStudentFromClass = (classId, studentId) =>
  request(api.delete(`${BASE}/${classId}/students/${studentId}`))
