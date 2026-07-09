// ─── WHAT THIS FILE DOES (in plain words) ───
// This is the "memory" for a logged-in parent. It keeps the list of the parent's
// children and remembers WHICH child is currently picked. All the parent pages
// (dashboard, attendance, grades...) read from here, so when the parent switches
// child in the top bar, every page shows that child's info.
import { defineStore } from 'pinia'
import { getMyChildren } from '../api/parent'

// Holds the logged-in parent's children (from GET /api/parent/my-children) and which
// child is currently selected. Shared across the parent dashboard / attendance / grades
// pages so switching child in the top bar updates every page. In-memory only (cleared on
// logout via $reset() so a different parent never inherits the previous one's children).
export const useParentStore = defineStore('parent', {
  state: () => ({
    children: [],
    selectedChildId: null,
    loading: false,
    error: '',
    loaded: false
  }),

  getters: {
    // The currently-selected child object, or null.
    selectedChild: (state) => state.children.find((c) => c.studentId === state.selectedChildId) || null,
    hasChildren: (state) => state.children.length > 0,
    hasMultiple: (state) => state.children.length > 1
  },

  actions: {
    // ACTION: ask the server for this parent's children and store them here.
    // The first child is picked automatically so a page always has someone to show.
    /**
     * Fetch the parent's children. Pass force=true to always refetch (used when the parent
     * area first mounts, so a fresh login never shows a previous session's children).
     */
    async loadChildren(force = false) {
      if (this.loaded && !force) return
      this.loading = true
      this.error = ''
      try {
        const { data } = await getMyChildren()
        this.children = data ?? []
        // Auto-select the first child; keep the current selection if it's still valid.
        if (!this.children.some((c) => c.studentId === this.selectedChildId)) {
          this.selectedChildId = this.children[0]?.studentId ?? null
        }
        this.loaded = true
      } catch (err) {
        this.error = err.message || 'We couldn’t load your children.'
        this.children = []
        this.selectedChildId = null
      } finally {
        this.loading = false
      }
    },

    setChild(id) {
      this.selectedChildId = id
    }
  }
})
