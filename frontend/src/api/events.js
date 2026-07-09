// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about EVENTS & ANNOUNCEMENTS:
//   GET    /api/events       → list all events (newest first)
//   GET    /api/events/{id}  → get one event
//   POST   /api/events       → create an event
//   PUT    /api/events/{id}  → edit an event
//   DELETE /api/events/{id}  → delete an event
//   GET    /api/events/my    → events meant for the logged-in user
import api from './axios'
import { request } from './client'

// Events / announcements API (Admin only). Returns { data, message }.
const BASE = '/api/events'

/** GET /api/events — newest first. */
export const listEvents = () => request(api.get(BASE))

/** GET /api/events/{id} */
export const getEvent = (id) => request(api.get(`${BASE}/${id}`))

/** POST /api/events — { title, description, date, type, audienceType, targetClassId? } */
export const createEvent = (payload) => request(api.post(BASE, payload))

/** PUT /api/events/{id} — { title, description, date, type, audienceType, targetClassId? } */
export const updateEvent = (id, payload) => request(api.put(`${BASE}/${id}`, payload))

/** DELETE /api/events/{id} */
export const removeEvent = (id) => request(api.delete(`${BASE}/${id}`))

/** GET /api/events/my — the events targeted at the logged-in user (role/level/class-filtered via JWT). */
export const getMyEvents = () => request(api.get(`${BASE}/my`))
