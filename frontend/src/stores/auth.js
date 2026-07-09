// ─── WHAT THIS FILE DOES (in plain words) ───
// This is the app's "memory" of WHO is logged in. It keeps the user's details,
// their login token (a secret pass called a JWT), and their profile picture.
// Almost everything uses it: the security guard checks it, axios attaches the
// token from it, and the top bars read the name/photo from it.
import { defineStore } from 'pinia'
import api from '../api/axios'

// Keys used to persist auth state in localStorage.
const TOKEN_KEY = 'almanahil_token'
const USER_KEY = 'almanahil_user'
// Profile pictures are stored per-user (keyed by id/email) as a base64 data URL.
// There is no backend upload endpoint yet, so this keeps avatars local + private.
const AVATAR_PREFIX = 'almanahil_avatar_'

// Safely parse the stored user object (returns null if missing/corrupt).
function loadStoredUser() {
  try {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

// A stable per-user key for the avatar (prefers id, falls back to email).
function avatarKey(user) {
  const id = user?.id ?? user?.userId ?? user?.email ?? 'me'
  return AVATAR_PREFIX + id
}

// Load the stored avatar (data URL) for the given user, if any.
function loadStoredAvatar(user) {
  try {
    return user ? localStorage.getItem(avatarKey(user)) || null : null
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', {
  // Initialize from localStorage so a page refresh keeps the user logged in.
  state: () => {
    const user = loadStoredUser()
    return {
      token: localStorage.getItem(TOKEN_KEY) || null,
      user,
      // base64 data URL of the profile picture, or null.
      avatar: loadStoredAvatar(user)
    }
  },

  getters: {
    // True when we have a token (i.e. the user is logged in).
    isAuthenticated: (state) => !!state.token,
    // The role string ("Admin" | "Teacher" | "Student" | "Parent") or null.
    userRole: (state) => state.user?.role || null
  },

  actions: {
    // ACTION: log the user in. Sends their email + password to the server,
    // then remembers the user and their token if it worked.
    /**
     * Logs in via the backend. On success, persists token + user and returns
     * the user object. On failure, throws so the caller can show an error.
     */
    async login(email, password) {
      // Step 1: Call the backend's login API with the typed email + password.
      const response = await api.post('/api/auth/login', { email, password })
      const body = response.data

      // Backend wraps payloads as { success, message, data }.
      if (!body?.success || !body.data?.token) {
        throw new Error(body?.message || 'Login failed')
      }

      // Step 2: Split the reply into the token (secret pass) and the user info,
      //         then save both into this store's memory.
      const { token, ...user } = body.data
      this.token = token
      this.user = user
      // Restore this user's saved profile picture (if they set one before).
      this.avatar = loadStoredAvatar(user)

      // Step 3: Also save them in the browser's localStorage (a small storage box
      //         that survives page refreshes) so the user stays logged in.
      // Persist for refresh-survival.
      localStorage.setItem(TOKEN_KEY, token)
      localStorage.setItem(USER_KEY, JSON.stringify(user))

      return user
    },

    // ACTION: save (or remove) the user's profile picture.
    /**
     * Sets (or clears, when passed null) the current user's profile picture.
     * Persists it per-user in localStorage so it survives refreshes and logouts.
     */
    setAvatar(dataUrl) {
      this.avatar = dataUrl || null
      try {
        const key = avatarKey(this.user)
        if (dataUrl) localStorage.setItem(key, dataUrl)
        else localStorage.removeItem(key)
      } catch {
        // Storage full or unavailable — keep the in-memory value regardless.
      }
    },

    // ACTION: log the user out by forgetting them everywhere they were saved.
    /**
     * Securely logs the user out. Wipes every place the session could live:
     * the store state, localStorage, and the axios Authorization header.
     * Navigation is handled by the caller (components use router.replace).
     */
    logout() {
      // 1. Clear in-memory state. token + user are the source of truth; the
      //    userRole getter derives from user, so it becomes null automatically.
      this.token = null
      this.user = null
      // Clear the in-memory avatar, but leave it in localStorage so it returns
      // for this user on their next login.
      this.avatar = null

      // 2. Remove persisted data so a page refresh can't restore the session.
      localStorage.removeItem(TOKEN_KEY)
      localStorage.removeItem(USER_KEY)

      // 3. Drop any Authorization header set as an axios default. (Normally the
      //    request interceptor adds the token per-request and now sees none, so
      //    this is a belt-and-suspenders cleanup in case it was ever set.)
      delete api.defaults.headers.common.Authorization
    }
  }
})
