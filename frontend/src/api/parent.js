// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend about the logged-in PARENT's own children:
//   GET /api/parent/my-children  → the list of children linked to this parent
import api from './axios'
import { request } from './client'

// Parent API. Every function returns { data, message }.
const BASE = '/api/parent'

/** GET /api/parent/my-children — the logged-in parent's linked children (parent-scoped via JWT). */
export const getMyChildren = () => request(api.get(`${BASE}/my-children`))
