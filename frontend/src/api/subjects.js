// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about SUBJECTS (like Maths, English):
//   GET    /api/subjects        → list subjects (can filter by class)
//   GET    /api/subjects/{id}   → get one subject
//   POST   /api/subjects        → create a subject
//   PUT    /api/subjects/{id}   → edit a subject
//   DELETE /api/subjects/{id}   → delete a subject
import api from './axios'
import { request } from './client'

// Subjects API (Admin only). Every function returns { data, message }.
const BASE = '/api/subjects'

/** GET /api/subjects — optional { classId } filter. */
export const listSubjects = (params = {}) => request(api.get(BASE, { params }))

/** GET /api/subjects/{id} */
export const getSubject = (id) => request(api.get(`${BASE}/${id}`))

/** POST /api/subjects — { name, classId } */
export const createSubject = (payload) => request(api.post(BASE, payload))

/** PUT /api/subjects/{id} — { name, classId } */
export const updateSubject = (id, payload) => request(api.put(`${BASE}/${id}`, payload))

/** DELETE /api/subjects/{id} */
export const removeSubject = (id) => request(api.delete(`${BASE}/${id}`))
