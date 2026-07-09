// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about which TEACHER teaches which SUBJECT:
//   GET    /api/assignments       → list all teacher-subject assignments
//   POST   /api/assignments       → assign a subject to a teacher
//   DELETE /api/assignments/{id}  → remove an assignment
import api from './axios'
import { request } from './client'

// Teacher-subject assignments API (Admin only). Returns { data, message }.
const BASE = '/api/assignments'

/** GET /api/assignments — all assignments with resolved teacher/subject/class names. */
export const listAssignments = () => request(api.get(BASE))

/** POST /api/assignments — { teacherId, subjectId } */
export const createAssignment = (payload) => request(api.post(BASE, payload))

/** DELETE /api/assignments/{id} */
export const removeAssignment = (id) => request(api.delete(`${BASE}/${id}`))
