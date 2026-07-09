// ─── WHAT THIS FILE DOES (in plain words) ───
// The backend always replies in the same wrapper shape: { success, message, data }.
// request() opens that wrapper for us and hands back just the useful { data, message }.
// If something goes wrong, it turns the raw technical error into a short, friendly
// message a normal person can understand (e.g. "Cannot reach the server").
import api from './axios'

/**
 * Shared helper for calling my ApiResponse<T> endpoints.
 *
 * The backend wraps every response as { success, message, data }. This unwraps it:
 *  - on success  -> resolves to { data, message }
 *  - on failure  -> throws a friendly Error (never a raw axios error), whose
 *                   `.message` is the backend's friendly message when available.
 *
 * Reuses the Module 1 axios instance (baseURL + JWT interceptor).
 */
// Runs an API call, then unwraps the reply (or throws a friendly error).
export async function request(promise) {
  try {
    const res = await promise
    const body = res?.data ?? {}
    // 2xx but the envelope reports a failure (defensive — shouldn't normally happen).
    if (body.success === false) throw new Error(body.message || 'Request failed.')
    return { data: body.data, message: body.message }
  } catch (err) {
    throw normalizeError(err)
  }
}

// Takes a messy technical error and returns a clear, human-readable one.
// Turn any axios/enveloped error into a friendly Error with a helpful message.
export function normalizeError(err) {
  // A plain Error we threw ourselves (e.g. the success:false guard) — pass through.
  if (err && err.isAxiosError !== true && err.message) return err

  const res = err?.response
  if (res) {
    // Prefer the backend's friendly ApiResponse message (duplicate email, wrong
    // domain, "Selected parent not found", etc.).
    const msg = res.data?.message
    if (msg) return new Error(msg)
    if (res.status === 401) return new Error('Your session has expired. Please sign in again.')
    if (res.status === 403) return new Error('You do not have permission to perform this action.')
    return new Error('Something went wrong. Please try again.')
  }

  // No response at all → the server is unreachable (backend down / network).
  return new Error('Cannot reach the server. Please check that the backend is running.')
}

export { api }
