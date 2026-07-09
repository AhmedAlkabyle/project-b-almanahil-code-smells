// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about a class's TIMETABLE (weekly schedule grid):
//   GET /api/timetable/{classId}  → get the whole timetable grid for a class
//   PUT /api/timetable/{classId}  → save the whole grid (warns if two subjects clash)
import api from './axios'
import { request, normalizeError } from './client'

// Timetable API (Admin only). Reuses the shared Axios/JWT instance.
const BASE = '/api/timetable'

/**
 * GET /api/timetable/{classId} — returns { data, message }.
 * `data` is the full grid payload: { classId, className, classDisplayName, level,
 * periodsPerDay, days[], periods[], break, slots[] }.
 */
export const getTimetable = (classId) => request(api.get(`${BASE}/${classId}`))

/**
 * PUT /api/timetable/{classId} — save the whole grid.
 * Body: { slots: [ { day, period, subjectId | null } ] }.
 *
 * Unlike request(), this resolves for BOTH outcomes so the caller can react to a clash
 * (the backend returns HTTP 400 { success:false, message, data:[clashes] } on a clash):
 *   success -> { ok: true,  message, clashes: [] }
 *   clash   -> { ok: false, message, clashes: [ { subjectName, day, period, otherClassName } ] }
 * Genuine transport/auth errors (network, 401/403, 500) still throw a friendly Error.
 */
export async function saveTimetable(classId, slots) {
  try {
    const res = await api.put(`${BASE}/${classId}`, { slots })
    const body = res?.data ?? {}
    return { ok: body.success !== false, message: body.message || 'Timetable saved.', clashes: [] }
  } catch (err) {
    const body = err?.response?.data
    // A clash (or other validation) is a 400 with success:false — surface it, don't throw.
    if (body && body.success === false) {
      return {
        ok: false,
        message: body.message || 'Cannot save the timetable.',
        clashes: Array.isArray(body.data) ? body.data : []
      }
    }
    // Anything else (network / 401 / 403 / 500) is a real error.
    throw normalizeError(err)
  }
}
