// ─── WHAT THIS FILE DOES (in plain words) ───
// This tiny file knows the "home page" for each type of user. Give it a role
// (Admin/Teacher/Student/Parent) and it tells you which dashboard address to
// open. The login redirect and the security guard both use dashboardFor().
// Maps a backend role string to its dashboard route.
// Shared between the login redirect and the router navigation guard.
export const roleToRoute = {
  Admin: '/admin',
  Teacher: '/teacher',
  Student: '/student',
  Parent: '/parent'
}

// Give this a role and it hands back that role's home page address.
// If the role is unknown, it safely falls back to the login page.
// Returns the dashboard path for a role, defaulting to /login if unknown.
export function dashboardFor(role) {
  return roleToRoute[role] || '/login'
}
