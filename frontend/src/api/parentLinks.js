// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about linking a PARENT to their CHILD:
//   GET    /api/parent-links       → list parent-student links (can filter by parent)
//   POST   /api/parent-links       → link a parent to a student
//   DELETE /api/parent-links/{id}  → remove a link
import api from './axios'
import { request } from './client'

// Parent-student links API (Admin only). Returns { data, message }.
const BASE = '/api/parent-links'

/** GET /api/parent-links — optional { parentId } filter. */
export const listParentLinks = (params = {}) => request(api.get(BASE, { params }))

/** POST /api/parent-links — { parentId, studentId } */
export const createParentLink = (payload) => request(api.post(BASE, payload))

/** DELETE /api/parent-links/{id} */
export const removeParentLink = (id) => request(api.delete(`${BASE}/${id}`))
