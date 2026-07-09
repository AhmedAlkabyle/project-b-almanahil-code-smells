// ─── WHAT THIS FILE DOES (in plain words) ───
// This builds the ONE shared messenger ("axios") that the whole app uses to talk
// to the backend server. The server's address comes from the .env settings file
// (VITE_API_URL). Every request that goes out through this messenger automatically
// carries the user's login token, so the server knows who is asking.
import axios from 'axios'
import { useAuthStore } from '../stores/auth'

// Central axios instance. baseURL comes from the Vite env (VITE_API_URL).
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Request interceptor: attach the JWT as a Bearer token when one is present.
api.interceptors.request.use((config) => {
  const auth = useAuthStore()
  // Attach the login token (JWT) to every request so the server knows who we are.
  if (auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }
  // Multipart uploads: the instance defaults to 'application/json', which makes axios
  // serialise a FormData body to JSON — silently dropping the file. Remove the header for
  // FormData so the browser sets 'multipart/form-data' with the correct boundary itself.
  // When we are uploading a file, drop the "this is JSON" label so the browser can
  // send the file correctly instead of accidentally turning it into plain text.
  if (typeof FormData !== 'undefined' && config.data instanceof FormData) {
    if (config.headers && typeof config.headers.delete === 'function') {
      config.headers.delete('Content-Type')
    } else if (config.headers) {
      delete config.headers['Content-Type']
      delete config.headers['content-type']
    }
  }
  return config
})

export default api
