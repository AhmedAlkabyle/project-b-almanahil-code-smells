// ─── WHAT THIS FILE DOES (in plain words) ───
// This file is the "phone book" for anything about USERS. Each function calls one
// backend address on the server:
//   GET    /api/users              → list users (can filter by role/search)
//   GET    /api/users/{id}         → get one user
//   POST   /api/users              → create a user
//   PUT    /api/users/{id}         → edit a user
//   PUT    /api/users/{id}/status  → activate / deactivate a user
//   DELETE /api/users/{id}         → permanently delete a user
import api from './axios'
import { request } from './client'

// Users management API (Admin only). Every function returns { data, message }.
const BASE = '/api/users'

/** GET /api/users — optional { role, search } filters. */
export const listUsers = (params = {}) => request(api.get(BASE, { params }))

/** GET /api/users?role=Student&unassigned=true — students not yet linked to any class. */
export const listUnassignedStudents = () =>
  request(api.get(BASE, { params: { role: 'Student', unassigned: true } }))

/** GET /api/users/{id} */
export const getUser = (id) => request(api.get(`${BASE}/${id}`))

/** POST /api/users — CreateUserDto (firstName, middleName, lastName, email, gender,
 *  dateOfBirth, role, classId?, parentId?, phoneNumber?). */
export const createUser = (payload) => request(api.post(BASE, payload))

/** PUT /api/users/{id} — UpdateUserDto (personal fields, no password/role). */
export const updateUser = (id, payload) => request(api.put(`${BASE}/${id}`, payload))

/** PUT /api/users/{id}/status — activate/deactivate (no hard delete). */
export const setUserStatus = (id, isActive) => request(api.put(`${BASE}/${id}/status`, { isActive }))

/** DELETE /api/users/{id} — permanently remove a user (and their links). Irreversible. */
export const deleteUser = (id) => request(api.delete(`${BASE}/${id}`))
