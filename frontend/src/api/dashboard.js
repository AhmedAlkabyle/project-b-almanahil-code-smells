// ─── WHAT THIS FILE DOES (in plain words) ───
// This file talks to the backend to get the numbers shown on the admin dashboard:
//   GET /api/dashboard/stats  → the headline counts for the dashboard cards
import api from './axios'
import { request } from './client'

// Admin dashboard stats API. Returns { data, message }.

/** GET /api/dashboard/stats — headline counts for the dashboard cards. */
export const getDashboardStats = () => request(api.get('/api/dashboard/stats'))
