// ─── WHAT THIS FILE DOES (in plain words) ───
// Holds small bits of "how the screen looks" state. Right now it just remembers
// whether the side menu is open (full) or collapsed (a thin icon-only strip),
// and saves that choice so it stays the same after a page refresh.
import { defineStore } from 'pinia'

// localStorage key for persisting the admin sidebar collapsed state.
const SIDEBAR_KEY = 'almanahil_sidebar_collapsed'

// Load the saved state, defaulting to the full (expanded) sidebar.
function loadCollapsed() {
  return localStorage.getItem(SIDEBAR_KEY) === '1'
}

// Small shared UI store for admin chrome preferences (sidebar, etc.).
export const useUiStore = defineStore('ui', {
  state: () => ({
    // When true, the admin sidebar shows as a slim, icon-only rail.
    sidebarCollapsed: loadCollapsed()
  }),

  actions: {
    // Flip the sidebar between the full menu and the slim rail.
    toggleSidebar() {
      this.setSidebarCollapsed(!this.sidebarCollapsed)
    },

    // Set a specific collapsed state and persist it across reloads.
    setSidebarCollapsed(collapsed) {
      this.sidebarCollapsed = !!collapsed
      localStorage.setItem(SIDEBAR_KEY, collapsed ? '1' : '0')
    }
  }
})
